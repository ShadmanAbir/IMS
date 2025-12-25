using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Domain.Entities;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Infrastructure.Services;

/// <summary>
/// Service for handling user authentication operations
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        IMapper mapper)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _mapper = mapper;
    }

    public async Task<AuthenticationResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return AuthenticationResult.Failure("User with this email already exists");
        }

        // Create new user
        var user = new ApplicationUser(
            request.Email,
            request.FirstName,
            request.LastName,
            TenantId.Create(request.TenantId));

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return AuthenticationResult.Failure(result.Errors.Select(e => e.Description).ToArray());
        }

        // Assign default role
        await _userManager.AddToRoleAsync(user, "WarehouseOperator");

        // Generate tokens
        var roles = await _userManager.GetRolesAsync(user);
        var claims = await _userManager.GetClaimsAsync(user);
        
        var accessToken = await _tokenService.GenerateAccessTokenAsync(user, roles, claims);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Store refresh token (in a real implementation, you'd store this in the database)
        // For now, we'll just return it

        var userDto = _mapper.Map<UserDto>(user);
        userDto.Roles = roles.ToList();
        userDto.Claims = claims.Select(c => new UserClaimDto { Type = c.Type, Value = c.Value }).ToList();

        return AuthenticationResult.SuccessResult(
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddMinutes(60), // Should match JWT expiration
            userDto);
    }

    public async Task<AuthenticationResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
        {
            return AuthenticationResult.Failure("Invalid email or password");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
            {
                return AuthenticationResult.Failure("Account is locked out");
            }
            return AuthenticationResult.Failure("Invalid email or password");
        }

        // Update last login
        user.RecordLogin();
        await _userManager.UpdateAsync(user);

        // Generate tokens
        var roles = await _userManager.GetRolesAsync(user);
        var claims = await _userManager.GetClaimsAsync(user);
        
        var accessToken = await _tokenService.GenerateAccessTokenAsync(user, roles, claims);
        var refreshToken = _tokenService.GenerateRefreshToken();

        var userDto = _mapper.Map<UserDto>(user);
        userDto.Roles = roles.ToList();
        userDto.Claims = claims.Select(c => new UserClaimDto { Type = c.Type, Value = c.Value }).ToList();

        return AuthenticationResult.SuccessResult(
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddMinutes(60), // Should match JWT expiration
            userDto);
    }

    public async Task<AuthenticationResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        // In a real implementation, you would:
        // 1. Validate the refresh token against stored tokens
        // 2. Check if it's expired
        // 3. Get the associated user
        // 4. Generate new tokens
        
        // For now, return failure as refresh token storage is not implemented
        return AuthenticationResult.Failure("Refresh token functionality not implemented");
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        // In a real implementation, you would remove the refresh token from storage
        // For now, return true as if it was revoked
        return await Task.FromResult(true);
    }

    public async Task<AuthenticationResult> ChangePasswordAsync(UserId userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (user == null)
        {
            return AuthenticationResult.Failure("User not found");
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return AuthenticationResult.Failure(result.Errors.Select(e => e.Description).ToArray());
        }

        return AuthenticationResult.SuccessResult(string.Empty, string.Empty, DateTime.UtcNow, _mapper.Map<UserDto>(user));
    }

    public async Task<UserDto?> GetUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

        if (user == null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        var claims = await _userManager.GetClaimsAsync(user);

        var userDto = _mapper.Map<UserDto>(user);
        userDto.Roles = roles.ToList();
        userDto.Claims = claims.Select(c => new UserClaimDto { Type = c.Type, Value = c.Value }).ToList();

        return userDto;
    }
}