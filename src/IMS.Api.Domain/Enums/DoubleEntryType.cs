namespace IMS.Api.Domain.Enums;

/// <summary>
/// Represents the double-entry accounting type for stock movements
/// </summary>
public enum DoubleEntryType
{
    /// <summary>
    /// Debit entry - increases stock (positive quantity)
    /// Used for: Purchases, Refunds, Positive Adjustments, Transfer In
    /// </summary>
    Debit = 1,
    
    /// <summary>
    /// Credit entry - decreases stock (negative quantity)
    /// Used for: Sales, Write-offs, Negative Adjustments, Transfer Out
    /// </summary>
    Credit = 2
}