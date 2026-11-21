# DraftPuck

A live hockey drinking game: .NET 10 API and Azure Functions worker, Vue 3 client, SQL Server, Redis, SignalR.

Read `docs/architecture.md` before changing code: it covers the projects, how requests, games and real-time updates
flow, the domain rules, and where new code goes. Known issues and planned work are GitHub issues
(`gh issue list`).

## Commands

- Server tests (Docker must be running; Testcontainers starts SQL Server and Redis): `dotnet test DraftPuck.Tests`
- Client (from `DraftPuck.Web/ClientApp`): `npm test`, `npm run lint:check`, `npm run type-check`,
  `npm run build-only:release`
- `build-only:release` rewrites `DraftPuck.Web/Views/App/Index.cshtml`. Restore it afterwards
  (`git restore DraftPuck.Web/Views/App/Index.cshtml`); it's a build output, not a change.
- Migrations: `dotnet ef migrations add <Name> --project DraftPuck.Infrastructure --startup-project DraftPuck.Infrastructure`.
  `dotnet-ef` comes from the repo's tool manifest (`dotnet-tools.json`, pinned to the EF Core version): run
  `dotnet tool restore` once. Don't install it globally.
- The client needs Node 22.22+ (or 24.15+).
- See a change in the real app: the `run-draftpuck` skill (`.claude/skills/run-draftpuck/`) runs an isolated copy built
  from the working tree at http://localhost:17100, seeds friends and lobbies, and screenshots pages in headless Chrome.

## Workflow

- Branch from `origin/develop` with `--no-track` (`git switch -c <branch> --no-track origin/develop`). GitHub
  Desktop pushes to the upstream, so a branch tracking `develop` pushes straight to `develop`.
- One PR per coherent change into `develop`; end the description with the Claude Code attribution line.
- Run both suites and wait for CI (`gh pr checks <n> --watch`). Merge only when the user asks; use a merge commit
  (`--merge`), which matches the repo's history.
- `main` deploys (`.github/workflows/autodeploytrigger.yml`). The maintainer deploys by merging a `develop` → `main`
  PR; don't push to `main` or open PRs against it.
- Never bypass branch protection (`gh pr merge --admin`, force pushes to `develop` or `main`). PRs merge only through
  review and passing checks.

## Conventions

- Global usings cover only `Domain` and `Application.Common`; import feature namespaces explicitly, so each file shows
  where its types come from. Name a file after the type it holds (or the group, for a file of related rules or
  notifications).
- Controllers stay thin: send a MediatR command or query. Rules live in handlers; follow-ups are notifications.
- Write characterization tests before refactoring, and integration tests against the real database (see
  `DraftPuck.Tests/Support`; seed your own data and use `Seed.NewGameId()`).
- Client logic goes in `helpers/` or `composables/` with tests next to it, not in components. Pick rules live only in
  `helpers/pickRules.ts`; the server sends them as facts on games and lobbies.
- Refactor to the design you'd choose from scratch, not to a slightly better version of the current pattern. Look at
  the architecture around a file before reworking the file.
- The server is the authority: validate every input in handlers, whatever the client checks.
- Keep security proportionate: it's a casual game holding no personal info. Do the basics that make a feature correct
  (e.g. a random, hashed, expiring, single-use reset token) and raise real holes (account takeover, missing auth), but
  don't add enterprise hardening like email verification or account-enumeration defenses.
- Don't use in-memory caches for shared state; the app can scale out. Redis or the database is the source of truth.
