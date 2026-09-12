# ADR-001: Adopt a Microservices Architecture

- **Status:** Accepted
- **Date:** 2026-09-01
- **Decision Type:** Architecture
- **Scope:** SwiftOrders

## Context

SwiftOrders is a learning-oriented project designed to practice modern
software architecture and backend development.

The project aims to explore concepts such as:

- Microservices architecture.
- Clean Architecture.
- SOLID principles.
- Domain-Driven Design (DDD).
- Automated testing.
- Docker.
- Kubernetes.

The system contains multiple business domains with different
responsibilities, including users, orders, and order processing.

Keeping these responsibilities separated provides an opportunity to study
how independently deployable services can be designed and maintained.

At the same time, introducing microservices adds operational and
architectural complexity.

Therefore, the architecture must be intentionally scoped to avoid
introducing distributed-system complexity that is not necessary for the
current learning objectives.

---

## Decision

We will adopt a **microservices architecture** for SwiftOrders.

The initial system will be divided into three independent microservices:

```text
+----------------------+
|     UserService      |
|                      |
| Users & User Domain  |
+----------------------+

+----------------------+
|     OrderService     |
|                      |
| Order Domain         |
+----------------------+

+-----------------------------+
|  OrderProcessingService     |
|                             |
| Order Processing Workflow   |
+-----------------------------+
```

Each microservice will own a specific business responsibility and will have
its own application boundary.

The services will be developed as independent .NET applications and will
communicate through explicitly defined contracts.

## Service Responsibilities

### UserService

Responsible for user-related functionality, including:

- User identity.
- Email management.
- Password management.
- User lifecycle.
- User details.
- Activation and deactivation.

### OrderService

Responsible for the order domain, including:

- Order creation.
- Order management.
- Order state.
- Order-related business rules.

### OrderProcessingService

Responsible for the order processing workflow, including:

- Processing orders.
- Managing processing states.
- Executing processing-related business rules.

The exact responsibilities of each service may evolve as the domain becomes
better understood.

### Communication Strategy

The initial communication mechanism between microservices will be HTTP.

Synchronous HTTP communication was chosen because it provides a simple and
explicit communication model while the project is being developed.

Message brokers and asynchronous messaging are intentionally excluded from
the initial scope.

This decision may be revisited in the future if the system introduces
requirements that justify asynchronous communication.

### Service Independence

Each microservice should maintain a clear boundary around its own domain.

Services should avoid sharing domain entities or tightly coupling their
internal implementations.

Communication between services should occur through contracts rather than
direct access to another service's internal implementation.

Conceptually:

```text
+-------------+       HTTP       +-------------+
| UserService | <--------------> | OrderService|
+-------------+                  +-------------+
                                       |
                                       | HTTP
                                       v
                              +----------------------+
                              | OrderProcessing      |
                              | Service              |
                              +----------------------+
```

The internal implementation of one service must not become a dependency of
another service.

## Why Microservices?

The architecture was chosen for the following reasons:

### 1. Separation of Business Domains

Each service can focus on a specific business capability.

This makes domain boundaries explicit and provides a practical environment
for studying bounded contexts and DDD concepts.

### 2. Independent Development

Services can evolve independently as long as their external contracts remain
compatible.

### 3. Independent Deployment

Each service can eventually be built, tested, containerized, and deployed
independently.

### 4. Independent Scaling

Different services may have different resource requirements.

For example, an order processing service could potentially require more
resources than a user management service.

### 5. Architectural Learning

Microservices provide a practical environment for studying distributed
systems, service boundaries, API contracts, observability, containerization,
and orchestration.

## Trade-offs

Adopting microservices also introduces significant complexity.

### Advantages

- Clear service boundaries.
- Independent deployment.
- Independent scaling.
- Technology and implementation isolation.
- Better alignment with bounded business domains.
- Practical experience with distributed systems.

### Disadvantages

- Increased operational complexity.
- Network communication between services.
- More difficult debugging.
- Distributed failure scenarios.
- Increased deployment complexity.
- More infrastructure requirements.
- Data consistency becomes more challenging.
- API versioning and contract management become important.

For this project, these trade-offs are considered acceptable because
distributed-system concepts are part of the learning objectives.

## Alternatives Considered

### Monolithic Architecture

A traditional monolithic architecture would keep all business domains in a
single application.

#### Monolith Advantages

- Simpler deployment.
- Simpler local development.
- Easier communication between components.
- Easier transaction management.
- Lower operational complexity.

#### Monolith Disadvantages

- Less explicit service boundaries.
- Less opportunity to practice distributed systems.
- Does not provide the same experience with independent service deployment
- and communication.

A monolith was rejected because it would not provide the architectural
learning opportunities that are central to SwiftOrders.

## Consequences

### Positive Consequences

The project will provide practical experience with:

- Service boundaries.
- HTTP-based service communication.
- API contracts.
- Distributed application design.
- Independent testing.
- Docker-based deployment.
- Kubernetes orchestration.
- DDD and bounded contexts.

### Negative Consequences

The project will need to deal with:

- Network failures.
- Service availability.
- API compatibility.
- Distributed debugging.
- Multiple application deployments.
- Increased infrastructure complexity.

These concerns will be introduced progressively rather than all at once.

## Scope Management

Microservices will not be used as a justification for unnecessary
complexity.

The project will initially avoid:

- Message brokers.
- Event buses.
- Distributed transactions.
- Service meshes.
- Complex event-driven architectures.

These technologies may be evaluated later if they provide a clear learning
or business benefit.

## Future Re-evaluation

This decision should be revisited if:

- Service boundaries prove to be incorrect.
- HTTP communication becomes insufficient.
- Asynchronous processing becomes necessary.
- Operational complexity outweighs the benefits.
- New business requirements significantly change the architecture.

Any significant change to this decision should be documented in a new ADR
or by updating the status of this ADR.

## Related Documentation

- [Architecture Overview](docs/architecture/overview.md)
- [Domain Architecture](docs/architecture/domain.md)
