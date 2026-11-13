# Contributing to DraftPuck

Thank you for your interest in contributing!

## Branch Strategy
- **main** → production
- **develop** → integration branch (PR target)
- **feature/*** → contributor feature branches

All pull requests must target **develop**.

## Fork & Clone Workflow
1. Fork the repository: https://github.com/tyler-roper/DraftPuck
2. Clone your fork:
```bash
git clone https://github.com/<your-username>/DraftPuck.git
cd DraftPuck
```
3. Add upstream remote:
```bash
git remote add upstream https://github.com/tyler-roper/DraftPuck.git
```

## Create a Feature Branch
```bash
git checkout develop
git pull upstream develop
git checkout -b feature/my-feature
```

## Commit & Push Changes
```bash
git add .
git commit -m "Add feature description"
git push origin feature/my-feature
```

## Open a Pull Request
- Base branch: `develop`
- Compare branch: your feature branch

## Code Style & Testing
- Use C# primary constructors
- Follow `.editorconfig`
- Run tests locally before PR

### Running tests
Every pull request runs both suites in GitHub Actions, and a deploy to `main` only proceeds if they pass.

**Server** (`DraftPuck.Tests`, xUnit) needs Docker running: the tests start SQL Server and Redis containers
(Testcontainers) and apply the real migrations, so queries are tested exactly as they run in production.
```bash
dotnet test DraftPuck.Tests
```
- `Application/` tests handlers against the database, with other services faked using NSubstitute.
- `Infrastructure/` tests the Redis game cache and NHL mapping.
- `Web/` sends real HTTP requests through the app (auth, filters, SignalR) with `ApiFactory`.
- `Support/` has the fixtures and seed helpers. Tests share one database, so seed your own lobbies and use
  `Seed.NewGameId()` rather than fixed IDs.

**Client** (Vitest):
```bash
cd DraftPuck.Web/ClientApp
npm test            # or: npm run test:watch
```
Put specs in a `__tests__` folder next to the code, named `*.spec.ts`.