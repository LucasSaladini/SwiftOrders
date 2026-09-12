# ADR-002: Use HTTP for Inter-Service Communication

- **Status:** Accepted
- **Date:** 2026-09-01
- **Decision Type:** Architecture
- **Scope:** SwiftOrders

## Context

SwiftOrders is composed of multiple independent microservices that need to
communicate with each other.

The initial architecture contains:

- `UserService`
- `OrderService`
- `OrderProcessingService`

Because these services are independently deployed applications, they cannot
rely on direct in-process communication.

A communication mechanism is therefore required for services to exchange
information and coordinate operations.

The project is also intended as a learning environment. The initial
architecture should provide practical experience with service-to-service
communication without introducing unnecessary distributed-system complexity
at this stage.

---

## Decision

SwiftOrders will use **HTTP-based synchronous communication** for
inter-service communication during the initial implementation.

Services will communicate through explicitly defined HTTP APIs and
contracts.

For example:

```text
+-------------+        HTTP        +-------------+
| UserService | <----------------> | OrderService|
+-------------+                    +-------------+
                                         |
                                         | HTTP
                                         v
                                +----------------------+
                                | OrderProcessing      |
                                | Service              |
                                +----------------------+
```

Each service remains responsible for its own internal implementation.

Services must communicate through their exposed contracts rather than
accessing another service's internal classes, database, or domain objects.

## Communication Model

The initial communication model is synchronous.

A service sends an HTTP request and waits for the receiving service to
return a response.

Conceptually:

```text
Service A
   |
   | HTTP Request
   v
Service B
   |
   | HTTP Response
   v
Service A
```

The response should contain enough information for the calling service to
determine whether the requested operation succeeded or failed.

## API Contracts

Communication between services must be based on explicit contracts.

A service should not expose its internal domain entities as a mechanism for
communication.

For example, instead of sharing a User domain entity between services,
UserService should expose an appropriate API contract.

Conceptually:

```text
UserService Domain
       |
       v
   API Contract
       |
       | HTTP
       v
   OrderService
```

This preserves service boundaries and prevents tight coupling between
internal domain models.

## Why HTTP?

HTTP was selected for the initial implementation for several reasons.

### 1. Simplicity

HTTP is widely understood and supported by the .NET ecosystem.

It allows the project to establish service boundaries without immediately
introducing additional infrastructure such as message brokers.

### 2. Explicit Communication

HTTP makes the interaction between services explicit.

A request represents an intentional operation against another service's API.

### 3. Easy Local Development

HTTP-based services can be executed locally and tested independently using
standard development and testing tools.

### 4. Compatibility with ASP.NET Core

The microservices are being developed with ASP.NET Core, which provides
mature support for HTTP APIs and client communication.

### 5. Learning Objectives

Using HTTP allows the project to focus initially on important distributed
system concepts such as:

- API contracts.
- Service boundaries.
- HTTP status codes.
- Timeouts.
- Retries.
- Error handling.
- Service availability.
- Distributed tracing.

These concepts can be introduced progressively.

## Alternatives Considered

### Message Broker

Examples include RabbitMQ, Kafka, or Azure Service Bus.

A message broker could provide asynchronous communication between services.

#### Advantages

- Asynchronous processing.
- Better decoupling in some scenarios.
- Improved resilience for certain workflows.
- Natural support for event-driven architectures.
- Ability to buffer messages.

#### Disadvantages

- Additional infrastructure.
- Increased operational complexity.
- More complex local development.
- Additional failure scenarios.
- More concepts to learn simultaneously.
- Event delivery and consistency concerns.

A message broker was rejected for the initial implementation because
asynchronous messaging is not currently required by the project's scope.

The decision can be revisited if future requirements justify it.

## Shared Database

Another alternative would be allowing multiple services to directly access
the same database.

This was rejected because it would create strong coupling between services
and weaken service ownership boundaries.

Services should own their data and expose information through contracts.

## Direct Code Sharing Between Services

Sharing domain entities or internal classes between microservices was also
rejected.

A shared implementation would create compile-time coupling and make the
services less independent.

Shared infrastructure abstractions may exist where appropriate, but a
service must not depend on another service's internal domain implementation.

## Error Handling

HTTP communication introduces the possibility of network and service-level
failures.

The application must therefore distinguish between:

- Successful responses.
- Client errors.
- Server errors.
- Timeouts.
- Connection failures.
- Unavailable services.

The initial implementation will handle these concerns progressively as
inter-service communication is introduced.

Retry policies, resilience mechanisms, and timeout strategies should be
introduced only where they provide a clear benefit.

## Synchronous Communication Trade-offs

### Advantages of Synchronous Communication

- Simple request/response model.
- Easy to understand and debug.
- Explicit API contracts.
- Straightforward integration with ASP.NET Core.
- Low infrastructure overhead.
- Appropriate for the current project scope.

### Disadvantages of Synchronous Communication

- Calling services depend on the availability of the target service.
- Network latency affects request processing.
- Failures can propagate between services.
- Long-running operations are less suitable for synchronous communication.
- Services can become temporally coupled.

These trade-offs are accepted for the initial architecture.

## Consequences

### Positive Consequences

SwiftOrders will gain practical experience with:

- REST APIs.
- HTTP communication.
- API contracts.
- Service discovery/configuration.
- HTTP error handling.
- Timeouts.
- Distributed failures.
- Service-to-service authentication.
- Observability of distributed requests.

The architecture remains relatively simple while still providing realistic
microservice communication.

### Negative Consequences

The system will need to handle distributed communication problems such as:

- Network failures.
- Service unavailability.
- Request timeouts.
- Latency.
- Contract compatibility.
- Cascading failures.

These concerns are considered part of the learning objectives of the
project.

## Scope

This decision applies to the initial implementation of SwiftOrders.

It does not prohibit the introduction of asynchronous communication in the
future.

The following technologies are intentionally outside the initial scope:

- Message brokers.
- Event buses.
- Kafka.
- RabbitMQ.
- Azure Service Bus.
- Event-driven communication.

They may be introduced later if a concrete requirement or architectural
learning objective justifies them.

## Future Re-evaluation

This decision should be revisited if:

- A workflow requires asynchronous processing.
- Long-running operations become necessary.
- Services become too tightly coupled through synchronous calls.
- HTTP communication creates unacceptable availability dependencies.
- Event-driven architecture becomes a meaningful requirement.
- The system requires message durability or buffering.

Any change should be documented through a new ADR or by updating the status
of this decision.

## Related Decisions

- [ADR-001: Adopt a Microservices Architecture](docs/decisions/ADR-001-microservices.md)
- [Architecture Overview](docs/architecture/overview.md)
