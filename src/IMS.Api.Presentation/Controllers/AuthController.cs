using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Domain.ValueObjects;
using System.Security.Claims;

namespace IMS.Api.Presentation.Controllers;

/// <summary>
/// Controller for authentication operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthenticationService authenticationService,
        ILogger<AuthController> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="request">Registration request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result with JWT token</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthenticationResult>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("User registration attempt for email: {Email}", request.Email);

            var result = await _authenticationService.RegisterAsync(request, cancellationToken);

            if (!result.Success)
            {
                _logger.LogWarning("User registration failed for email: {Email}. Errors: {Errors}", 
                    request.Email, string.Join(", ", result.Errors));
                return BadRequest(result);
            }

            _logger.LogInformation("User registered successfully: {Email}", request.Email);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration for email: {Email}", request.Email);
            return StatusCode(500, AuthenticationResult.Failure("An error occurred during registration"));
        }
    }

    /// <summary>
    /// Authenticate user and return JWT token
    /// </summary>
    /// <param name="request">Login request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result with JWT token</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthenticationResult>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("User login attempt for email: {Email}", request.Email);

            var result = await _authenticationService.LoginAsync(request, cancellationToken);

            if (!result.Success)
            {
                _logger.LogWarning("User login failed for email: {Email}. Errors: {Errors}", 
                    request.Email, string.Join(", ", result.Errors));
                return BadRequest(result);
            }

            _logger.LogInformation("User logged in successfully: {Email}", request.Email);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user login for email: {Email}", request.Email);
            return StatusCode(500, AuthenticationResult.Failure("An error occurred during login"));
        }
    }

    /// <summary>
    /// Refresh JWT token
    /// </summary>
    /// <param name="refreshToken">Refresh token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New authentication result with refreshed JWT token</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthenticationResult>> RefreshToken(
        [FromBody] string refreshToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Token refresh attempt");

            var result = await _authenticationService.RefreshTokenAsync(refreshToken, cancellationToken);

            if (!result.Success)
            {
                _logger.LogWarning("Token refresh failed. Errors: {Errors}", string.Join(", ", result.Errors));
                return BadRequest(result);
            }

            _logger.LogInformation("Token refreshed successfully");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, AuthenticationResult.Failure("An error occurred during token refresh"));
        }
    }

    /// <summary>
    /// Revoke refresh token
    /// </summary>
    /// <param name="refreshToken">Refresh token to revoke</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpPost("revoke")]
    [Authorize]
    public async Task<ActionResult> RevokeToken(
        [FromBody] string refreshToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Token revocation attempt");

            var success = await _authenticationService.RevokeTokenAsync(refreshToken, cancellationToken);

            if (!success)
            {
                _logger.LogWarning("Token revocation failed");
                return BadRequest("Failed to revoke token");
            }

            _logger.LogInformation("Token revoked successfully");
            return Ok(new { message = "Token revoked successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token revocation");
            return StatusCode(500, "An error occurred during token revocation");
        }
    }

    /// <summary>
    /// Change user password
    /// </summary>
    /// <param name="request">Change password request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result</returns>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<AuthenticationResult>> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userIdClaim = User.FindFirst("user_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userIdGuid))
            {
                return BadRequest(AuthenticationResult.Failure("Invalid user context"));
            }

            var userId = UserId.Create(userIdGuid);
            _logger.LogInformation("Password change attempt for user: {UserId}", userId.Value);

            var result = await _authenticationService.ChangePasswordAsync(userId, request, cancellationToken);

            if (!result.Success)
            {
                _logger.LogWarning("Password change failed for user: {UserId}. Errors: {Errors}", 
                    userId.Value, string.Join(", ", result.Errors));
                return BadRequest(result);
            }

            _logger.LogInformation("Password changed successfully for user: {UserId}", userId.Value);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password change");
            return StatusCode(500, AuthenticationResult.Failure("An error occurred during password change"));
        }
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User information</returns>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetCurrentUser(CancellationToken cancellationToken = default)
    {
        try
        {
            var userIdClaim = User.FindFirst("user_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userIdGuid))
            {
                return BadRequest("Invalid user context");
            }

            var userId = UserId.Create(userIdGuid);
            var user = await _authenticationService.GetUserAsync(userId, cancellationToken);

            if (user == null)
            {
                return NotFound("User not found");
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user information");
            return StatusCode(500, "An error occurred while retrieving user information");
        }
    }
}