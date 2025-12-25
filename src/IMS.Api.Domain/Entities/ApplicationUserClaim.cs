using Microsoft.AspNetCore.Identity;

namespace IMS.Api.Domain.Entities;

/// <summary>
/// User claims for additional user-specific permissions
/// </summary>
public class ApplicationUserClaim : IdentityUserClaim<Guid>
{
    public DateTime CreatedAtUtc { get; private set; }
    public Guid CreatedBy { get; private set; }

    // Navigation properties
    public virtual ApplicationUser User { get; set; } = null!;

    // Private constructor for EF Core
    private ApplicationUserClaim() { }

    public ApplicationUserClaim(Guid userId, string claimType, string claimValue, Guid createdBy)
    {
        UserId = userId;
        ClaimType = claimType;
        ClaimValue = claimValue;
        CreatedBy = createdBy;
        CreatedAtUtc = DateTime.UtcNow;
    }
}