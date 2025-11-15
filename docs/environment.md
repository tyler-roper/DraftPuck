# Environment Setup

## Prerequisites

* Visual Studio 2022+
* .NET 8 SDK
* SQL Server & SSMS
* Node.js & npm
* Docker Desktop
* Optional: Azure Storage Explorer, Redis Insight, Vue DevTools, MediatR VS Extension

## SQL Server Setup

1. Enable TCP/IP in SQL Server Configuration Manager
2. Restart SQL Server
3. Create database `DraftPuck`
4. Enable `sa` login and assign `db_owner` role
5. Set SQL Server to Mixed Authentication mode

## Environment Variables

Copy sample file:

```bash
cp env.development.list.sample env.development.list
```

Update:

```
DRAFTPUCK_ASPNETCORE_Kestrel__Certificates__Default__Password=<PFX_PASSWORD>
DRAFTPUCK_ConnectionStrings__DefaultConnection="Server=localhost;Database=DraftPuck;User Id=sa;Password=<SA_PASSWORD>;"
DRAFTPUCK_AUTH__JWTKEY=<GUID>
```

Run certificate script:

```powershell
.\CreateCertificate.ps1
```

## Frontend

```bash
cd DraftPuck.Web/ClientApp
npm install
npm run dev
```
