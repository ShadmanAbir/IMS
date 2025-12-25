namespace IMS.Api.Application.Common.DTOs;

/// <summary>
/// DTO for refund information
/// </summary>
public class RefundDto
{
    public Guid Id { get; set; }
    public Guid VariantId { get; set; }
    public string VariantSku { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string OriginalSaleReference { get; set; } = string.Empty;
    public Guid ProcessedBy { get; set; }
    public string ProcessedByName { get; set; } = string.Empty;
    public DateTime ProcessedAtUtc { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public decimal RunningBalance { get; set; }
}

/// <summary>
/// DTO for refund request
/// </summary>
public class CreateRefundDto
{
    public Guid VariantId { get; set; }
    public Guid WarehouseId { get; set; }
    public decimal Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string OriginalSaleReference { get; set; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// DTO for refund validation information
/// </summary>
public class RefundValidationDto
{
    public string OriginalSaleReference { get; set; } = string.Empty;
    public decimal OriginalSaleQuantity { get; set; }
    public decimal TotalRefundedQuantity { get; set; }
    public decimal RemainingRefundableQuantity { get; set; }
    public DateTime OriginalSaleDate { get; set; }
    public List<RefundHistoryDto> RefundHistory { get; set; } = new();
    public bool CanRefund { get; set; }
    public string ValidationMessage { get; set; } = string.Empty;
}

/// <summary>
/// DTO for refund history item
/// </summary>
public class RefundHistoryDto
{
    public Guid Id { get; set; }
    public decimal Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime ProcessedAtUtc { get; set; }
    public string ProcessedByName { get; set; } = string.Empty;
}

/// <summary>
/// DTO for sale information related to refunds
/// </summary>
public class SaleInfoDto
{
    public string ReferenceNumber { get; set; } = string.Empty;
    public Guid VariantId { get; set; }
    public string VariantSku { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public DateTime SaleDate { get; set; }
    public string ProcessedByName { get; set; } = string.Empty;
    public decimal TotalRefunded { get; set; }
    public decimal RemainingRefundable { get; set; }
}