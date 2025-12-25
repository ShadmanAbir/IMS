using IMS.Api.Domain.Common;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Domain.Entities;

/// <summary>
/// Pricing tier entity for managing different pricing levels
/// </summary>
public sealed class PricingTier : SoftDeletableEntity<PricingTierId>
{
    public string Name { get; private set; }
    public string Code { get; private set; }
    public string Description { get; private set; }
    public decimal MarkupPercentage { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; }
    public int Priority { get; private set; } // Lower number = higher priority
    public TenantId TenantId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public UserId CreatedBy { get; private set; }
    public UserId? UpdatedBy { get; private set; }

    // Navigation properties
    private readonly List<CustomerGroup> _customerGroups = new();
    public IReadOnlyCollection<CustomerGroup> CustomerGroups => _customerGroups.AsReadOnly();

    private PricingTier(
        PricingTierId id,
        string name,
        string code,
        string description,
        decimal markupPercentage,
        bool isDefault,
        bool isActive,
        int priority,
        TenantId tenantId,
        UserId createdBy) : base(id)
    {
        Name = name;
        Code = code;
        Description = description;
        MarkupPercentage = markupPercentage;
        IsDefault = isDefault;
        IsActive = isActive;
        Priority = priority;
        TenantId = tenantId;
        CreatedBy = createdBy;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public static PricingTier Create(
        string name,
        string code,
        string description,
        decimal markupPercentage,
        bool isDefault,
        bool isActive,
        int priority,
        TenantId tenantId,
        UserId createdBy)
    {
        ValidatePricingTierData(name, code, description, markupPercentage, priority);

        var pricingTierId = PricingTierId.CreateNew();
        return new PricingTier(
            pricingTierId,
            name,
            code,
            description,
            markupPercentage,
            isDefault,
            isActive,
            priority,
            tenantId,
            createdBy);
    }

    public void UpdateDetails(
        string name,
        string description,
        decimal markupPercentage,
        int priority,
        UserId updatedBy)
    {
        ValidatePricingTierData(name, Code, description, markupPercentage, priority);

        Name = name;
        Description = description;
        MarkupPercentage = markupPercentage;
        Priority = priority;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetAsDefault(UserId updatedBy)
    {
        if (IsDefault)
            throw new InvalidOperationException("Pricing tier is already set as default");

        IsDefault = true;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RemoveAsDefault(UserId updatedBy)
    {
        if (!IsDefault)
            throw new InvalidOperationException("Pricing tier is not set as default");

        IsDefault = false;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Activate(UserId updatedBy)
    {
        if (IsActive)
            throw new InvalidOperationException("Pricing tier is already active");

        IsActive = true;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate(UserId updatedBy)
    {
        if (!IsActive)
            throw new InvalidOperationException("Pricing tier is already inactive");

        if (IsDefault)
            throw new InvalidOperationException("Cannot deactivate default pricing tier");

        IsActive = false;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AssignCustomerGroup(CustomerGroup customerGroup)
    {
        if (customerGroup == null)
            throw new ArgumentNullException(nameof(customerGroup));

        if (_customerGroups.Any(cg => cg.Id == customerGroup.Id))
            throw new InvalidOperationException("Customer group is already assigned to this pricing tier");

        _customerGroups.Add(customerGroup);
    }

    public void RemoveCustomerGroup(CustomerGroupId customerGroupId)
    {
        var customerGroup = _customerGroups.FirstOrDefault(cg => cg.Id == customerGroupId);
        if (customerGroup != null)
        {
            _customerGroups.Remove(customerGroup);
        }
    }

    public decimal CalculatePrice(decimal basePrice)
    {
        if (basePrice < 0)
            throw new ArgumentException("Base price cannot be negative", nameof(basePrice));

        return basePrice * (1 + MarkupPercentage / 100);
    }

    private static void ValidatePricingTierData(
        string name,
        string code,
        string description,
        decimal markupPercentage,
        int priority)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Pricing tier name is required", nameof(name));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Pricing tier code is required", nameof(code));

        if (name.Length > 255)
            throw new ArgumentException("Pricing tier name cannot exceed 255 characters", nameof(name));

        if (code.Length > 50)
            throw new ArgumentException("Pricing tier code cannot exceed 50 characters", nameof(code));

        if (!string.IsNullOrEmpty(description) && description.Length > 1000)
            throw new ArgumentException("Pricing tier description cannot exceed 1000 characters", nameof(description));

        if (markupPercentage < -100)
            throw new ArgumentException("Markup percentage cannot be less than -100%", nameof(markupPercentage));

        if (priority < 0)
            throw new ArgumentException("Priority must be non-negative", nameof(priority));
    }
}