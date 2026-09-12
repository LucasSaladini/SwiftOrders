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

### Development

Development documentation describes engineering practices used by the
project.

- [Testing Strategy](docs/development/testing.md)
