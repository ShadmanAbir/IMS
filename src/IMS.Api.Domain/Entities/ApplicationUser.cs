using Microsoft.AspNetCore.Identity;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Domain.Entities;

/// <summary>
/// Application user entity extending IdentityUser for authentication and authorization
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public TenantId TenantId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public DateTime? LastLoginUtc { get; private set; }

    // Navigation properties
    public virtual ICollection<ApplicationUserRole> UserRoles { get; set; } = new List<ApplicationUserRole>();

    // Parameterless constructor for EF Core and tests
    public ApplicationUser() { }

    public ApplicationUser(
        string email,
        string firstName,
        string lastName,
        TenantId tenantId)
    {
        Id = Guid.NewGuid();
        Email = email;
        UserName = email; // Use email as username
        NormalizedEmail = email.ToUpperInvariant();
        NormalizedUserName = email.ToUpperInvariant();
        FirstName = firstName;
        LastName = lastName;
        TenantId = tenantId;
        CreatedAtUtc = DateTime.UtcNow;
        EmailConfirmed = true; // Auto-confirm for now
    }

    public string FullName => $"{FirstName} {LastName}";

    public void UpdateProfile(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordLogin()
    {
        LastLoginUtc = DateTime.UtcNow;
    }
}