using System.Security.Claims;
using IMS.Api.Domain.Entities;

namespace IMS.Api.Application.Common.Interfaces;

/// <summary>
/// Service for JWT token generation and validation
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a JWT access token for the user
    /// </summary>
    Task<string> GenerateAccessTokenAsync(ApplicationUser user, IList<string> roles, IList<Claim> claims);

    /// <summary>
    /// Generates a refresh token
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates a JWT token and returns the principal
    /// </summary>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// Gets the user ID from a JWT token
    /// </summary>
    Guid? GetUserIdFromToken(string token);

    /// <summary>
    /// Gets the tenant ID from a JWT token
    /// </summary>
    Guid? GetTenantIdFromToken(string token);
}