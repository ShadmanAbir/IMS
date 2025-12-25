using FluentValidation;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;
using IMS.Api.Application.Common.DTOs;

namespace IMS.Api.Application.Queries.Warehouses;

/// <summary>
/// Query to get a paginated list of warehouses
/// </summary>
public class GetWarehousesQuery : IQuery<Result<PagedResult<WarehouseDto>>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public bool IncludeDeleted { get; set; } = false;
}

/// <summary>
/// Validator for GetWarehousesQuery
/// </summary>
public class GetWarehousesQueryValidator : AbstractValidator<GetWarehousesQuery>
{
    public GetWarehousesQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(100)
            .WithMessage("Page size must be between 1 and 100");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(255)
            .When(x => !string.IsNullOrEmpty(x.SearchTerm))
            .WithMessage("Search term must not exceed 255 characters");
    }
}

/// <summary>
/// Handler for GetWarehousesQuery
/// </summary>
public class GetWarehousesQueryHandler : IQueryHandler<GetWarehousesQuery, Result<PagedResult<WarehouseDto>>>
{
    private readonly IWarehouseRepository _warehouseRepository;

    public GetWarehousesQueryHandler(IWarehouseRepository warehouseRepository)
    {
        _warehouseRepository = warehouseRepository;
    }

    public async Task<Result<PagedResult<WarehouseDto>>> Handle(GetWarehousesQuery request, CancellationToken cancellationToken)
    {
        var pagedWarehouses = await _warehouseRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.SearchTerm,
            request.IsActive,
            request.IncludeDeleted);

        var warehouseDtos = pagedWarehouses.Items.Select(warehouse => new WarehouseDto
        {
            Id = warehouse.Id.Value,
            Name = warehouse.Name,
            Code = warehouse.Code,
            Description = warehouse.Description,
            Address = warehouse.Address,
            City = warehouse.City,
            State = warehouse.State,
            Country = warehouse.Country,
            PostalCode = warehouse.PostalCode,
            Latitude = warehouse.Latitude,
            Longitude = warehouse.Longitude,
            IsActive = warehouse.IsActive,
            TenantId = warehouse.TenantId.Value,
            CreatedAtUtc = warehouse.CreatedAtUtc,
            UpdatedAtUtc = warehouse.UpdatedAtUtc,
            IsDeleted = warehouse.IsDeleted,
            DeletedAtUtc = warehouse.DeletedAtUtc,
            DeletedBy = warehouse.DeletedBy?.Value
        }).ToList();

        var result = new PagedResult<WarehouseDto>(
            warehouseDtos,
            pagedWarehouses.TotalCount,
            pagedWarehouses.PageNumber,
            pagedWarehouses.PageSize);

        return Result<PagedResult<WarehouseDto>>.Success(result);
    }
}