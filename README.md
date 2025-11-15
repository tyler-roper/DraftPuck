# DraftPuck

DraftPuck is a containerized .NET 8 application with a Vue 3 frontend, built using Clean Architecture principles and a MediatR CQRS pattern. This repository contains everything needed to run the backend API, background worker, frontend, and local infrastructure dependencies such as Redis and Azurite.

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

- **Using Docker Compose (recommended)**: Set the startup project in Visual Studio to `docker-compose` and use the Docker Compose launch profile. This starts DraftPuck.Web, DraftPuck.Worker, Redis, and Azurite.
- **Running Web API Only**: Set `DraftPuck.Web` as the startup project with the Docker launch profile.

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
- **Infrastructure** — SQL Server, Redis, Blob storage, integrations
- **Shared** — cross-cutting helpers and abstractions
- **Worker** — background processing, queues, notifications
Full details in [`docs/architecture.md`](./docs/architecture.md)

---

## Community
Join our Discord: https://discord.gg/8xqnqs35

---

## License
MIT License (see LICENSE file)