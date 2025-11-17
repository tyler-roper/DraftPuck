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

Copy sample files:

```bash
cp env.development.list.sample env.development.list
cp env.web.development.list.sample env.development.list
cp CreateCertificate.ps1.sample CreateCertificate.ps1
```

Update `env.web.development.list`:

```
// The PFX_PASSWORD can be any string you choose
ASPNETCORE_Kestrel__Certificates__Default__Password=<PFX_PASSWORD>
```

Update `env.development.list`:

```
DRAFTPUCK_ConnectionStrings__DefaultConnection="Server=localhost;Database=DraftPuck;User Id=sa;Password=<SA_PASSWORD>;"
DRAFTPUCK_AUTH__JWTKEY=<JWT_KEY> // The JWT_KEY can be any string you choose (generating a GUID is recommended)
```

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
