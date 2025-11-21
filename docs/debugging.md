# Debugging & Test Mode

## Test Mode

DraftPuck allows simulating historical game states for debugging purposes.

Set environment variables:

```
DRAFTPUCK_APPLICATION__ISTESTMODE=true
DRAFTPUCK_APPLICATION__TestModeStartDateTimeUtc=<UTC Date>
```

## Tools

* **Azure Storage Explorer**: inspect local Azurite storage
* **Redis Insight**: monitor Redis queues and cache
* **MediatR Navigation (VS Extension)**: navigate commands/queries to handlers
* **Vue.js DevTools (Chrome Extension)**: debug frontend SPA

## Running Locally

* Use Docker Compose to start Web API, Worker, Redis, and Azurite
* Ensure `.env` variables are correctly set
* Check logs for errors in backend and worker

## Tips

* Always fetch upstream develop branch before creating new feature branches
* Use Visual Studio breakpoints in Application and Worker layers for business logic debugging
* Frontend hot reload is enabled via `npm run dev`
