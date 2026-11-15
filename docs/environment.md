# Environment Setup

## Prerequisites

* Visual Studio 2026 (2022 can't build .NET 10 projects)
* .NET 10 SDK
* SQL Server: the one in Docker Compose, or your own. SSMS or Azure Data Studio to look at the data.
* Node.js 22.22+ or 24.15+, with npm (Vite 8 and the client tests need it; the Docker build uses Alpine's Node 22)
* Docker Desktop
* Optional: Azure Storage Explorer, Redis Insight, Vue DevTools, MediatR VS Extension

## SQL Server Setup

### In Docker (easiest)

```bash
docker compose --profile sql up -d sqlserver
```

It's opt-in (the `sql` profile), so a plain `docker compose up` or Visual Studio's launch doesn't start it; start it
once and it keeps running across restarts. The sample env file already points at it. The Web app creates the
`DraftPuck` database and applies migrations when it starts.

* **Query it** from SSMS or Azure Data Studio: server `localhost,14333`, SQL login `sa`, password
  `DraftPuck-Local-1`, and tick *Trust server certificate*. (Port 14333, so it doesn't clash with a SQL Server on 1433.)
* **Data persists** in the `sql_data` Docker volume through restarts and rebuilds. `docker compose down -v` deletes it
  (along with Redis's data).

### Your own SQL Server

1. Enable TCP/IP in SQL Server Configuration Manager
2. Restart SQL Server
3. Create database `DraftPuck`
4. Enable `sa` login and assign `db_owner` role
5. Set SQL Server to Mixed Authentication mode
6. Use the commented "your own SQL Server" connection strings in `env.development.list`

## Environment Variables

Copy sample files:

```bash
cp env.development.list.sample env.development.list
cp env.web.development.list.sample env.web.development.list
cp CreateCertificate.ps1.sample CreateCertificate.ps1
```

Update `env.web.development.list`:

```
// The PFX_PASSWORD can be any string you choose
ASPNETCORE_Kestrel__Certificates__Default__Password=<PFX_PASSWORD>
```

Update `env.development.list`:

```
DRAFTPUCK_AUTH__JWTKEY=<JWT_KEY> // The JWT_KEY can be any string you choose (generating a GUID is recommended)
```

Email (password reset links) is optional locally. Without `DRAFTPUCK_Email__ConnectionString` (an Azure
Communication Services connection string), emails are written to the log instead of sent, so you can copy a reset
link from the console.

Update `CreateCertificate.ps1`, using the same `PFX_PASSWORD` from `env.web.development.list`:

```powershell
$certificatePassword = "<PFX_PASSWORD>"
```

Now run the certificate script:

```powershell
.\CreateCertificate.ps1
```

## Frontend

```bash
cd DraftPuck.Web/ClientApp
npm install
npm run dev
```
