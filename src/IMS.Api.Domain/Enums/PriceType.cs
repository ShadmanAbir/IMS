namespace IMS.Api.Domain.Enums;

/// <summary>
/// Represents the type of pricing calculation
/// </summary>
public enum PriceType
{
    /// <summary>
    /// Fixed price per unit
    /// </summary>
    Fixed = 1,

    /// <summary>
    /// Price calculated based on cost plus markup percentage
    /// </summary>
    CostPlusMarkup = 2,

    /// <summary>
    /// Price calculated based on cost plus fixed margin
    /// </summary>
    CostPlusMargin = 3,

    /// <summary>
    /// Tiered pricing based on quantity breaks
    /// </summary>
    Tiered = 4
}