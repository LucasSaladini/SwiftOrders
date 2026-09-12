# Testing Strategy

## Overview

SwiftOrders follows a layered testing strategy aligned with the internal
Clean Architecture used by each microservice.

The primary goal is to verify business rules and application behavior while
keeping tests isolated, maintainable, and reliable.

Testing is considered part of the architecture rather than a final step of
the development process.

The initial testing strategy focuses on:

- Domain unit tests.
- Application unit tests.
- Infrastructure integration tests when required.
- API integration tests when required.

The project uses **xUnit** as its primary testing framework.

---

## Testing Principles

SwiftOrders follows these principles when designing tests.

### 1. Test Behavior, Not Implementation Details

Tests should verify observable behavior and business rules instead of
depending unnecessarily on internal implementation details.

For example, a test should verify that a deactivated user can be
reactivated within the allowed period rather than testing private fields or
internal implementation steps.

---

### 2. Prefer Fast and Isolated Tests

Tests that do not require external infrastructure should remain isolated.

Domain tests should not require:

- A database.
- HTTP.
- Docker.
- Kubernetes.
- External services.

This allows business rules to be validated quickly and reliably.

---

### 3. Business Rules Have Priority

The most important tests are those that protect business rules and
invariants.

For example, the `User` domain contains rules related to:

- Email validity.
- User activation state.
- User deactivation.
- Reactivation.
- The 15-day reactivation window.
- Password hash changes.

These rules should be protected by unit tests.

---

## Testing Pyramid

The project follows a testing pyramid concept:

```text
                 / \
                /   \
               / API \
              /-------\
             /         \
            /Integration\
           /------------ \
          /               \
         /  Unit Tests     \
        /-------------------\
```

The majority of tests should be fast unit tests.

Integration and API tests should be added where interaction between
components or infrastructure needs to be verified.

The project does not aim to maximize the number of tests blindly.

The objective is to provide meaningful coverage of behavior and
architectural boundaries.

## Test Types

### Unit Tests

Unit tests verify a small unit of behavior in isolation.

They are primarily used for:

- Domain entities.
- Value Objects.
- Application use cases.
- Application services.
- Business rules.

Unit tests should execute quickly and should not require external
infrastructure.

Example:

```text
User
 |
 +-- Deactivate()
 |
 +-- Reactivate()
 |
 +-- ChangeEmail()
 |
 +-- ChangePassword()
```

Each behavior can be tested independently.

## Domain Tests

The Domain layer has the highest priority for unit testing because it
contains the core business rules.

Examples of domain scenarios include:

### User Creation

- A valid email creates a user successfully.
- An invalid email is rejected.
- The user starts as active.
- CreatedAt is initialized.
- UpdatedAt is initialized.

### Email Changes

- A valid email can replace the current email.
- An invalid email is rejected.
- UpdatedAt changes when the email changes.

### Password Changes

- A new password hash can replace the current hash.
- The password hash is stored as a hash rather than a plaintext password.
- UpdatedAt changes when the password hash changes.

Password hashing itself is not a Domain responsibility.

The Domain only receives the resulting password hash.

### Deactivation

- An active user can be deactivated.
- Active becomes false.
- DeletedAt is populated.
- UpdatedAt changes.
- Deactivating an already inactive user does not perform the operation again.

### Reactivation

- A deactivated user can be reactivated within the allowed period.
- A user cannot be reactivated after the 15-day window.
- An active user cannot be reactivated.
- Reactivation clears DeletedAt.
- Reactivation restores Active.
- UpdatedAt changes after successful reactivation.

## Application Tests

Application tests verify use cases and application orchestration.

They should validate that the application layer:

- Executes the correct use case.
- Coordinates domain operations correctly.
- Uses required abstractions.
- Handles expected failures.
- Produces appropriate application results.

External dependencies should normally be replaced by test doubles.

For example:

```text
Application Use Case
        |
        v
IUserRepository
        ^
        |
   Test Double
```

The Application layer should not require a real database for ordinary unit
tests.

## Test Doubles

When testing application behavior, external dependencies may be replaced by
test doubles.

Depending on the scenario, the project may use:

- Mocks.
- Stubs.
- Fakes.

The choice should be based on the behavior being tested rather than on a
requirement to mock everything.

Mocks should not be introduced unnecessarily.

The test should remain focused on the behavior of the component under test.

## Infrastructure Integration Tests

Infrastructure tests verify interactions with real technical components.

Examples include:

- Database persistence.
- Entity Framework Core configuration.
- Repository implementations.
- External service integrations.

These tests may require infrastructure such as:

```text
Application
     |
     v
Infrastructure
     |
     v
Database
```

Unlike unit tests, infrastructure integration tests may use real
dependencies.

They should therefore be kept separate from the fast unit test suite.

## API Tests

API tests verify the behavior exposed by a microservice through HTTP.

They may validate:

- HTTP status codes.
- Request validation.
- Response contracts.
- Authentication and authorization.
- Dependency injection configuration.
- Application integration.
- API error handling.

Conceptually:

```text
HTTP Client
     |
     v
   API
     |
     v
Application
     |
     v
Domain
```

API tests should focus on the externally observable behavior of the
service.

They should not duplicate every Domain unit test.

## Integration Between Microservices

Because SwiftOrders uses HTTP for inter-service communication, future tests
may also verify service-to-service communication.

For example:

```text
OrderService
     |
     | HTTP
     v
 UserService
```

These tests may validate:

- HTTP contracts.
- Serialization.
- Status code handling.
- Error responses.
- Timeout behavior.
- Service availability scenarios.

These tests should be introduced when actual inter-service communication is
implemented.

They are not required for isolated Domain development.

## Test Organization

Tests are organized separately from production code.

The current structure includes:

```text
tests/
└── UserService.Tests/
```

As the service grows, tests may be organized by architectural concern.

For example:

```text
tests/
└── UserService.Tests/
    ├── Domain/
    │   ├── Entities/
    │   └── ValueObjects/
    │
    ├── Application/
    │
    ├── Infrastructure/
    │
    └── API/
```

The exact organization may evolve as the test suite grows.

## Naming Tests

Test names should clearly communicate the behavior being verified.

A recommended structure is:

```text
MethodOrScenario_Condition_ExpectedResult
```

For example:

```text
Reactivate_WhenWithin15Days_ReactivatesUser

Reactivate_WhenMoreThan15DaysHavePassed_DoesNotReactivateUser

ChangeEmail_WhenEmailIsInvalid_RejectsChange
```

The exact naming convention may evolve, but test names should remain
descriptive enough to understand the scenario without reading the
implementation.

## Arrange, Act, Assert

Tests should preferably follow the Arrange, Act, Assert (AAA) pattern.

```text
Arrange
   |
   v
Prepare the test scenario

Act
   |
   v
Execute the behavior

Assert
   |
   v
Verify the expected result
```

Example conceptually:

```C#
// Arrange
var user = new User("user@example.com");

// Act
user.Deactivate();

// Assert
Assert.False(user.Active);
```

The pattern improves readability and makes the intention of each test
explicit.

## What Should Not Be Tested

The project should avoid tests that provide little behavioral value.

Examples include:

- Testing private implementation details.
- Testing framework behavior.
- Testing trivial property getters without meaningful behavior.
- Duplicating the same behavior across multiple test layers.
- Writing tests only to increase a coverage percentage.

Code coverage is useful as a diagnostic metric, but it is not the primary
definition of test quality.

## Test Isolation

Tests should avoid unnecessary dependencies on:

- Test execution order.
- Shared mutable state.
- External services.
- Local machine configuration.
- Developer-specific environments.

Each test should ideally be capable of running independently.

This is particularly important for unit tests.

## Deterministic Tests

Tests should produce the same result when executed repeatedly under the same
conditions.

Time-dependent domain rules require special attention.

For example, the User entity uses DateTime.UtcNow for timestamps and for
the reactivation window.

As time-dependent behavior becomes more complex, the architecture may
introduce an abstraction for obtaining the current time.

This can make tests deterministic without changing the business rule itself.

For example:

```text
Application / Domain
        |
        v
   Time abstraction
        ^
        |
    Test implementation
```

This abstraction should only be introduced when the complexity of
time-dependent behavior justifies it.

## Test Coverage

The project does not define success solely by a percentage of code
coverage.

Coverage should be used to identify potentially untested areas.

Priority should be given to:

- Business rules.
- Domain invariants.
- Application use cases.
- Error scenarios.
- Architectural boundaries.
- Infrastructure integrations.
- API contracts.

A smaller suite of meaningful tests is preferred over a large suite of
fragile or redundant tests.

## Current Testing Scope

The initial testing scope focuses on the UserService Domain and its core
business behavior.

The project will progressively expand testing as additional layers are
implemented.

Current and future testing can evolve approximately as follows:

```text
Current Testing Scope

The initial testing scope focuses on the UserService Domain and its core
business behavior.

The project will progressively expand testing as additional layers are
implemented.

Current and future testing can evolve approximately as follows:
```

Tests should be introduced alongside the corresponding functionality rather
than postponed until the end of development.

## Future Evolution

The testing strategy may evolve as SwiftOrders becomes more complex.

Potential future improvements include:

- Shared test utilities.
- Integration test containers.
- API integration testing.
- Contract testing between services.
- End-to-end testing.
- Performance testing.
- Resilience testing.
- Distributed tracing validation.

These capabilities should be introduced only when they provide meaningful
value to the project.

The initial architecture intentionally avoids unnecessary testing
infrastructure.

## Related Decisions

- (ADR-001: Adopt a Microservices Architecture) [docs/decisions/ADR-001-microservices.md]
- (ADR-002: Use HTTP for Inter-Service Communication) [docs/decisions/ADR-002-http-communication.md]
- (ADR-003: Adopt Clean Architecture for Internal Service Structure) [docs/decisions/ADR-003-user-domain.md]
- (Architecture Overview) [docs/architecture/overview.md]
- (Domain Architecture) [docs/architecture/domain.md]
