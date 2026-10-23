# SwiftOrders

## What is it

SwiftOrders is a learning project focused on building a distributed order
management system using modern software architecture and development
practices.

The project is being developed with a microservices architecture, where each
service is independently responsible for a specific business capability.

The initial implementation focuses on building the foundation of the
architecture before introducing additional services and infrastructure
complexity.

---

## Project Objectives

The main objectives of SwiftOrders are:

- Practice microservices architecture.
- Apply Clean Architecture.
- Apply Domain-Driven Design concepts.
- Practice SOLID principles.
- Build services using C# and .NET.
- Implement communication between services using HTTP.
- Practice automated testing with xUnit.
- Learn containerization with Docker.
- Learn container orchestration with Kubernetes.
- Understand distributed-system concepts.
- Develop maintainable and testable software.
- Build a project that evolves progressively from a simple foundation to a
  more complete distributed system.

The project intentionally avoids unnecessary infrastructure during the early
stages of development.

---

## Architecture

SwiftOrders uses a **microservices architecture**.

Each microservice is an independently deployable application with its own
business responsibilities and internal architecture.

Inside each service, **Clean Architecture** is used to separate business
rules from application, infrastructure, and API concerns.

The general structure is:

```text
                         SwiftOrders

                             |

          +------------------+------------------+

          |                  |                  |

          v                  v                  v

    UserService        OrderService      OrderProcessing
                                              Service

          Each service follows Clean Architecture

                    +-------------+

                    |     API     |

                    +-------------+

                           |

                           v

                    +-------------+

                    | Application |

                    +-------------+

                           |

                           v

                    +-------------+

                    |   Domain    |

                    +-------------+

                    +-------------+

                    |Infrastructure|

                    +-------------+
```

The main architectural principles are:

- Service independence.
- Separation of concerns.
- Dependency inversion.
- Domain isolation.
- Explicit service boundaries.
- Testability.

For more details, see the architecture documentation and Architecture
Decision Records.

## Tech Stack

The project uses or plans to use the following technologies:

### Backend

- C#
- .NET 10
- ASP.NET Core

### Testing

- xUnit

### Infrastructure

- Docker
- Kubernetes

### Communication

- HTTP
- REST APIs

### Architecture and Development Practices

- Microservices
- Clean Architecture
- Domain-Driven Design
- SOLID
- Result Pattern
- Automated Testing

Technologies are introduced progressively as the corresponding project
requirements are implemented.

## Microservices

The initial SwiftOrders architecture contains three planned microservices.

### UserService

Responsible for user-related functionality.

Current domain concepts include:

- User
- Email
- User lifecycle management
- Password management

The service contains the initial implementation of the project's domain
model and is being used to establish the architectural foundation.

### OrderService

Responsible for order-related functionality.

Its implementation will be introduced progressively after the initial
service architecture has been established.

### OrderProcessingService

Responsible for order processing workflows.

Its implementation will also be introduced progressively.

### Service Communication

Services communicate through HTTP APIs.

The initial architecture intentionally does not use message brokers or
event-driven communication.

## Solution Structure

The current solution is organized as follows:

```text
SwiftOrders/
│
├── src/
│   ├── BuildingBlock/
│   │   └── BuildingBlocks/
│   │       ├── Results/
│   │       │   ├── Error.cs
│   │       │   ├── Result.cs
│   │       │   └── ResultOfT.cs
│   │       └── BuildingBlocks.csproj
│   │
│   └── UserService/
│       ├── UserService.API/
│       ├── UserService.Application/
│       ├── UserService.Domain/
│       │   ├── Abstractions/
│       │   │   └── IClock.cs
│       │   ├── Entities/
│       │   │   └── User.cs
│       │   └── ValueObjects/
│       │       └── Email.cs
│       │
│       └── UserService.Infrastructure/
│           └── Time/
│               └── SystemClock.cs
│
├── tests/
│   └── UserService.Tests/
│
├── docs/
│   ├── architecture/
│   │   ├── overview.md
│   │   └── domain.md
│   │
│   ├── decisions/
│   │   ├── ADR-001-microservices.md
│   │   ├── ADR-002-http-communication.md
│   │   └── ADR-003-clean-architecture.md
|   |   └── ADR-004-postgresql-ef-core-persistence.md
│   │
│   └── development/
│       └── testing.md
│
├── SwiftOrders.slnx
└── README.md
```

The solution structure will evolve as new microservices and infrastructure
components are implemented.

## How to Run

### Prerequisites

The initial development environment requires:

- .NET 10 SDK
- Git

Docker and Kubernetes will be required as those parts of the project are
introduced.

### Clone the repository

```bash
git clone <repository-url>
cd SwiftOrders
```

### Build the solution

```bash
dotnet build SwiftOrders.slnx
```

### Run the UserService

The API project can be started using:

```bash
dotnet run --project src/UserService/UserService.API
```

The API will start using the configuration defined by the ASP.NET Core
application.

The exact endpoints and development URLs may change as the API evolves.

## How to Test

The project uses xUnit for automated testing.

Tests are located under:

```text
tests/
└── UserService.Tests/
```

To execute the test suite:

```text
dotnet test SwiftOrders.slnx
```

The testing strategy follows the project's Clean Architecture.

The initial focus is on testing domain behavior and business rules.

As the project evolves, tests will progressively cover:

- Domain behavior.
- Application use cases.
- Infrastructure integrations.
- API behavior.
- Inter-service communication.

For more information, see the [Testing Strategy](docs/development/testing.md)

## Documentation

The project documentation is organized into three main areas.

### Architecture Docs

Architecture documentation describes the structure and responsibilities of
the system.

- [Architecture Overview](docs/architecture/overview.md)
- [Domain Architecture](docs/architecture/domain.md)

### Architecture Decision Records

ADRs document important architectural decisions and their reasoning.

- [ADR-001: Adopt a Microservices Architecture](docs/decisions/ADR-001-microservices.md)
- [ADR-002: Use HTTP for Inter-Service Communication](docs/decisions/ADR-002-http-communication.md)
- [ADR-003: Adopt Clean Architecture for Internal Service Structure](docs/decisions/ADR-003-clean-architecture.md)
- [ADR-004: Postgresql and EFCore Persistence.md.md](docs/decisions/ADR-004-postgresql-ef-core-persistence.md.md)

### Development

Development documentation describes engineering practices used by the
project.

- [Testing Strategy](docs/development/testing.md)

## Local Development

### Prerequisites For Local Development

Before running SwiftOrders locally, ensure you have:

- .NET 10 SDK.
- Docker with the local PostgreSQL container running.
- PostgreSQL databases named `swiftorders_dev` and `swiftorders_test`.
- The database credentials configured through .NET User Secrets.

### Database Environments

SwiftOrders separates development and automated testing databases:

| Environment | Database | Purpose |
| --- | --- | --- |
| Development | `swiftorders_dev` | Local API development and manual validation |
| Testing | `swiftorders_test` | Automated integration tests |
| Production | Deployment-managed | Reserved for deployed environments |

**Never run local development or automated tests against the production database.**

### Configure User Secrets

Database connection strings and credentials must not be committed to source control.

The API project uses .NET User Secrets to store local connection settings. From the repository root, configure the development connection:

```bash
dotnet user-secrets set \
  "ConnectionStrings:DevDatabase" \
  "Host=localhost;Port=5432;Database=swiftorders_dev;Username=YOUR_DB_USER;Password=YOUR_DB_PASSWORD" \
  --project src/UserService/UserService.API/UserService.API.csproj
```

Configure the test connection separately:

```bash
dotnet user-secrets set \
  "ConnectionStrings:TestDatabase" \
  "Host=localhost;Port=5432;Database=swiftorders_test;Username=YOUR_DB_USER;Password=YOUR_DB_PASSWORD" \
  --project src/UserService/UserService.API/UserService.API.csproj
```

Replace the username and password placeholders with your local PostgreSQL credentials. Do not commit credentials or share the output of dotnet user-secrets list.

User Secrets are intended for local development and are not a production secrets-management solution.

### Select an Environment

Use the environment script from the repository root:

```bash
./scripts/db-env.sh
```

The script offers the following actions:

1. Development — starts the UserService API using the DevDatabase connection.
2. Automated tests — runs dotnet test SwiftOrders.slnx using the TestDatabase connection.
3. Production — blocked for local execution.
0. Exit — exits without running an action.

The script reads the connection strings from User Secrets without printing their values. It also validates the expected database name before executing the selected action.

### Run the API

Select option 1 in the environment script. The API runs in the Development environment.

When available, the Scalar API reference can be used to inspect and manually test the HTTP endpoints.

If the configured local port is already in use, check whether another API instance is running before starting another one.

### Run Automated Tests

Select option 2 in the environment script. The script exports the test connection through SWIFTORDERS_TEST_CONNECTION for the duration of the test process.

The test suite includes unit and integration tests. Integration tests that require PostgreSQL use swiftorders_test; health-check tests can simulate database unavailability without stopping PostgreSQL.

### Database Migrations

Database migrations must be applied deliberately to the intended environment. The environment script does not apply migrations automatically.

Before applying a migration, verify the target database and ensure that the operation is appropriate for that environment.
