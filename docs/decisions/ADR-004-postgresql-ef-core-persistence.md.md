# ADR-004: PostgreSQL and Entity Framework Core for Persistence

- **Status:** Accepted
- **Date:** 2026-10-16

## Context

The User Service needs a persistence mechanism to store and retrieve user data. The solution must preserve the separation between the domain model and infrastructure concerns established by ADR-003.

The persistence implementation must support relational data, asynchronous database operations, and the mapping of domain entities and value objects to database structures.

## Decision

We will use PostgreSQL as the relational database and Entity Framework Core (EF Core) as the object-relational mapper.

The persistence implementation will reside in the User Service Infrastructure layer.

The initial implementation will follow these conventions:

- `UserDbContext` will represent the EF Core database context.
- `UserConfiguration` will define the persistence mapping for the `User` entity.
- `EmailConverter` will convert the `Email` value object to and from its string representation.
- The Domain layer will remain independent of EF Core and PostgreSQL.
- Automated tests will validate the EF Core model metadata and mapping configuration.

## Consequences

### Positive

- PostgreSQL provides relational persistence and database constraints.
- EF Core provides entity mapping, change tracking, and asynchronous database operations.
- Explicit configuration makes persistence mappings easier to understand and maintain.
- Value converters allow the domain model to retain its `Email` value object without exposing database concerns to the Domain layer.
- Model configuration tests help detect mapping regressions.

### Trade-offs and follow-up work

- Database connectivity and dependency injection registration must be configured.
- Database migrations must be established.
- The repository implementation must enforce consistent email normalization and duplicate detection.
- Integration tests against a real PostgreSQL instance are needed to validate actual persistence behavior.

## Implementation status

The initial `UserDbContext`, entity configuration, email value converter, and model configuration tests have been implemented.

The repository, database connection, migrations, and PostgreSQL integration tests remain pending.
