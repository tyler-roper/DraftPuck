# DraftPuck

DraftPuck is a containerized .NET 10 application with a Vue 3 frontend, built using Clean Architecture principles and a MediatR CQRS pattern. This repository contains everything needed to run the backend API, background worker, frontend, and local infrastructure dependencies such as Redis and Azurite.

This README provides a high-level overview and quickstart instructions.  
For full development setup and contributor workflows, see:  
➡️ **[CONTRIBUTING.md](./CONTRIBUTING.md)**  
➡️ **[docs/architecture.md](./docs/architecture.md)**  
➡️ **[docs/environment.md](./docs/environment.md)**  
➡️ **[docs/debugging.md](./docs/debugging.md)**

---

## Quick Start

### Clone the Repository
```bash
git clone https://github.com/tyler-roper/DraftPuck.git
cd DraftPuck
```

### Backend
The backend can be run either:

- **Using Docker Compose (recommended)**: Set the startup project in Visual Studio to `docker-compose` and use the Docker Compose launch profile. This starts DraftPuck.Web, DraftPuck.Worker, Redis, and Azurite on one Docker network. The `docker-compose` project isn't in `DraftPuck.sln`: add it once with **Add → Existing Project → `docker-compose.dcproj`**.
- **Without Visual Studio**: `docker compose up --build` from the repository root starts the same containers (without the debugger attached).

Don't run `DraftPuck.Web` on its own with its Docker launch profile: that container isn't on the Compose network, so it can't reach Redis (`redis:6379`) and every request times out.

SQL Server: start the one in Compose once with `docker compose --profile sql up -d sqlserver` (it keeps running, and its data persists), or use your own (see [environment.md](./docs/environment.md)). The Web app creates the database and applies migrations at startup, and won't start if it can't.

### Frontend (Vue 3)
```bash
cd DraftPuck.Web/ClientApp
npm install
npm run dev
```
Visit: `https://localhost:17010`

---

## Architecture Summary
DraftPuck uses a Clean Architecture structure:
- **Web** — API + Vue 3 SPA
- **Application** — business logic, commands/queries, MediatR
- **Infrastructure** — SQL Server, Redis, Blob storage, external integrations
- **Domain** — the database entities and the enums they use
- **Worker** — Azure Functions: loading the daily schedule, polling games, awarding achievements, deactivating stale lobbies
- **DiscordBot** — the Discord bot

See [docs/architecture.md](./docs/architecture.md) for how requests, games and real-time updates flow, and where new code goes.

---

## Community
Join our Discord: https://discord.gg/8xqnqs35

---

## License
MIT License (see LICENSE file)