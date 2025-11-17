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

* Recommended: **Use Docker Compose** to build the Web, Worker, Redis, and Azurite"
```
cd path\to\DraftPuck
docker compose up
```

**or**

* **Run the Web project independently** in Visual Studio by selecting `DraftPuck.Web` as the startup project, and selecting the **Docker** launch profile
***(Note: You will have to run Azurite and Redis separately for most things to work)***

## Tips

* Always fetch upstream develop branch before creating new feature branches
* Use Visual Studio breakpoints in Application and Worker layers for business logic debugging
* Frontend hot reload is enabled via `npm run dev`
