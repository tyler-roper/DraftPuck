## What and why
<!-- What does this change, and why? Link the issue if there is one. -->

## How it was checked
- [ ] `dotnet test DraftPuck.Tests` passes (needs Docker)
- [ ] `npm test`, `npm run lint:check` and `npm run type-check` pass (in `DraftPuck.Web/ClientApp`), if the client changed
- [ ] New behaviour has tests: handlers against the real database, client logic in `helpers/` or `composables/`

## Also
- [ ] Includes a migration (`dotnet ef migrations add <Name> --project DraftPuck.Infrastructure --startup-project DraftPuck.Infrastructure`)
- [ ] Screenshots for visible changes (phone and desktop)
