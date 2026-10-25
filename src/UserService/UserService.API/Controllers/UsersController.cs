using Microsoft.AspNetCore.Mvc;
using UserService.Application.Users.ChangePassword;
using UserService.Application.Users.CreateUser;
using UserService.Application.Users.DeleteUser;
using UserService.Application.Users.GetActiveUsers;
using UserService.Application.Users.GetAllUsers;
using UserService.Application.Users.GetInactiveUsers;
using UserService.Application.Users.GetUserById;
using UserService.Application.Users.ReactivateUser;

namespace UserService.API.Controllers;

/// <summary>
/// Provides endpoints for registering and retrieving users.
/// </summary>
[ApiController]
[Route("api/users")]
public sealed class UsersController(
  CreateUserHandler createUserHandler,
  GetUserByIdHandler getUserByIdHandler,
  GetAllUsersHandler getAllUsersHandler,
  GetActiveUsersHandler getActiveUsersHandler,
  GetInactiveUsersHandler getInactiveUsersHandler,
  DeleteUserHandler deleteUserHandler,
  ReactivateUserHandler reactivateUserHandler,
  ChangePasswordHandler changePasswordHandler) : ControllerBase
{
  /// <summary>
  /// Registers a new user.
  /// </summary>
  /// <remarks>
  /// The password must contain at least 15 characters.
  /// The email address must be valid and not already registered.
  /// The password is never returned in the response.
  /// </remarks>
  /// <param name="request">The user's email address and password.</param>
  /// <param name="cancellationToken">Token used to cancel the operation.</param>
  /// <response code="201">The user was successfully created.</response>
  /// <response code="400">The supplied email or password is invalid.</response>
  /// <response code="409">An account with this email already exists.</response>
  [HttpPost]
  [Produces("application/json")]
  [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status201Created)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
  public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
  {
    var command = new CreateUserCommand(request.Email, request.Password);
    var result = await createUserHandler.Handler(command, cancellationToken);

    if (result.IsFailure)
    {
      return result.Error?.Code switch
      {
        "User.EmailAlreadyExists" => Conflict(new ProblemDetails
        {
          Status = StatusCodes.Status409Conflict,
          Title = "Email already exists.",
          Detail = result.Error.Message
        }),
        _ => BadRequest(new ProblemDetails
        {
          Status = StatusCodes.Status400BadRequest,
          Title = "Invalid user data",
          Detail = result.Error?.Message
        })
      };
    }

    var response = new CreateUserResponse(result.Value);

    return CreatedAtAction(nameof(GetUserById), new { id = response.Id }, response);
  }

  /// <summary>
  /// Retrieves a user by their unique identifier.
  /// </summary>
  /// <remarks>
  /// Inactive users are also returned. The response never includes the password hash.
  /// </remarks>
  /// <param name="id">The unique identifier of the user.</param>
  /// <param name="cancellationToken">Token used to cancel the operation.</param>
  /// <response code="200">The user was found.</response>
  /// <response code="400">The supplied user identifier is invalid.</response>
  /// <response code="404">No user was found with the supplied identifier.</response>
  [HttpGet("{id:guid}")]
  [Produces("application/json")]
  [ProducesResponseType(typeof(GetUserByIdResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
  public async Task<IActionResult> GetUserById(Guid id, CancellationToken cancellationToken)
  {
    var result = await getUserByIdHandler.Handle(id, cancellationToken);

    if (result.IsFailure)
    {
      return result.Error?.Code switch
      {
        "User.InvalidId" => BadRequest(new ProblemDetails
        {
          Status = StatusCodes.Status400BadRequest,
          Title = "Invalid user ID",
          Detail = result.Error.Message
        }),

        "User.NotFound" => NotFound(new ProblemDetails
        {
          Status = StatusCodes.Status404NotFound,
          Title = "User not found",
          Detail = result.Error.Message
        }),

        _ => StatusCode(
        StatusCodes.Status500InternalServerError,
        new ProblemDetails
        {
          Status = StatusCodes.Status500InternalServerError,
          Title = "An unexpected error occurred."
        })
      };
    }

    return Ok(result.Value);
  }

  /// <summary>
  /// Retrieves all users.
  /// </summary>
  /// <remarks>
  /// Returns active and inactive users.
  /// The response never includes the password hash.
  /// </remarks>
  /// <param name="cancellationToken">
  /// Token used to cancel the operation.
  /// </param>
  /// <response code="200">
  /// The users were successfully retrieved.
  /// </response>
  [HttpGet]
  [Produces("application/json")]
  [ProducesResponseType(typeof(IReadOnlyList<GetAllUsersResponse>), StatusCodes.Status200OK)]
  public async Task<IActionResult> GetAllUsers(
      CancellationToken cancellationToken)
  {
    var result = await getAllUsersHandler.Handle(
        cancellationToken);

    if (result.IsFailure)
    {
      return StatusCode(
          StatusCodes.Status500InternalServerError,
          new ProblemDetails
          {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred."
          });
    }

    return Ok(result.Value);
  }

  /// <summary>
  /// Retrieves all active users.
  /// </summary>
  /// <param name="cancellationToken">
  /// Token used to cancel the operation.
  /// </param>
  /// <response code="200">
  /// The active users were successfully retrieved.
  /// </response>
  [HttpGet("active")]
  [Produces("application/json")]
  [ProducesResponseType(typeof(IReadOnlyList<GetActiveUsersResponse>), StatusCodes.Status200OK)]
  public async Task<IActionResult> GetActiveUsers(CancellationToken cancellationToken)
  {
    var result = await getActiveUsersHandler.Handle(
        cancellationToken);

    if (result.IsFailure)
    {
      return StatusCode(
          StatusCodes.Status500InternalServerError,
          new ProblemDetails
          {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred."
          });
    }

    return Ok(result.Value);
  }

  /// <summary>
  /// Retrieves all inactive users.
  /// </summary>
  /// <param name="cancellationToken">
  /// Token used to cancel the operation.
  /// </param>
  /// <response code="200">
  /// The inactive users were successfully retrieved.
  /// </response>
  [HttpGet("inactive")]
  [Produces("application/json")]
  [ProducesResponseType(typeof(IReadOnlyList<GetInactiveUsersResponse>), StatusCodes.Status200OK)]
  public async Task<IActionResult> GetInactiveUsers(CancellationToken cancellationToken)
  {
    var result = await getInactiveUsersHandler.Handle(cancellationToken);

    if (result.IsFailure)
    {
      return StatusCode(
          StatusCodes.Status500InternalServerError,
          new ProblemDetails
          {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred."
          });
    }

    return Ok(result.Value);
  }

  /// <summary>
  /// Logically deletes a user by deactivating the account.
  /// </summary>
  /// <param name="id">The unique identifier of the user.</param>
  /// <param name="cancellationToken">Token used to cancel the operation.</param>
  /// <response code="204">The user was successfully deactivated.</response>
  /// <response code="400">The supplied user identifier is invalid.</response>
  /// <response code="404">No user was found with the supplied identifier.</response>
  [HttpDelete("{id:guid}")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
  public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
  {
    var result = await deleteUserHandler.Handle(id, cancellationToken);

    if (result.IsFailure)
    {
      return result.Error?.Code switch
      {
        "User.InvalidId" => BadRequest(new ProblemDetails
        {
          Status = StatusCodes.Status400BadRequest,
          Title = "Invalid user ID",
          Detail = result.Error.Message
        }),
        "User.NotFound" => NotFound(new ProblemDetails
        {
          Status = StatusCodes.Status404NotFound,
          Title = "User not found",
          Detail = result.Error.Message
        }),

        _ => StatusCode(
          StatusCodes.Status500InternalServerError,
          new ProblemDetails
          {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
          })
      };
    }

    return NoContent();
  }

  /// <summary>
  /// Reactivates a user account within the allowed reactivation window,
  /// </summary>
  /// <param name="id">The unique identifier of the user.</param>
  /// <param name="cancellationToken">Token used to cancel the operation.</param>
  /// <response code="204">The user was successfully reactivated.</response>
  /// <response code="400">The supplied user identifier is invalid.</response>
  /// <response code="404">No user was found with the supplied identifier.</response>
  /// <response code="409">The user is already active, has an expired reactivation window, or has an inconsistent deactivation state.</response>
  [HttpPost("{id:guid}/reactivate")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
  public async Task<IActionResult> ReactivateUser(Guid id, CancellationToken cancellationToken)
  {
    var result = await reactivateUserHandler.Handle(id, cancellationToken);

    if (result.IsFailure)
    {
      return result.Error?.Code switch
      {
        "User.InvalidId" => BadRequest(new ProblemDetails
        {
          Status = StatusCodes.Status400BadRequest,
          Title = "Invalid user ID",
          Detail = result.Error.Message
        }),

        "User.NotFound" => NotFound(new ProblemDetails
        {
          Status = StatusCodes.Status404NotFound,
          Title = "User not found",
          Detail = result.Error.Message
        }),

        "User.AlreadyActive" or "User.ReactivationWindowExpired" or "User.NotDeactivated" => Conflict(new ProblemDetails
        {
          Status = StatusCodes.Status409Conflict,
          Title = "User cannot be reactivated",
          Detail = result.Error.Message
        }),

        _ => StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
        {
          Status = StatusCodes.Status500InternalServerError,
          Title = "An unexpected error occurred."
        })
      };
    }

    return NoContent();
  }

  /// <summary>
  /// Change a user's password after validating the current password
  /// </summary>
  /// <param name="id">The user's unique identifier</param>
  /// <param name="request">The current and the new passwords</param>
  /// <param name="cancellationToken">Token used to cancel the operation</param>
  /// <response code="204">The password was successfully changed.</response>
  /// <response code="400">The supplied data is invalid</response>
  /// <response code="404">The user was not found</response>
  /// <response code="409">The password could not be changed</response>
  [HttpPut("{id:guid}/password")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
  public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
  {
    var command = new ChangePasswordCommand(id, request.CurrentPassword, request.NewPassword);
    var result = await changePasswordHandler.Handle(command, cancellationToken);

    if (result.IsFailure)
    {
      return result.Error?.Code switch
      {
        "User.InvalidId" or "User.IncorrectCurrentPassword" or "User.InvalidCurrentPassword" or "User.InvalidPassword" => BadRequest(new ProblemDetails
        {
          Status = StatusCodes.Status400BadRequest,
          Title = "Invalid password change request.",
          Detail = result.Error.Message
        }),

        "User.NotFound" => NotFound(new ProblemDetails
        {
          Status = StatusCodes.Status404NotFound,
          Title = "User not found.",
          Detail = result.Error.Message
        }),

        "User.Inactive" or "User.PasswordUnchanged" => Conflict(new ProblemDetails
        {
          Status = StatusCodes.Status409Conflict,
          Title = "Password cannot be changed.",
          Detail = result.Error.Message
        }),

        _ => StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
        {
          Status = StatusCodes.Status500InternalServerError,
          Title = "An unexpected error occurred."
        })
      };
    }

    return NoContent();
  }
}

/// <summary>
/// Represents the data required to register a new user.
/// </summary>
/// <param name="Email">The user's email address.</param>
/// <param name="Password">The user's password, which must contain at least 15 characters.</param>
public sealed record CreateUserRequest(string Email, string Password);

/// <summary>
/// Represents the response returned after a user is created.
/// </summary>
/// <param name="Id">The unique identifier of the newly created user.</param>
public sealed record CreateUserResponse(Guid Id);

/// <summary>
/// Represents the data required to change a user's password.
/// </summary>
/// <param name="CurrentPassword">The user's current password.</param>
/// <param name="NewPassword">The desired new password.</param>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);