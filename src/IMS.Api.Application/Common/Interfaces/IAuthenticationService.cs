using IMS.Api.Application.Common.DTOs;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Application.Common.Interfaces;

/// <summary>
/// Service for handling user authentication operations
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Registers a new user
    /// </summary>
    Task<AuthenticationResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates a user and returns JWT token
    /// </summary>
    Task<AuthenticationResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes an expired JWT token
    /// </summary>
    Task<AuthenticationResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a refresh token
    /// </summary>
    Task<bool> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes user password
    /// </summary>
    Task<AuthenticationResult> ChangePasswordAsync(UserId userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user information by ID
    /// </summary>
    Task<UserDto?> GetUserAsync(UserId userId, CancellationToken cancellationToken = default);
}