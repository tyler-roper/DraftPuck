# Architecture Overview

DraftPuck uses Clean Architecture principles with separation of concerns.

## Projects

* **DraftPuck.Domain**: Core business logic, entities, value objects, domain events
* **DraftPuck.Application**: Use cases, MediatR handlers, DTOs, validation, interfaces
* **DraftPuck.Infrastructure**: EF Core, SQL Server, Redis, Azure Blob Storage, external clients
* **DraftPuck.Web**: API controllers, dependency injection, authentication
* **DraftPuck.Worker**: Background processing, queues, notifications

## Layers

* **Web/API** → Application → Domain → Infrastructure
* **Worker** → Application → Domain → Infrastructure

This structure allows easy testing, maintainability, and separation of business logic from technical details.