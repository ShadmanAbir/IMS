using System.ComponentModel.DataAnnotations;

namespace IMS.Api.Infrastructure.Data.DTOs;

/// <summary>
/// Custom validation attributes for DTOs
/// </summary>
public class PositiveDecimalAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is decimal decimalValue)
        {
            return decimalValue > 0;
        }
        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be a positive decimal value.";
    }
}

/// <summary>
/// Validates that a decimal value is non-negative
/// </summary>
public class NonNegativeDecimalAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is decimal decimalValue)
        {
            return decimalValue >= 0;
        }
        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be a non-negative decimal value.";
    }
}

/// <summary>
/// Validates that a date is in the future
/// </summary>
public class FutureDateAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is DateTime dateValue)
        {
            return dateValue > DateTime.UtcNow;
        }
        if (value is DateTime? nullableDateValue)
        {
            return !nullableDateValue.HasValue || nullableDateValue.Value > DateTime.UtcNow;
        }
        return true; // Allow null values
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be a future date.";
    }
}

/// <summary>
/// Validates SKU format
/// </summary>
public class SKUFormatAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is string sku)
        {
            // SKU should be alphanumeric with hyphens, 3-50 characters
            return !string.IsNullOrWhiteSpace(sku) && 
                   sku.Length >= 3 && 
                   sku.Length <= 50 &&
                   System.Text.RegularExpressions.Regex.IsMatch(sku, @"^[A-Za-z0-9\-]+$");
        }
        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be 3-50 characters long and contain only letters, numbers, and hyphens.";
    }
}