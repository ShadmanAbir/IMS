using IMS.Api.Domain.Common;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Domain.Entities;

/// <summary>
/// Customer group entity for organizing customers into pricing tiers
/// </summary>
public sealed class CustomerGroup : SoftDeletableEntity<CustomerGroupId>
{
    public string Name { get; private set; }
    public string Code { get; private set; }
    public string Description { get; private set; }
    public decimal DiscountPercentage { get; private set; }
    public bool IsActive { get; private set; }
    public int Priority { get; private set; } // Lower number = higher priority
    public TenantId TenantId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public UserId CreatedBy { get; private set; }
    public UserId? UpdatedBy { get; private set; }

    private CustomerGroup(
        CustomerGroupId id,
        string name,
        string code,
        string description,
        decimal discountPercentage,
        bool isActive,
        int priority,
        TenantId tenantId,
        UserId createdBy) : base(id)
    {
        Name = name;
        Code = code;
        Description = description;
        DiscountPercentage = discountPercentage;
        IsActive = isActive;
        Priority = priority;
        TenantId = tenantId;
        CreatedBy = createdBy;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public static CustomerGroup Create(
        string name,
        string code,
        string description,
        decimal discountPercentage,
        bool isActive,
        int priority,
        TenantId tenantId,
        UserId createdBy)
    {
        ValidateCustomerGroupData(name, code, description, discountPercentage, priority);

        var customerGroupId = CustomerGroupId.CreateNew();
        return new CustomerGroup(
            customerGroupId,
            name,
            code,
            description,
            discountPercentage,
            isActive,
            priority,
            tenantId,
            createdBy);
    }

    public void UpdateDetails(
        string name,
        string description,
        decimal discountPercentage,
        int priority,
        UserId updatedBy)
    {
        ValidateCustomerGroupData(name, Code, description, discountPercentage, priority);

        Name = name;
        Description = description;
        DiscountPercentage = discountPercentage;
        Priority = priority;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Activate(UserId updatedBy)
    {
        if (IsActive)
            throw new InvalidOperationException("Customer group is already active");

        IsActive = true;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate(UserId updatedBy)
    {
        if (!IsActive)
            throw new InvalidOperationException("Customer group is already inactive");

        IsActive = false;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static void ValidateCustomerGroupData(
        string name,
        string code,
        string description,
        decimal discountPercentage,
        int priority)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Customer group name is required", nameof(name));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Customer group code is required", nameof(code));

        if (name.Length > 255)
            throw new ArgumentException("Customer group name cannot exceed 255 characters", nameof(name));

        if (code.Length > 50)
            throw new ArgumentException("Customer group code cannot exceed 50 characters", nameof(code));

        if (!string.IsNullOrEmpty(description) && description.Length > 1000)
            throw new ArgumentException("Customer group description cannot exceed 1000 characters", nameof(description));

        if (discountPercentage < 0 || discountPercentage > 100)
            throw new ArgumentException("Discount percentage must be between 0 and 100", nameof(discountPercentage));

        if (priority < 0)
            throw new ArgumentException("Priority must be non-negative", nameof(priority));
    }
}