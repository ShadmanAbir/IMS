using Microsoft.AspNetCore.Identity;

namespace IMS.Api.Domain.Entities;

/// <summary>
/// Application role entity extending IdentityRole for role-based authorization
/// </summary>
public class ApplicationRole : IdentityRole<Guid>
{
    public string Description { get; internal set; } = string.Empty;
    public DateTime CreatedAtUtc { get; internal set; }
    public DateTime? UpdatedAtUtc { get; internal set; }

    // Navigation properties
    public virtual ICollection<ApplicationUserRole> UserRoles { get; set; } = new List<ApplicationUserRole>();
    public virtual ICollection<ApplicationRoleClaim> RoleClaims { get; set; } = new List<ApplicationRoleClaim>();

    // Public parameterless constructor for EF Core and tests
    public ApplicationRole() { }

    public ApplicationRole(string name, string description)
    {
        Id = Guid.NewGuid();
        Name = name;
        NormalizedName = name.ToUpperInvariant();
        Description = description;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateDescription(string description)
    {
        Description = description;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}