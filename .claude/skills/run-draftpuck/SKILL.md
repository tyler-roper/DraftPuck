---
name: run-draftpuck
description: Launch an isolated copy of DraftPuck built from the working tree (Web + Worker in Docker, own SQL Server/Redis/Azurite), seed signed-in test players who are friends with lobbies and invites, and screenshot pages in headless Chrome. Use to see a change working in the real app, not just in tests.
---

# Run DraftPuck

`run.sh` (next to this file) runs a private copy of the app at **http://localhost:17100**. Everything is named
`draftpuck-run-*` and sits on its own Docker network, so it never touches the developer's own stack (their
`draftpuck-web`/`draftpuck-worker` containers, Redis on 6379, Azurite on 10000, or the SQL Server on 1433 with their
`DraftPuck` database). Don't stop or reuse those; don't run their `npm run dev`.

Run from the repo root in Git Bash. Docker Desktop must be running; Node 22.22+ must be on PATH (for `shot`).

```bash
bash .claude/skills/run-draftpuck/run.sh up      # build images from the working tree, start; ~5 min first time, ~1 min after
bash .claude/skills/run-draftpuck/run.sh seed    # sign up 4 players (unique names each run), friends, lobbies, an invite
bash .claude/skills/run-draftpuck/run.sh shot <email> <out-dir> "/" "/|#dropdownMenuButton" "/account/friends"
bash .claude/skills/run-draftpuck/run.sh logs    # Web + Worker logs
bash .claude/skills/run-draftpuck/run.sh down    # remove everything it started
```

- **`up` again after every code change**: the app runs from images, not from source (no hot reload). The Web image
  builds the client too (with type-check), so a client type error fails the build.
- **`seed`** prints the logins (password `draftpuck-run-1`). Sam is an admin (use them for `/admin/...` pages) and has
  a friend (Alex), a pending request (Jo), someone they've played with but not friended (Max, in Sam's lobby), an
  invite to Alex's lobby, and a lobby of their own. Data an admin page lists (event logos, etc.) isn't seeded: add it
  through the page or the admin API, or insert it with `docker exec draftpuck-run-sql /opt/mssql-tools18/bin/sqlcmd
  -C -I -S localhost -U sa -P DraftPuck-Run-1 -d DraftPuck -Q "..."` (`-I` is required for writes to some tables). Lobbies need tonight's NHL games, which the Worker loads from the
  real NHL API when it starts; on a day without games `seed` says so and skips the lobbies.
- **`shot`** signs in through the login page as that player and saves `01.png`, `02.png`, ... in `<out-dir>`
  (400×860 phone viewport). A step is a path, optionally `|` and a Playwright selector to click first:
  `"/lobby/ABCD|text=ABCD"` opens the share window, `"/|#dropdownMenuButton"` opens the avatar menu. It prints console
  errors and failing `/api` calls; one 401 on `refreshTokens/use` at the start is the pre-login session check, expected.
  It uses the installed Chrome via `playwright-core` (installed once into `$TMPDIR/draftpuck-run-tools`; no browser
  download). Pass Windows-style paths for `<out-dir>` (`cygpath -m`).
- **Look at the screenshots** (Read the PNGs) before calling a change verified.

## How it's put together (for fixing it)

- `up` writes `$TMPDIR/draftpuck-run.env` from the developer's `env.development.list` (keeps the JWT key, AutoMapper
  licence and Firebase settings) and overrides the database, Redis, storage, `BasePath` and test mode. That file holds
  their secrets: it stays outside the repo and `down` deletes it.
- The Web container runs as `Development` over plain HTTP (no HTTPS redirect); it applies migrations to a fresh
  `DraftPuck` database in `draftpuck-run-sql` on start. Data lasts until `down`.
- Git Bash quirks the script handles: `MSYS_NO_PATHCONV=1` (else `/opt/...` paths for `docker exec` get rewritten), and
  Windows-style paths (`pwd -W`, `cygpath -m`) for anything handed to `docker.exe` or `node.exe`.
- Shell scripts must have LF line endings (`.gitattributes`: `*.sh text eol=lf`); a CRLF `entrypoint.sh` makes the Web
  container exit with `exec /opt/app/entrypoint.sh: no such file or directory`.
- Pushes: seeded players register no devices, so nothing is pushed even though Firebase is configured.
