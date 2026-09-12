# SwiftOrders Architecture Overview

## 1. Overview

**SwiftOrders** is a learning-oriented distributed system designed to
practice software architecture and backend development using
microservices, Clean Architecture, SOLID principles, Domain-Driven Design
(DDD) concepts, automated testing, Docker, and Kubernetes.

The system is designed around independent services that communicate through
HTTP.

The initial architecture consists of three microservices:

- **UserService** — responsible for users, authentication-related
  operations, and user lifecycle.
- **OrderService** — responsible for orders and order-related business
  operations.
- **OrderProcessingService** — responsible for processing and managing the
  order processing workflow.

Messaging and event-driven communication are intentionally outside the
initial scope of the project. The services communicate synchronously through
HTTP.

---

## 2. Architectural Goals

The main goals of the project are:

- Practice microservices architecture.
- Apply Clean Architecture principles.
- Apply SOLID principles in a real project.
- Practice Domain-Driven Design concepts.
- Maintain clear separation of responsibilities.
- Minimize coupling between components and services.
- Create independently testable components.
- Practice containerization with Docker.
- Practice orchestration with Kubernetes.
- Build a foundation that can evolve as the project grows.

The project is also intended as a learning environment, therefore some
architectural decisions prioritize clarity and learning value over
unnecessary complexity.

---

## 3. Microservices

### 3.1 UserService

The `UserService` is responsible for user-related functionality.

Its initial domain includes:

- User identity.
- Email management.
- Password management.
- User activation and deactivation.
- User reactivation.
- Soft deletion.
- User details.

The service follows a layered architecture composed of:

- API
- Application
- Domain
- Infrastructure

The Domain layer contains the business rules and entities related to users.

---

### 3.2 OrderService

The `OrderService` will be responsible for the order domain.

Its responsibilities will include operations such as:

- Creating orders.
- Retrieving orders.
- Updating order information.
- Managing order state.
- Applying order-related business rules.

The exact domain model will be defined as development progresses.

---

### 3.3 OrderProcessingService

The `OrderProcessingService` will be responsible for the processing workflow
of orders.

Its responsibilities will include:

- Receiving orders that require processing.
- Executing order processing rules.
- Managing processing states.
- Communicating processing results through HTTP.

The service is intentionally separated from `OrderService` to allow the
processing workflow to evolve independently from the order management
domain.

---

## 4. Communication Between Services

The initial version of SwiftOrders uses **HTTP-based synchronous
communication** between microservices.

Example:

```text
+-------------+       HTTP        +-------------+
| UserService | <---------------- | OrderService|
+-------------+                   +-------------+
                                         |
                                         | HTTP
                                         v
                                +----------------------+
                                | OrderProcessing      |
                                | Service              |
                                +----------------------+
```

No message broker or asynchronous messaging infrastructure is part of the
initial architecture.

This decision keeps the initial system easier to understand while allowing
the communication strategy to evolve in the future.

## 5. Internal Architecture

Each microservice follows a layered architecture inspired by
Clean Architecture.

The main layers are:

```text
+----------------------+
|         API          |
+----------------------+
           |
           v
+----------------------+
|     Application      |
+----------------------+
           |
           v
+----------------------+
|        Domain        |
+----------------------+
           ^
           |
+----------------------+
|    Infrastructure    |
+----------------------+
```

The dependency direction is designed to keep business rules independent
from technical implementation details.

### API

The API layer is responsible for communication with external clients.

Typical responsibilities include:

- HTTP endpoints.
- Request handling.
- Response handling.
- Authentication and authorization configuration.
- Middleware.
- Dependency injection configuration.

The API should not contain core business rules.

### Application

The Application layer coordinates use cases.

Its responsibilities include:

- Application use cases.
- Orchestration of domain operations.
- Coordination between domain objects and infrastructure abstractions.
- Application-level validation.
- Authorization rules related to use cases.

The Application layer should depend on abstractions rather than concrete
infrastructure implementations whenever possible.

### Domain

The Domain layer contains the core business rules.

It includes concepts such as:

- Entities.
- Value Objects.
- Domain rules.
- Domain behaviors.

The Domain should remain independent of:

- ASP.NET Core.
- Entity Framework Core.
- PostgreSQL.
- HTTP infrastructure.
- External services.
- Specific cryptographic implementations.

This layer represents the business rather than the technology used to
implement the business.

### Infrastructure

The Infrastructure layer contains technical implementations.

Examples include:

- Database access.
- Entity Framework Core configurations.
- PostgreSQL integration.
- Password hashing implementations.
- External service integrations.
- Implementations of interfaces defined by inner layers.

Infrastructure details should not leak into the Domain.

## 6. Dependency Direction

The architecture follows the principle that business rules should not
depend on technical details.

Conceptually:

```text
API
 |
 v
Application
 |
 v
Domain
 ^
 |
Infrastructure
```

Infrastructure implements abstractions required by the Application or
Domain without forcing the Domain to depend on the implementation.

For example, password hashing should be represented through an abstraction
rather than making the Domain depend directly on a specific hashing
library.

## 7. Domain-Driven Design

SwiftOrders uses selected Domain-Driven Design concepts.

The initial concepts include:

- Entities.
- Value Objects.
- Encapsulation of business behavior.
- Domain invariants.
- Bounded responsibilities per service.

For example, User is an Entity because it has a unique identity and a
lifecycle.

Email is modeled as a Value Object because its identity is determined by
its value and it encapsulates email-related validation.

DDD concepts will be introduced progressively as the project evolves.

The project intentionally avoids introducing patterns solely for the sake
of complexity.

## 8. SOLID Principles

The project aims to apply SOLID principles throughout its implementation.

Examples include:

### Single Responsibility Principle

Each class and service should have a clearly defined responsibility.

### Open/Closed Principle

Components should be designed to allow extension without unnecessary
modification of existing behavior.

### Liskov Substitution Principle

Abstractions should be designed so implementations can be substituted
without violating expected behavior.

### Interface Segregation Principle

Interfaces should represent focused contracts instead of forcing
implementations to depend on unnecessary members.

### Dependency Inversion Principle

High-level business logic should depend on abstractions rather than
concrete infrastructure implementations.

## 9. Result Pattern

The project contains a shared BuildingBlocks project with a Result
abstraction.

The Result pattern is intended to represent expected operation outcomes
without using exceptions as the primary mechanism for normal business
failures.

Conceptually:

```text
Operation
   |
   +---- Success
   |
   +---- Failure
          |
          +---- Code
          +---- Message
```

Generic and reusable result-related concepts belong to the shared
BuildingBlocks project, while domain-specific errors should remain close to
the domain or service that owns them.

## 10. Testing Strategy

Each microservice will have its own test project.

The initial testing strategy prioritizes unit tests for domain behavior.

For example:

```text
UserService
    |
    +-- Domain
    |
    +-- UserService.Tests
            |
            +-- UserTests
            +-- EmailTests
```

Tests should verify business rules independently from external
infrastructure whenever possible.

The project will progressively introduce:

- Unit tests.
- Application tests.
- Integration tests.
- API tests.

The exact testing strategy will evolve as the system becomes more complex.

## 11. Containerization and Deployment

Docker will be used to containerize the microservices and their supporting
infrastructure.

Kubernetes will be introduced later to practice:

- Container orchestration.
- Service deployment.
- Configuration management.
- Service discovery.
- Scaling.
- Health checks.

These concerns are intentionally separated from the initial domain
development.

## 12. Current Scope

The initial implementation focuses on establishing a strong foundation for
the `UserService`.

Current focus:

```text
SwiftOrders
    |
    +-- UserService
          |
          +-- Domain
          |     |
          |     +-- User
          |     +-- Email
          |
          +-- Application
          +-- Infrastructure
          +-- API
```

The `OrderService` and `OrderProcessingService` will be implemented
incrementally after the initial UserService foundation is established.

## 13. Architectural Evolution

SwiftOrders is intentionally designed to evolve.

Architectural decisions may change as new requirements and technical
challenges are introduced.

When an important architectural decision is made, it should be documented
as an Architecture Decision Record (ADR) under:

```text
docs/decisions/
```

This allows the project to document not only what was implemented, but
also why a particular architectural decision was made.
