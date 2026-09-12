# ADR-003: Adopt Clean Architecture for Internal Service Structure

- **Status:** Accepted
- **Date:** 2026-09-01
- **Decision Type:** Architecture
- **Scope:** SwiftOrders

## Context

SwiftOrders is composed of multiple independent microservices.

While microservices define the boundaries between independently deployable
services, each service still requires an internal architectural structure
that keeps business rules isolated from infrastructure and delivery
mechanisms.

The project is intended to follow good software engineering practices
while remaining suitable for a learning environment.

Without a clear internal architecture, business rules could become tightly
coupled to frameworks, databases, HTTP endpoints, or other infrastructure
concerns.

The initial implementation of `UserService` already separates the service
into the following projects:

- `UserService.API`
- `UserService.Application`
- `UserService.Domain`
- `UserService.Infrastructure`

A consistent architectural decision is therefore required to define the
responsibilities and dependency rules of these layers.

---

## Decision

Each SwiftOrders microservice will follow **Clean Architecture** as its
internal architectural structure.

The architecture will prioritize:

- Separation of concerns.
- Dependency inversion.
- Isolation of business rules.
- Framework independence of the domain.
- Testability.
- Explicit dependency direction.

The general structure is:

```text
                +----------------+
                |      API       |
                +----------------+
                        |
                        v
                +----------------+
                |  Application   |
                +----------------+
                        |
                        v
                +----------------+
                |     Domain     |
                +----------------+

                +----------------+
                | Infrastructure |
                +----------------+
                        |
                        v
                +----------------+
                |  Application   |
                |    / Domain    |
                +----------------+
```

The most important rule is that dependencies must point toward the
application's core.

## Architectural Layers

### Domain

The Domain layer contains the core business rules of the service.

Responsibilities include:

- Entities.
- Value Objects.
- Domain rules.
- Domain behavior.
- Business invariants.

The Domain layer must not depend on:

- ASP.NET Core.
- Entity Framework Core.
- Databases.
- HTTP.
- Controllers.
- External services.
- Infrastructure implementations.

The Domain should remain independent of technical infrastructure.

For example, User and Email belong to the Domain because they represent
business concepts and rules.

### Application

The Application layer coordinates application use cases.

Responsibilities include:

- Use cases.
- Application services.
- Commands and queries.
- DTOs and contracts where appropriate.
- Orchestration of domain operations.
- Abstractions required by the application.

The Application layer may depend on the Domain layer.

It must not contain infrastructure implementations.

For example, an application use case may retrieve a user through an
abstraction and then invoke domain behavior on the User entity.

### Infrastructure

The Infrastructure layer contains technical implementations required by the
application.

Responsibilities may include:

- Database access.
- Entity Framework Core.
- Repository implementations.
- External service clients.
- Authentication implementations.
- Password hashing implementations.
- Persistence configuration.
- Other framework-specific integrations.

Infrastructure may depend on the Application and Domain layers.

Infrastructure must implement abstractions defined by inner layers rather
than forcing those layers to depend on infrastructure technologies.

### API

The API layer is responsible for exposing the service to external clients.

Responsibilities include:

- HTTP endpoints.
- Controllers or Minimal API endpoints.
- HTTP request and response handling.
- Authentication and authorization integration.
- Dependency injection composition.
- HTTP-specific concerns.

The API layer must not contain core business rules.

Business behavior should be delegated to the Application and Domain layers.

## Dependency Rule

Dependencies must point inward.

Conceptually:

```text
API
 |
 v
Application
 |
 v
Domain

Infrastructure
 |
 +----> Application
 |
 +----> Domain
```

The Domain must remain independent.

The Application may depend on Domain abstractions and concepts.

Infrastructure and API are outer layers and may depend on inner layers.

Inner layers must not depend on outer layers.

For example:

```text
Allowed:

Infrastructure -> Application
Infrastructure -> Domain
Application    -> Domain
API            -> Application

Not allowed:

Domain        -> Infrastructure
Domain        -> API
Application   -> Infrastructure
Domain        -> ASP.NET Core
```

This rule is central to the architecture.

### Dependency Inversion

When an inner layer requires an external capability, the abstraction should
be defined inside an appropriate inner layer and implemented by an outer
layer.

For example:

```text
Application
    |
    | depends on
    v
IUserRepository
    ^
    |
    | implements
    |
Infrastructure
    |
    v
EF Core / Database
```

This allows the Application layer to depend on an abstraction instead of a
specific infrastructure technology.

The same principle can be applied to capabilities such as:

- Persistence.
- Password hashing.
- External service communication.
- Time providers.
- Other infrastructure-dependent operations.

## Framework Independence

The Domain layer should not depend directly on application frameworks.

For example, the following should not be required by the Domain:

```text
ASP.NET Core
Entity Framework Core
HTTP
SQL Server
PostgreSQL
Docker
Kubernetes
```

Frameworks may be used by outer layers where appropriate.

This keeps business rules independent from implementation details.

## Testing Strategy

Clean Architecture should allow the core business rules to be tested
without requiring infrastructure.

The Domain should therefore be testable using unit tests without:

- A database.
- An HTTP server.
- Docker.
- Kubernetes.
- External services.

Application tests may use mocks, fakes, or other test doubles for external
dependencies.

Infrastructure tests may use integration testing when interaction with real
technical components needs to be validated.

The intended testing structure is:

```text
Domain
  -> Unit Tests

Application
  -> Unit / Use Case Tests

Infrastructure
  -> Integration Tests

API
  -> API / Integration Tests
```

## Alternatives Considered

Traditional Layered Architecture

A conventional layered architecture was considered.

Although it provides separation between presentation, business, and data
access concerns, it does not necessarily enforce the same dependency
direction or dependency inversion principles.

Clean Architecture was selected because stronger dependency boundaries are
valuable for the project's goals.

## Simple Project Structure

Another alternative would be to keep each microservice as a single project
and organize the code using folders.

This would reduce the number of projects but would provide weaker compile-time
boundaries between architectural concerns.

It was rejected because the project explicitly aims to practice architectural
separation and maintainability.

## Vertical Slice Architecture

Vertical Slice Architecture was considered as an alternative organizational
model.

It can be effective for feature-oriented applications, but Clean Architecture
was selected for the initial implementation because the project currently
benefits more from explicit architectural layers and dependency boundaries.

Vertical slicing can be reconsidered in the future if the structure of the
services becomes feature-oriented enough to justify it.

## Consequences

### Positive Consequences

The architecture provides:

- Clear separation of responsibilities.
- Stronger business-rule isolation.
- Better testability.
- Reduced coupling to frameworks.
- Explicit dependency direction.
- Easier replacement of infrastructure implementations.
- Better maintainability as services grow.
- Practical experience with dependency inversion and SOLID principles.

It also establishes a consistent internal architecture that can be reused
across the SwiftOrders microservices.

### Negative Consequences

Clean Architecture introduces additional projects, abstractions, and
structural rules.

For small services, this can create more code and ceremony than a simpler
architecture would require.

Developers must also understand the dependency rules and avoid placing
business logic in outer layers.

These costs are accepted because architectural learning and maintainability
are important goals of SwiftOrders.

## Scope

This decision applies to the internal architecture of SwiftOrders
microservices.

The initial structure is:

```text
UserService
├── UserService.API
├── UserService.Application
├── UserService.Domain
└── UserService.Infrastructure
```

The same architectural principles should be applied to future services such
as:

```text
OrderService
OrderProcessingService
```

The exact internal organization of each service may evolve according to its
business complexity.

## Architectural Evolution

Clean Architecture does not require every service to contain the same number
of abstractions or implementations.

As a service grows, its internal structure may evolve.

The architecture should be reconsidered if:

- Layers become unnecessarily complex.
- Dependency boundaries no longer provide meaningful value.
- A service becomes better suited to another architectural style.
- Feature-based organization becomes more appropriate.
- Performance or operational requirements introduce new constraints.

Any significant architectural change should be documented through a new ADR
or by updating the status of this decision.

## Related Decisions

- [ADR-001: Adopt a Microservices Architecture](docs/decisions/ADR-001-microservices.md)
- [ADR-002: Use HTTP for Inter-Service Communication](docs/decisions/ADR-002-http-communication.md)
- [Architecture Overview](docs/architecture/overview.md)
- [Domain Architecture](docs/architecture/domain.md)
