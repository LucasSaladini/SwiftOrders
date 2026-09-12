# UserService

## Domain

The **Domain** layer contains the business rules and the fundamental concepts related to users.

This layer does not have infrastructure dependencies, database, web frameworks or specific persistence mechanisms

### Entities

#### User

`User` is the responsible entity to represent a user on the system.

The entity has own identity through a `Guid` and controls its own state and its own business rules.

##### Properties

| Property | Type | Description |
| --- | --- | --- |
| `Id` | `Guid` | Unique user identifier. |
| `Email` | `Email` | User email, represented through a Value Object. |
| `PasswordHash` | `string` | User's password Hash. The original password is never saved. |
| `CreatedAt` | `DateTime` | User's creation date and time. It is immutable. |
| `UpdatedAt` | `DateTime` | User's last change date and time. |
| `Active` | `bool` | Indicates if the user is active. |
| `DeletedAt` | `DateTime?` | Date and time of deactivation/logic exclusion. `null` when the user is not marked for exclusion. |

##### Behaviors

The `User` entity encapsulates operations related to its own state.

###### `ChangeEmail`

Changes the user email.

The creation of a new email is delegated to a Value Object `Email`, assuring that an invalid email is not set to the entity.

Changing the email updates `UpdatedAt`.

> The validation of current password to allow changing email will be
> implemented further on the Application layer.

###### `ChangePassword`

Changes user's `PasswordHash`.
The entity receives the new hash, not the password in plain text.

The implementation of hashing mechanism does not belong to the Domain and it will be abstracted further through an interface.

Change the password will update `UpdatedAt`.

###### `Deactivate`

Performs the logic deactivation of the user.

When a user is deactivated:

- `Active` is set to `false`;
- `DeletedAt` receives the date and time of deactivation;
- `UpdatedAt` is updated.

An entity that is already inactive is not deactivated again.

The exclusion logic allows that the user remains available during the pre-defined period for further reactivation.

###### `Reactivate`

Allows the reactivation of a user that has been deactivated between the allowed period of 15 days.

When reactivated:

- `Active` is set to `true`;
- `DeletedAt` is set to `null`;
- `UpdatedAt` is updated.

After a grace period of 15 days, the reactivation is not allowed.

The permanent removal of the user after this period will be responsibility of the Application/Infrastructure layers and not the `User` entity.

##### Domain Rules

The main rules related to `User` entity are:

1. The user has his own unique identifier.
2. The email is required and must be a valid address.
3. The email is represented by the Value Object `Email`.
4. The original password must never be stored.
5. The system stores only the `PasswordHash`.
6. `CreatedAt` can not be changed after its creation.
7. `UpdatedAt` must be updated when the entity state suffer modification/updates.
8. A created user starts as active.
9. The deactivation is performed logically through `Active` and `DeletedAt`.
10. A deactivated user can be reactivated after 15 days.
11. After 15 days, the user can no longer be reactivated.
12. The entity does not know hashing details, database or infrastructure.

#### Email

`Email` is a Value Object responsible to represent a valid email address inside domain.

Differently from a `string`, the `Email` encapsulates the rule that its value must represent a valid address.

##### Characteristics

- It's `sealed`, not allowing inheritance.
- Has immutable value.
- Its constructor is private.
- The creation is perform through an `Email.Create()`.
- Empty or invalid values are rejected.

##### Responsibility

The `Email` Value Object guarantees that the `User` entity does not directly receives a string without email validation.

The `User` works with:

`Email`

instead of:

`string`

This centralizes the email validation rule and avoids duplication on different parts of the system.

## Application

The **Application** layer will be responsible for orchestrating the use cases from `UserService`.

It most not contain fundamental rules that belongs to domain, but must coordinate the flow between domain and other layers.

Among its responsibilities are:

- Use cases related to users.
- Permission validation and operation context.
- Orchestration of domain entities.
- Utilization of necessary abstractions to external services.
- Coordinating persistence through abstractions.
- Returning results from use cases.

Future examples:

- Create user.
- Authenticate user.
- Change email.
- Change password.
- Deactivate user.
- Reactivate user.

The Application must not directly depend on infrastructure concrete implementations.

## Infrastructure

The **Infrastructure** layer contain the technical implementation needed to perform the defined operations by the application.

Among its responsibilities are:

- Persisting users.
- Implementing database access.
- Implementing password hashing mechanism.
- Implementing defined interfaces by the internal layers.
- Settings related to external resources.

The Infrastructure can depend on Domain and Application, but the Domain must not depend on Infrastructure.

## API

The **API** layer representes the HTTP input from `UserService`.

Its responsibilities will be:

- Expose HTTP endpoints.
- Receive and validate request on API level.
- Forward operations to Application.
- Convert results from Application on its HTTP responses.
- Set authentication, authorization and middleware.

The API must not contain business rules that belongs to Domain.

The expected flow is:

API → Application → Domain

and, when it's necessary:

Application → Infrastructure
