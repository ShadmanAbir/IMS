using Microsoft.AspNetCore.Identity;

namespace IMS.Api.Domain.Entities;

/// <summary>
/// Role claims for fine-grained permissions
/// </summary>
public class ApplicationRoleClaim : IdentityRoleClaim<Guid>
{
    public DateTime CreatedAtUtc { get; private set; }
    public Guid CreatedBy { get; private set; }

    // Navigation properties
    public virtual ApplicationRole Role { get; set; } = null!;

    // Private constructor for EF Core
    private ApplicationRoleClaim() { }

    public ApplicationRoleClaim(Guid roleId, string claimType, string claimValue, Guid createdBy)
    {
        RoleId = roleId;
        ClaimType = claimType;
        ClaimValue = claimValue;
        CreatedBy = createdBy;
        CreatedAtUtc = DateTime.UtcNow;
    }
}