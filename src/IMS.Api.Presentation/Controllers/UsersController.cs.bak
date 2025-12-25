using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Domain.Entities;
using IMS.Api.Domain.ValueObjects;
using IMS.Api.Presentation.Authorization;
using AutoMapper;
using System.Security.Claims;

namespace IMS.Api.Presentation.Controllers;

/// <summary>
/// Controller for user management operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[AuthorizePermission("manage_users")]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IMapper _mapper;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IMapper mapper,
        ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get all users (tenant-aware)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of users</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers(CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserTenantId = GetCurrentUserTenantId();
            if (currentUserTenantId == null)
            {
                return BadRequest("Invalid tenant context");
            }

            var users = await _userManager.Users
                .Where(u => u.TenantId == currentUserTenantId)
                .ToListAsync(cancellationToken);

            var userDtos = new List<UserDto>();
            foreach (var user in users)
            {
                var userDto = _mapper.Map<UserDto>(user);
                userDto.Roles = (await _userManager.GetRolesAsync(user)).ToList();
                userDto.Claims = (await _userManager.GetClaimsAsync(user))
                    .Select(c => new UserClaimDto { Type = c.Type, Value = c.Value })
                    .ToList();
                userDtos.Add(userDto);
            }

            return Ok(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return StatusCode(500, "An error occurred while retrieving users");
        }
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User information</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetUser(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserTenantId = GetCurrentUserTenantId();
            if (currentUserTenantId == null)
            {
                return BadRequest("Invalid tenant context");
            }

            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == currentUserTenantId, cancellationToken);

            if (user == null)
            {
                return NotFound($"User with ID {id} not found");
            }

            var userDto = _mapper.Map<UserDto>(user);
            userDto.Roles = (await _userManager.GetRolesAsync(user)).ToList();
            userDto.Claims = (await _userManager.GetClaimsAsync(user))
                .Select(c => new UserClaimDto { Type = c.Type, Value = c.Value })
                .ToList();

            return Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user with ID: {UserId}", id);
            return StatusCode(500, "An error occurred while retrieving the user");
        }
    }

    /// <summary>
    /// Update user profile
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">Update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user</returns>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserDto>> UpdateUser(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserTenantId = GetCurrentUserTenantId();
            if (currentUserTenantId == null)
            {
                return BadRequest("Invalid tenant context");
            }

            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == currentUserTenantId, cancellationToken);

            if (user == null)
            {
                return NotFound($"User with ID {id} not found");
            }

            _logger.LogInformation("Updating user: {UserId}", id);

            user.UpdateProfile(request.FirstName, request.LastName);
            
            if (request.IsActive != user.IsActive)
            {
                if (request.IsActive)
                {
                    user.Activate();
                }
                else
                {
                    user.Deactivate();
                }
            }

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("User update failed: {Errors}", errors);
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            var userDto = _mapper.Map<UserDto>(user);
            userDto.Roles = (await _userManager.GetRolesAsync(user)).ToList();
            userDto.Claims = (await _userManager.GetClaimsAsync(user))
                .Select(c => new UserClaimDto { Type = c.Type, Value = c.Value })
                .ToList();

            _logger.LogInformation("User updated successfully: {UserId}", id);
            return Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user: {UserId}", id);
            return StatusCode(500, "An error occurred while updating the user");
        }
    }

    /// <summary>
    /// Delete user (soft delete)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteUser(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserTenantId = GetCurrentUserTenantId();
            if (currentUserTenantId == null)
            {
                return BadRequest("Invalid tenant context");
            }

            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == currentUserTenantId, cancellationToken);

            if (user == null)
            {
                return NotFound($"User with ID {id} not found");
            }

            _logger.LogInformation("Deactivating user: {UserId}", id);

            // Soft delete by deactivating the user
            user.Deactivate();
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("User deactivation failed: {Errors}", errors);
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            _logger.LogInformation("User deactivated successfully: {UserId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user: {UserId}", id);
            return StatusCode(500, "An error occurred while deactivating the user");
        }
    }

    /// <summary>
    /// Add claim to user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">Add claim request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpPost("{userId:guid}/claims")]
    public async Task<ActionResult> AddClaimToUser(
        Guid userId,
        [FromBody] AddClaimRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserTenantId = GetCurrentUserTenantId();
            if (currentUserTenantId == null)
            {
                return BadRequest("Invalid tenant context");
            }

            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == currentUserTenantId, cancellationToken);

            if (user == null)
            {
                return NotFound($"User with ID {userId} not found");
            }

            _logger.LogInformation("Adding claim {ClaimType}:{ClaimValue} to user {UserId}", 
                request.ClaimType, request.ClaimValue, userId);

            var claim = new Claim(request.ClaimType, request.ClaimValue);
            var result = await _userManager.AddClaimAsync(user, claim);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Add claim failed: {Errors}", errors);
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            _logger.LogInformation("Claim added successfully to user {UserId}", userId);
            return Ok(new { message = "Claim added successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding claim to user {UserId}", userId);
            return StatusCode(500, "An error occurred while adding the claim");
        }
    }

    /// <summary>
    /// Remove claim from user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="claimType">Claim type</param>
    /// <param name="claimValue">Claim value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpDelete("{userId:guid}/claims/{claimType}/{claimValue}")]
    public async Task<ActionResult> RemoveClaimFromUser(
        Guid userId,
        string claimType,
        string claimValue,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserTenantId = GetCurrentUserTenantId();
            if (currentUserTenantId == null)
            {
                return BadRequest("Invalid tenant context");
            }

            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == currentUserTenantId, cancellationToken);

            if (user == null)
            {
                return NotFound($"User with ID {userId} not found");
            }

            _logger.LogInformation("Removing claim {ClaimType}:{ClaimValue} from user {UserId}", 
                claimType, claimValue, userId);

            var claim = new Claim(claimType, claimValue);
            var result = await _userManager.RemoveClaimAsync(user, claim);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Remove claim failed: {Errors}", errors);
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            _logger.LogInformation("Claim removed successfully from user {UserId}", userId);
            return Ok(new { message = "Claim removed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing claim from user {UserId}", userId);
            return StatusCode(500, "An error occurred while removing the claim");
        }
    }

    private TenantId? GetCurrentUserTenantId()
    {
        var tenantIdClaim = User.FindFirst("tenant_id")?.Value;
        if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            return TenantId.Create(tenantId);
        }
        return null;
    }
}

/// <summary>
/// Request model for updating user information
/// </summary>
public class UpdateUserRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Request model for adding claims to users
/// </summary>
public class AddClaimRequest
{
    public string ClaimType { get; set; } = string.Empty;
    public string ClaimValue { get; set; } = string.Empty;
}