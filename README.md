# DraftPuck

DraftPuck is a containerized .NET 8 application with a Vue3 front-end, designed to track NHL-related game data in real time and react accordingly. The project follows Clean Architecture principles and uses MediatR, Entity Framework Core, Redis, and Azure Functions for backend processing.

This repository is intended to allow developers to quickly get started with local development and understand the overall architecture.

---

## Prerequisites

Before running the project, make sure you have the following installed:

* [Visual Studio 2022 or later](https://visualstudio.microsoft.com/)
* [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
* [SQL Server](https://www.microsoft.com/en-us/sql-server)
* [Node.js & npm](https://nodejs.org/)
* [Docker Desktop](https://www.docker.com/products/docker-desktop)

**Recommended for improved debugging and visibility:**

* [Azure Storage Explorer](https://azure.microsoft.com/en-us/features/storage-explorer/)
* [SQL Server Management Studio](https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms)
* [Redis Insight](https://redis.com/redis-enterprise/redis-insight/)
* [MediatR Navigation Extension](https://marketplace.visualstudio.com/items?itemName=YuriiChornyi.mediatr-navigation-extension-2022)
* [Vue.js Devtools](https://chrome.google.com/webstore/detail/vuejs-devtools/nhdogjmejiglipccpnnnanhbledajbpd) (Chrome Extension)

---

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/your-username/DraftPuck.git
cd DraftPuck
```

### 2. Configure SQL Server

1. Open **SQL Server Configuration Manager**.
2. Go to **SQL Server Network Configuration → Protocols for MSSQLSERVER**.
3. Right-click on **TCP/IP** and select **Enable**.
4. Restart SQL Server if required.

### 3. Create Database and Configure `sa` User

1. Open SQL Server Management Studio (SSMS) and connect to your server.
2. Create a new database named `DraftPuck`.
3. Ensure the `sa` login is enabled:

   * Expand **Security → Logins**
   * Right-click **sa → Properties**
   * Set **Password** and ensure **SQL Server Authentication** is selected.
   * Under **User Mapping**, check your `DraftPuck` database and assign `db_owner` role.
4. Ensure **SQL Server authentication** is enabled if:
  
   * At the top of the side bar, Right click **<your server name> → Properties**
   * Under **Security**, enable **SQL Server and Windows Authentication mode**

### 4. Environment Configuration

1. Copy the sample environment file:

```bash
cp env.development.list.sample env.development.list
```

2. Update the required values:

```
DRAFTPUCK_ASPNETCORE_Kestrel__Certificates__Default__Password=<PFX_PASSWORD>
DRAFTPUCK_ConnectionStrings__DefaultConnection="Server=localhost;Database=DraftPuck;User Id=sa;Password=<SA_PASSWORD>;"
DRAFTPUCK_AUTH__JWTKEY=<JWT_KEY>
```

* Use any string for `PFX_PASSWORD`.
* Replace `<SA_PASSWORD>` with the password you configured for `sa`.
* Generate a GUID for `JWT_KEY` (e.g., [GUID Generator](https://www.guidgenerator.com/)).

3. Update `CreateCertificate.ps1` in the root directory with the same `PFX_PASSWORD`.

4. Run the PowerShell script to generate a local trusted SSL certificate:

```powershell
.\CreateCertificate.ps1
```

### 5. Install Front-End Dependencies

```bash
cd DraftPuck.Web/ClientApp
npm install
```

---

## Running the Application

The project consists of **backend (.NET 8 API + Worker)** and **frontend (Vue3)**.

### Options for Running the Backend

* **Full Application (Recommended)**:

  * In Visual Studio, set `docker-compose` as the startup project with the `Docker Compose` launch profile.
  * This builds the Web project, Worker project, and spins up Azurite and Redis containers.

* **Backend Only**:

  * In Visual Studio, set `DraftPuck.Web` as the startup project with the `Docker` launch profile.
  * Manually start Azurite and Redis containers if needed.

### Frontend

Run the front-end in development mode with hot-reloads:

```bash
cd DraftPuck.Web/ClientApp
npm run dev
```

Visit `http://localhost:17010` to see the application.

---

## Architecture Overview

* **Clean Architecture Principles**:

  * `Web` – API + ClientApp
  * `Application` – business logic, MediatR patterns, EF Core DbSets as repositories
  * `Infrastructure` – data persistence, external integrations
  * `Shared` – reusable utilities
  * `Worker` – background processing (Redis queues, notifications)

* **MediatR**:

  * Implements the CQRS pattern (Commands & Queries) to decouple request handling from business logic.

* **Authentication**:

  * Short-lived JWTs, refresh tokens, CSRF tokens.

* **Other Infrastructure**:

  * **Redis** – caching, locks, queues, notification flags.
  * **Azure Functions** – NHL polling, lobby expiration, achievement processing.
  * **AutoMapper** – optional but recommended.
  * **Firebase** – optional for local push notification testing.

---

## Debugging and Test Mode

* Test mode is enabled via:

```
DRAFTPUCK_APPLICATION__ISTESTMODE=
DRAFTPUCK_APPLICATION__TestModeStartDateTimeUtc=
```

* This allows you to simulate historical game states for debugging.

---

## Contributing

1. Fork the repository
2. Create a feature branch
3. Open a pull request
4. Ensure your environment variables and database are correctly configured before submitting changes.

---

## Community

Have questions or need help? Join our Discord community to discuss issues, ask questions, and collaborate with other developers. Or just hang out.

[Join the DraftPuck Discord](https://discord.gg/8xqnqs35)

---

## License

MIT License. See `LICENSE` for details.