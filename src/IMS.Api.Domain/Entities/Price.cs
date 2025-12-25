using IMS.Api.Domain.Common;
using IMS.Api.Domain.Enums;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Domain.Entities;

/// <summary>
/// Price entity representing pricing information for variants with unit-aware calculations
/// </summary>
public sealed class Price : SoftDeletableEntity<PriceId>
{
    public VariantId VariantId { get; private set; }
    public PricingTierId? PricingTierId { get; private set; }
    public CustomerGroupId? CustomerGroupId { get; private set; }
    public PriceType PriceType { get; private set; }
    public decimal BasePrice { get; private set; }
    public decimal? CostPrice { get; private set; }
    public decimal? MarkupPercentage { get; private set; }
    public decimal? FixedMargin { get; private set; }
    public UnitOfMeasure PriceUnit { get; private set; }
    public decimal MinimumQuantity { get; private set; }
    public decimal? MaximumQuantity { get; private set; }
    public DateTime EffectiveFromUtc { get; private set; }
    public DateTime? EffectiveToUtc { get; private set; }
    public bool IsActive { get; private set; }
    public string Currency { get; private set; }
    public TenantId TenantId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public UserId CreatedBy { get; private set; }
    public UserId? UpdatedBy { get; private set; }

    private Price(
        PriceId id,
        VariantId variantId,
        PricingTierId? pricingTierId,
        CustomerGroupId? customerGroupId,
        PriceType priceType,
        decimal basePrice,
        decimal? costPrice,
        decimal? markupPercentage,
        decimal? fixedMargin,
        UnitOfMeasure priceUnit,
        decimal minimumQuantity,
        decimal? maximumQuantity,
        DateTime effectiveFromUtc,
        DateTime? effectiveToUtc,
        bool isActive,
        string currency,
        TenantId tenantId,
        UserId createdBy) : base(id)
    {
        VariantId = variantId;
        PricingTierId = pricingTierId;
        CustomerGroupId = customerGroupId;
        PriceType = priceType;
        BasePrice = basePrice;
        CostPrice = costPrice;
        MarkupPercentage = markupPercentage;
        FixedMargin = fixedMargin;
        PriceUnit = priceUnit;
        MinimumQuantity = minimumQuantity;
        MaximumQuantity = maximumQuantity;
        EffectiveFromUtc = effectiveFromUtc;
        EffectiveToUtc = effectiveToUtc;
        IsActive = isActive;
        Currency = currency;
        TenantId = tenantId;
        CreatedBy = createdBy;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public static Price CreateFixedPrice(
        VariantId variantId,
        decimal basePrice,
        UnitOfMeasure priceUnit,
        decimal minimumQuantity,
        decimal? maximumQuantity,
        DateTime effectiveFromUtc,
        DateTime? effectiveToUtc,
        string currency,
        TenantId tenantId,
        UserId createdBy,
        PricingTierId? pricingTierId = null,
        CustomerGroupId? customerGroupId = null)
    {
        ValidatePriceData(variantId, basePrice, priceUnit, minimumQuantity, maximumQuantity, 
            effectiveFromUtc, effectiveToUtc, currency, tenantId, createdBy);

        var priceId = PriceId.CreateNew();
        return new Price(
            priceId,
            variantId,
            pricingTierId,
            customerGroupId,
            PriceType.Fixed,
            basePrice,
            null, // No cost price for fixed pricing
            null, // No markup for fixed pricing
            null, // No fixed margin for fixed pricing
            priceUnit,
            minimumQuantity,
            maximumQuantity,
            effectiveFromUtc,
            effectiveToUtc,
            true,
            currency,
            tenantId,
            createdBy);
    }

    public static Price CreateCostPlusMarkupPrice(
        VariantId variantId,
        decimal costPrice,
        decimal markupPercentage,
        UnitOfMeasure priceUnit,
        decimal minimumQuantity,
        decimal? maximumQuantity,
        DateTime effectiveFromUtc,
        DateTime? effectiveToUtc,
        string currency,
        TenantId tenantId,
        UserId createdBy,
        PricingTierId? pricingTierId = null,
        CustomerGroupId? customerGroupId = null)
    {
        ValidatePriceData(variantId, costPrice, priceUnit, minimumQuantity, maximumQuantity, 
            effectiveFromUtc, effectiveToUtc, currency, tenantId, createdBy);

        if (markupPercentage < -100)
            throw new ArgumentException("Markup percentage cannot be less than -100%", nameof(markupPercentage));

        var basePrice = costPrice * (1 + markupPercentage / 100);
        var priceId = PriceId.CreateNew();
        
        return new Price(
            priceId,
            variantId,
            pricingTierId,
            customerGroupId,
            PriceType.CostPlusMarkup,
            basePrice,
            costPrice,
            markupPercentage,
            null, // No fixed margin for markup pricing
            priceUnit,
            minimumQuantity,
            maximumQuantity,
            effectiveFromUtc,
            effectiveToUtc,
            true,
            currency,
            tenantId,
            createdBy);
    }

    public static Price CreateCostPlusMarginPrice(
        VariantId variantId,
        decimal costPrice,
        decimal fixedMargin,
        UnitOfMeasure priceUnit,
        decimal minimumQuantity,
        decimal? maximumQuantity,
        DateTime effectiveFromUtc,
        DateTime? effectiveToUtc,
        string currency,
        TenantId tenantId,
        UserId createdBy,
        PricingTierId? pricingTierId = null,
        CustomerGroupId? customerGroupId = null)
    {
        ValidatePriceData(variantId, costPrice, priceUnit, minimumQuantity, maximumQuantity, 
            effectiveFromUtc, effectiveToUtc, currency, tenantId, createdBy);

        var basePrice = costPrice + fixedMargin;
        if (basePrice < 0)
            throw new ArgumentException("Fixed margin cannot result in negative price", nameof(fixedMargin));

        var priceId = PriceId.CreateNew();
        
        return new Price(
            priceId,
            variantId,
            pricingTierId,
            customerGroupId,
            PriceType.CostPlusMargin,
            basePrice,
            costPrice,
            null, // No markup percentage for margin pricing
            fixedMargin,
            priceUnit,
            minimumQuantity,
            maximumQuantity,
            effectiveFromUtc,
            effectiveToUtc,
            true,
            currency,
            tenantId,
            createdBy);
    }

    public void UpdateFixedPrice(decimal newBasePrice, UserId updatedBy)
    {
        if (PriceType != PriceType.Fixed)
            throw new InvalidOperationException("Can only update base price for fixed price type");

        if (newBasePrice < 0)
            throw new ArgumentException("Price cannot be negative", nameof(newBasePrice));

        BasePrice = newBasePrice;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateCostAndMarkup(decimal newCostPrice, decimal newMarkupPercentage, UserId updatedBy)
    {
        if (PriceType != PriceType.CostPlusMarkup)
            throw new InvalidOperationException("Can only update cost and markup for cost plus markup price type");

        if (newCostPrice < 0)
            throw new ArgumentException("Cost price cannot be negative", nameof(newCostPrice));

        if (newMarkupPercentage < -100)
            throw new ArgumentException("Markup percentage cannot be less than -100%", nameof(newMarkupPercentage));

        CostPrice = newCostPrice;
        MarkupPercentage = newMarkupPercentage;
        BasePrice = newCostPrice * (1 + newMarkupPercentage / 100);
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateCostAndMargin(decimal newCostPrice, decimal newFixedMargin, UserId updatedBy)
    {
        if (PriceType != PriceType.CostPlusMargin)
            throw new InvalidOperationException("Can only update cost and margin for cost plus margin price type");

        if (newCostPrice < 0)
            throw new ArgumentException("Cost price cannot be negative", nameof(newCostPrice));

        var newBasePrice = newCostPrice + newFixedMargin;
        if (newBasePrice < 0)
            throw new ArgumentException("Fixed margin cannot result in negative price", nameof(newFixedMargin));

        CostPrice = newCostPrice;
        FixedMargin = newFixedMargin;
        BasePrice = newBasePrice;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateQuantityRange(decimal minimumQuantity, decimal? maximumQuantity, UserId updatedBy)
    {
        if (minimumQuantity < 0)
            throw new ArgumentException("Minimum quantity cannot be negative", nameof(minimumQuantity));

        if (maximumQuantity.HasValue && maximumQuantity.Value <= minimumQuantity)
            throw new ArgumentException("Maximum quantity must be greater than minimum quantity", nameof(maximumQuantity));

        MinimumQuantity = minimumQuantity;
        MaximumQuantity = maximumQuantity;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateEffectivePeriod(DateTime effectiveFromUtc, DateTime? effectiveToUtc, UserId updatedBy)
    {
        if (effectiveToUtc.HasValue && effectiveToUtc.Value <= effectiveFromUtc)
            throw new ArgumentException("Effective to date must be after effective from date", nameof(effectiveToUtc));

        EffectiveFromUtc = effectiveFromUtc;
        EffectiveToUtc = effectiveToUtc;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Activate(UserId updatedBy)
    {
        if (IsActive)
            throw new InvalidOperationException("Price is already active");

        IsActive = true;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate(UserId updatedBy)
    {
        if (!IsActive)
            throw new InvalidOperationException("Price is already inactive");

        IsActive = false;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool IsEffectiveAt(DateTime dateTimeUtc)
    {
        return dateTimeUtc >= EffectiveFromUtc && 
               (!EffectiveToUtc.HasValue || dateTimeUtc <= EffectiveToUtc.Value);
    }

    public bool IsValidForQuantity(decimal quantity)
    {
        return quantity >= MinimumQuantity && 
               (!MaximumQuantity.HasValue || quantity <= MaximumQuantity.Value);
    }

    public decimal CalculatePriceForQuantity(decimal quantity, UnitOfMeasure requestedUnit, UnitConversion? unitConversion = null)
    {
        if (!IsValidForQuantity(quantity))
            throw new ArgumentException($"Quantity {quantity} is not valid for this price (min: {MinimumQuantity}, max: {MaximumQuantity})", nameof(quantity));

        var pricePerUnit = BasePrice;

        // Apply unit conversion if needed
        if (requestedUnit != PriceUnit && unitConversion != null)
        {
            pricePerUnit = unitConversion.Convert(pricePerUnit);
        }

        return pricePerUnit * quantity;
    }

    public decimal GetMarginAmount()
    {
        if (!CostPrice.HasValue)
            return 0;

        return BasePrice - CostPrice.Value;
    }

    public decimal GetMarginPercentage()
    {
        if (!CostPrice.HasValue || CostPrice.Value == 0)
            return 0;

        return ((BasePrice - CostPrice.Value) / CostPrice.Value) * 100;
    }

    private static void ValidatePriceData(
        VariantId variantId,
        decimal price,
        UnitOfMeasure priceUnit,
        decimal minimumQuantity,
        decimal? maximumQuantity,
        DateTime effectiveFromUtc,
        DateTime? effectiveToUtc,
        string currency,
        TenantId tenantId,
        UserId createdBy)
    {
        if (variantId == null)
            throw new ArgumentNullException(nameof(variantId));

        if (price < 0)
            throw new ArgumentException("Price cannot be negative", nameof(price));

        if (priceUnit == null)
            throw new ArgumentNullException(nameof(priceUnit));

        if (minimumQuantity < 0)
            throw new ArgumentException("Minimum quantity cannot be negative", nameof(minimumQuantity));

        if (maximumQuantity.HasValue && maximumQuantity.Value <= minimumQuantity)
            throw new ArgumentException("Maximum quantity must be greater than minimum quantity", nameof(maximumQuantity));

        if (effectiveToUtc.HasValue && effectiveToUtc.Value <= effectiveFromUtc)
            throw new ArgumentException("Effective to date must be after effective from date", nameof(effectiveToUtc));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required", nameof(currency));

        if (currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-character ISO code", nameof(currency));

        if (tenantId == null)
            throw new ArgumentNullException(nameof(tenantId));

        if (createdBy == null)
            throw new ArgumentNullException(nameof(createdBy));
    }
}