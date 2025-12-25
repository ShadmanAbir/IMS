using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Domain.Entities;
using System.Security.Claims;

namespace IMS.Api.Presentation.Controllers;

/// <summary>
/// Controller for role management operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize(Roles = "SystemAdmin,TenantAdmin")]
public class RolesController : ControllerBase
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<RolesController> _logger;

    public RolesController(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ILogger<RolesController> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Get all roles
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of roles</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApplicationRole>>> GetRoles(CancellationToken cancellationToken = default)
    {
        try
        {
            var roles = await _roleManager.Roles.ToListAsync(cancellationToken);
            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles");
            return StatusCode(500, "An error occurred while retrieving roles");
        }
    }

    /// <summary>
    /// Get role by ID
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Role information</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApplicationRole>> GetRole(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                return NotFound($"Role with ID {id} not found");
            }

            return Ok(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role with ID: {RoleId}", id);
            return StatusCode(500, "An error occurred while retrieving the role");
        }
    }

    /// <summary>
    /// Create a new role
    /// </summary>
    /// <param name="request">Role creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created role</returns>
    [HttpPost]
    public async Task<ActionResult<ApplicationRole>> CreateRole(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating new role: {RoleName}", request.Name);

            var existingRole = await _roleManager.FindByNameAsync(request.Name);
            if (existingRole != null)
            {
                return BadRequest($"Role '{request.Name}' already exists");
            }

            var role = new ApplicationRole(request.Name, request.Description);
            var result = await _roleManager.CreateAsync(role);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Role creation failed: {Errors}", errors);
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            _logger.LogInformation("Role created successfully: {RoleName}", request.Name);
            return CreatedAtAction(nameof(GetRole), new { id = role.Id }, role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role: {RoleName}", request.Name);
            return StatusCode(500, "An error occurred while creating the role");
        }
    }

    /// <summary>
    /// Update role description
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <param name="request">Role update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated role</returns>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApplicationRole>> UpdateRole(
        Guid id,
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                return NotFound($"Role with ID {id} not found");
            }

            _logger.LogInformation("Updating role: {RoleId}", id);

            role.UpdateDescription(request.Description);
            var result = await _roleManager.UpdateAsync(role);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Role update failed: {Errors}", errors);
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            _logger.LogInformation("Role updated successfully: {RoleId}", id);
            return Ok(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role: {RoleId}", id);
            return StatusCode(500, "An error occurred while updating the role");
        }
    }

    /// <summary>
    /// Delete a role
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteRole(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                return NotFound($"Role with ID {id} not found");
            }

            _logger.LogInformation("Deleting role: {RoleId}", id);

            var result = await _roleManager.DeleteAsync(role);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Role deletion failed: {Errors}", errors);
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            _logger.LogInformation("Role deleted successfully: {RoleId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role: {RoleId}", id);
            return StatusCode(500, "An error occurred while deleting the role");
        }
    }

    /// <summary>
    /// Assign role to user
    /// </summary>
    /// <param name="request">Role assignment request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpPost("assign")]
    public async Task<ActionResult> AssignRole(
        [FromBody] AssignRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Assigning role {RoleName} to user {UserId}", request.RoleName, request.UserId);

            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
            {
                return NotFound($"User with ID {request.UserId} not found");
            }

            var role = await _roleManager.FindByNameAsync(request.RoleName);
            if (role == null)
            {
                return NotFound($"Role '{request.RoleName}' not found");
            }

            var isInRole = await _userManager.IsInRoleAsync(user, request.RoleName);
            if (isInRole)
            {
                return BadRequest($"User is already assigned to role '{request.RoleName}'");
            }

            var result = await _userManager.AddToRoleAsync(user, request.RoleName);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Role assignment failed: {Errors}", errors);
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            _logger.LogInformation("Role assigned successfully: {RoleName} to user {UserId}", request.RoleName, request.UserId);
            return Ok(new { message = $"Role '{request.RoleName}' assigned to user successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role {RoleName} to user {UserId}", request.RoleName, request.UserId);
            return StatusCode(500, "An error occurred while assigning the role");
        }
    }

    /// <summary>
    /// Remove role from user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="roleName">Role name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpDelete("users/{userId:guid}/roles/{roleName}")]
    public async Task<ActionResult> RemoveRoleFromUser(
        Guid userId,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Removing role {RoleName} from user {UserId}", roleName, userId);

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return NotFound($"User with ID {userId} not found");
            }

            var isInRole = await _userManager.IsInRoleAsync(user, roleName);
            if (!isInRole)
            {
                return BadRequest($"User is not assigned to role '{roleName}'");
            }

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Role removal failed: {Errors}", errors);
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            _logger.LogInformation("Role removed successfully: {RoleName} from user {UserId}", roleName, userId);
            return Ok(new { message = $"Role '{roleName}' removed from user successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role {RoleName} from user {UserId}", roleName, userId);
            return StatusCode(500, "An error occurred while removing the role");
        }
    }

    /// <summary>
    /// Get user roles
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user roles</returns>
    [HttpGet("users/{userId:guid}/roles")]
    public async Task<ActionResult<IEnumerable<string>>> GetUserRoles(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return NotFound($"User with ID {userId} not found");
            }

            var roles = await _userManager.GetRolesAsync(user);
            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving user roles");
        }
    }
}