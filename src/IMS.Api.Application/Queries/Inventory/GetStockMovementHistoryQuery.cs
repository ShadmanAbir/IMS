using FluentValidation;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;

namespace IMS.Api.Application.Queries.Inventory;

/// <summary>
/// Query to get paginated stock movement history
/// </summary>
public class GetStockMovementHistoryQuery : IQuery<Result<PagedResult<StockMovementDto>>>
{
    public Guid? VariantId { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? MovementType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? ReferenceNumber { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Validator for GetStockMovementHistoryQuery
/// </summary>
public class GetStockMovementHistoryQueryValidator : AbstractValidator<GetStockMovementHistoryQuery>
{
    public GetStockMovementHistoryQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("End date must be after start date");

        RuleFor(x => x.MovementType)
            .Must(BeValidMovementType)
            .When(x => !string.IsNullOrEmpty(x.MovementType))
            .WithMessage("Invalid movement type");

        RuleFor(x => x.ReferenceNumber)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.ReferenceNumber))
            .WithMessage("Reference number must not exceed 100 characters");
    }

    private static bool BeValidMovementType(string? movementType)
    {
        if (string.IsNullOrEmpty(movementType))
            return true;

        var validTypes = new[] { "OpeningBalance", "Purchase", "Sale", "Refund", "Adjustment", "WriteOff", "Transfer" };
        return validTypes.Contains(movementType);
    }
}

/// <summary>
/// Handler for GetStockMovementHistoryQuery
/// </summary>
public class GetStockMovementHistoryQueryHandler : IQueryHandler<GetStockMovementHistoryQuery, Result<PagedResult<StockMovementDto>>>
{
    private readonly IStockMovementRepository _stockMovementRepository;

    public GetStockMovementHistoryQueryHandler(IStockMovementRepository stockMovementRepository)
    {
        _stockMovementRepository = stockMovementRepository;
    }

    public async Task<Result<PagedResult<StockMovementDto>>> Handle(GetStockMovementHistoryQuery request, CancellationToken cancellationToken)
    {
        // Create a query object for the repository
        var query = new StockMovementQuery
        {
            VariantId = request.VariantId.HasValue ? Domain.ValueObjects.VariantId.Create(request.VariantId.Value) : null,
            WarehouseId = request.WarehouseId.HasValue ? Domain.ValueObjects.WarehouseId.Create(request.WarehouseId.Value) : null,
            MovementType = !string.IsNullOrEmpty(request.MovementType) ? Enum.Parse<Domain.Enums.MovementType>(request.MovementType) : null,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            ReferenceNumber = request.ReferenceNumber,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        var pagedMovements = await _stockMovementRepository.GetMovementHistoryAsync(query);

        var dtos = pagedMovements.Items.Select(movement => new StockMovementDto
        {
            Id = movement.Id.Value,
            Type = movement.Type.ToString(),
            Quantity = movement.Quantity,
            RunningBalance = movement.RunningBalance,
            Reason = movement.Reason,
            ActorId = movement.ActorId.Value,
            TimestampUtc = movement.TimestampUtc,
            ReferenceNumber = movement.ReferenceNumber ?? string.Empty,
            Metadata = movement.Metadata?.ToString() ?? string.Empty,
            EntryType = movement.EntryType.ToString(),
            PairedMovementId = movement.PairedMovementId?.Value
        }).ToList();

        var result = new PagedResult<StockMovementDto>(
            dtos,
            pagedMovements.TotalCount,
            pagedMovements.PageNumber,
            pagedMovements.PageSize);

        return Result<PagedResult<StockMovementDto>>.Success(result);
    }
}

/// <summary>
/// Query object for stock movement repository
/// </summary>
public class StockMovementQuery
{
    public Domain.ValueObjects.VariantId? VariantId { get; set; }
    public Domain.ValueObjects.WarehouseId? WarehouseId { get; set; }
    public Domain.Enums.MovementType? MovementType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? ReferenceNumber { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int ActorId { get; set; }
}