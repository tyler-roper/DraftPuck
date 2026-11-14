# Architecture

How DraftPuck fits together, and where to put new code. For setup see [environment.md](./environment.md); for the
contribution workflow see [CONTRIBUTING.md](../CONTRIBUTING.md).

## Projects

| Project | What it holds | Depends on |
|---|---|---|
| `DraftPuck.Shared` | Entities, DTOs, enums, MediatR notifications, and small rules that belong to the data (e.g. `GameDto.IsPickingOpen`) | — |
| `DraftPuck.Application` | Features as MediatR commands, queries and notification handlers, one folder per area under `Features/` | Shared |
| `DraftPuck.Infrastructure` | EF Core (`DraftPuckContext`, migrations), Redis, SignalR, the NHL API client, Azure Storage, Firebase | Application, Shared |
| `DraftPuck.Web` | ASP.NET Core API (`Features/*Controller.cs`), the SignalR hub endpoint, and the Vue app in `ClientApp/` | all |
| `DraftPuck.Worker` | Azure Functions: the daily schedule load, game polling, achievement checks | Application, Infrastructure |
| `DraftPuck.DiscordBot` | The Discord bot; tells Web about Discord events through the internal API (`api/internal`, API-key protected) | Application, Infrastructure |
| `DraftPuck.Tests` | xUnit tests against real SQL Server and Redis (Testcontainers) | all |

Controllers stay thin: they turn a request into a command or query and `mediator.Send` it. Business rules live in
handlers, and handlers talk to the database through `IDbContext`.

## Side effects are notifications

When something happens, the handler that did it publishes a notification instead of doing every follow-up itself.
For example, `MakePickCommandHandler` publishes `PickMadeNotification`; `LobbyEventHandler` turns it into a lobby
event (the feed), and `LobbyEventCreatedHandler` pushes that to clients. To react to something new, add an
`INotificationHandler<...>` rather than editing the handler that raised it.

Notifications are published in-process, in order, and run synchronously. A handler that must not fail its publisher
(pushes, bot picks) catches and logs its own errors.

## A game's day

1. **10:01 UTC**, `ApplyCutoffFunction` (Worker): deactivates yesterday's lobbies and loads today's schedule
   (`RefreshScheduleCommand`), queuing one `ProcessGameMessage` per game. The "NHL day" switches at 10:00 UTC
   (`GameProcessingHelpers.CutoffHours`).
2. **Each poll**, `ProcessGameFunction` → `ProcessGameCommandHandler`:
   - fetches the game from the NHL API and the previous poll's copy from Redis (`IGameCache`);
   - syncs rosters (`GameRosterSync`): refetches changed lineups, and removes picks of players who left the lineup
     ("scratched", with a push to the pick's owner);
   - works out what happened to each goal (`GoalMatcher`): matched by content first (scorer, period, clock within 3s)
     because the NHL renumbers plays, then by play ID;
   - saves the game and pushes it to every client (`GameUpdated`);
   - publishes goal notifications, which award, revoke or re-key drinks (see *Goal corrections* below);
   - sends pregame alerts and bot picks, and queues the next poll (`GameProcessingHelpers.GetPollDelay`: 10s while
     live, 1 min near puck drop, 10 min before that).
3. Polling stops once the game is final.

Failures schedule a 2-minute retry. NHL API outages log a warning; anything else logs an error.

## Real time

Clients connect to the SignalR hub at `/hub` (`LobbyHub`) and join their lobby's group. The Worker sends through the
same hub via the Redis backplane, so either process can push. Messages:

| Message | Sent to | Client reaction |
|---|---|---|
| `LobbyEvent` | the lobby | refetch the lobby, add to the feed, maybe toast (`useLobbyEvents`) |
| `Message` | the lobby | add to chat, count unread (`useLobbyChat`) |
| `LobbyStateChanged` | the lobby | refetch the lobby |
| `GameUpdated` | everyone | update that game in place (`stores/games.ts`) |

On the client, `useLobbyConnection` owns the connection: retries, auto-reconnect, rejoining the group, and pausing
while the tab is hidden. After every (re)connect, and every 5 minutes as a safety net for lost pushes, the page
refetches everything (`resync`). Games carry an `UpdatedUtc` stamp so an older copy never overwrites a newer one.

## Domain rules worth knowing

- **Picking** opens 30 minutes before puck drop and stays open until the game is final. The rules are defined once,
  on the server (`GameDto.PickingOpensUtc` / `IsPickingClosed` / `CanMembersRemovePicks`, `PickPermissions`), and
  sent to the client as facts. The client applies them only through `ClientApp/src/helpers/pickRules.ts`. Don't
  re-implement a pick rule in a component.
- **Picks wait for the server**: the UI shows them as pending until it confirms (`lobbyStore.makePicks`). Chat is the
  one optimistic action, and it rolls back on failure.
- **Bots** pick as soon as rosters are complete, before members can, so they never compete for players.
- **Drinks** only go to picks made before the goal happened (`PlayDto.HappenedUtc`: when a poll first saw it).
- **Goal corrections** are game events every lobby with the game hears about:

  | What happened | What the lobby sees |
  |---|---|
  | New goal | Drink awarded to each picker of the scorer |
  | Scorer changed | "Goal changed"; old scorer's drinks revoked; new scorer's pickers awarded |
  | Goal called back | "Goal removed"; the scorer's drinks revoked |
  | Time-only correction, renumbered play | Nothing |

  A revoke is "drink invalidated" if the drink was already given to someone, otherwise "drink revoked".

## The client (`DraftPuck.Web/ClientApp`)

| Folder | What goes there |
|---|---|
| `views/` | Pages. They hold page state and wire things together; keep logic out of them. |
| `components/` | Reusable UI (`V*.vue`). |
| `stores/` | Pinia stores: `lobby`, `games`, `user`, `system`. Server data lives here. |
| `composables/` | Stateful behavior shared or pulled out of a page (`useLobbyConnection`, `useLobbyEvents`, ...). |
| `helpers/` | Pure functions: rules, formatting, templates. Easiest to test, so prefer them. |
| `services/` | One class per API controller, on top of `HttpService` (auth, token refresh, date parsing). |

Everything under `helpers/`, `stores/` and `composables/` has tests in a sibling `__tests__/` folder (Vitest).

## Adding a feature

1. **Server**: a folder under `Application/Features/<Area>/` with the command or query, its handler, and DTOs. Add a
   thin controller action in `Web/Features/<Area>/`.
2. **Data**: change the entity in `Shared`, configure it in `DraftPuckContext`, and add a migration
   (`dotnet ef migrations add <Name> --project DraftPuck.Infrastructure`). The Web app applies migrations at startup.
3. **Follow-ups**: publish a notification and handle it, rather than growing the handler.
4. **Client**: a `services/` method, store state if several components need it, and logic in a helper or composable
   with tests.
5. **Tests**: handler tests in `DraftPuck.Tests/Application/<Area>/` against the real database (see the fixtures in
   `Support/`); client tests next to the code.
