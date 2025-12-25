using Microsoft.AspNetCore.Identity;

namespace IMS.Api.Domain.Entities;

/// <summary>
/// Junction entity for user-role relationships
/// </summary>
public class ApplicationUserRole : IdentityUserRole<Guid>
{
    public DateTime AssignedAtUtc { get; private set; }
    public Guid AssignedBy { get; private set; }

    // Navigation properties
    public virtual ApplicationUser User { get; set; } = null!;
    public virtual ApplicationRole Role { get; set; } = null!;

    // Private constructor for EF Core
    private ApplicationUserRole() { }

    public ApplicationUserRole(Guid userId, Guid roleId, Guid assignedBy)
    {
        UserId = userId;
        RoleId = roleId;
        AssignedBy = assignedBy;
        AssignedAtUtc = DateTime.UtcNow;
    }
}