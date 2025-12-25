using IMS.Api.Domain.Common;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Domain.Aggregates;

/// <summary>
/// Warehouse aggregate root representing a physical or logical location where inventory is stored
/// </summary>
public sealed class Warehouse : SoftDeletableAggregateRoot<WarehouseId>
{
    public string Name { get; private set; }
    public string Code { get; private set; }
    public string Description { get; private set; }
    public string Address { get; private set; }
    public string City { get; private set; }
    public string State { get; private set; }
    public string Country { get; private set; }
    public string PostalCode { get; private set; }
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public bool IsActive { get; private set; }
    public decimal? MaxCapacity { get; private set; }
    public string? CapacityUnit { get; private set; }
    public TenantId TenantId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public UserId CreatedBy { get; private set; }
    public UserId? UpdatedBy { get; private set; }

    private Warehouse(
        WarehouseId id,
        string name,
        string code,
        string description,
        string address,
        string city,
        string state,
        string country,
        string postalCode,
        decimal? latitude,
        decimal? longitude,
        bool isActive,
        decimal? maxCapacity,
        string? capacityUnit,
        TenantId tenantId,
        UserId createdBy) : base(id)
    {
        Name = name;
        Code = code;
        Description = description;
        Address = address;
        City = city;
        State = state;
        Country = country;
        PostalCode = postalCode;
        Latitude = latitude;
        Longitude = longitude;
        IsActive = isActive;
        MaxCapacity = maxCapacity;
        CapacityUnit = capacityUnit;
        TenantId = tenantId;
        CreatedBy = createdBy;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public static Warehouse Create(
        string name,
        string code,
        string description,
        string address,
        string city,
        string state,
        string country,
        string postalCode,
        decimal? latitude,
        decimal? longitude,
        bool isActive,
        decimal? maxCapacity,
        string? capacityUnit,
        TenantId tenantId,
        UserId createdBy)
    {
        ValidateWarehouseData(name, code, address, city, state, country, postalCode, latitude, longitude, maxCapacity, capacityUnit);

        var warehouseId = WarehouseId.CreateNew();
        return new Warehouse(
            warehouseId,
            name,
            code,
            description,
            address,
            city,
            state,
            country,
            postalCode,
            latitude,
            longitude,
            isActive,
            maxCapacity,
            capacityUnit,
            tenantId,
            createdBy);
    }

    public void UpdateDetails(
        string name,
        string description,
        string address,
        string city,
        string state,
        string country,
        string postalCode,
        decimal? latitude,
        decimal? longitude,
        UserId updatedBy)
    {
        ValidateWarehouseData(name, Code, address, city, state, country, postalCode, latitude, longitude, MaxCapacity, CapacityUnit);

        Name = name;
        Description = description;
        Address = address;
        City = city;
        State = state;
        Country = country;
        PostalCode = postalCode;
        Latitude = latitude;
        Longitude = longitude;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateCapacity(decimal? maxCapacity, string? capacityUnit, UserId updatedBy)
    {
        if (maxCapacity.HasValue && maxCapacity.Value < 0)
            throw new ArgumentException("Capacity cannot be negative", nameof(maxCapacity));

        if (maxCapacity.HasValue && string.IsNullOrWhiteSpace(capacityUnit))
            throw new ArgumentException("Capacity unit is required when capacity is specified", nameof(capacityUnit));

        MaxCapacity = maxCapacity;
        CapacityUnit = capacityUnit;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Activate(UserId updatedBy)
    {
        if (IsActive)
            throw new InvalidOperationException("Warehouse is already active");

        IsActive = true;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate(UserId updatedBy)
    {
        if (!IsActive)
            throw new InvalidOperationException("Warehouse is already inactive");

        IsActive = false;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetActiveStatus(bool isActive, UserId updatedBy)
    {
        IsActive = isActive;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool HasCapacityLimit()
    {
        return MaxCapacity.HasValue && MaxCapacity.Value > 0;
    }

    public bool IsWithinCapacity(decimal currentUtilization)
    {
        if (!HasCapacityLimit())
            return true;

        return currentUtilization <= MaxCapacity.Value;
    }

    private static void ValidateWarehouseData(
        string name,
        string code,
        string address,
        string city,
        string state,
        string country,
        string postalCode,
        decimal? latitude,
        decimal? longitude,
        decimal? maxCapacity,
        string? capacityUnit)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Warehouse name is required", nameof(name));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Warehouse code is required", nameof(code));

        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address is required", nameof(address));

        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City is required", nameof(city));

        if (string.IsNullOrWhiteSpace(state))
            throw new ArgumentException("State is required", nameof(state));

        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country is required", nameof(country));

        if (string.IsNullOrWhiteSpace(postalCode))
            throw new ArgumentException("Postal code is required", nameof(postalCode));

        if (latitude.HasValue && (latitude.Value < -90 || latitude.Value > 90))
            throw new ArgumentException("Latitude must be between -90 and 90 degrees", nameof(latitude));

        if (longitude.HasValue && (longitude.Value < -180 || longitude.Value > 180))
            throw new ArgumentException("Longitude must be between -180 and 180 degrees", nameof(longitude));

        if (maxCapacity.HasValue && maxCapacity.Value < 0)
            throw new ArgumentException("Capacity cannot be negative", nameof(maxCapacity));

        if (maxCapacity.HasValue && string.IsNullOrWhiteSpace(capacityUnit))
            throw new ArgumentException("Capacity unit is required when capacity is specified", nameof(capacityUnit));
    }
}