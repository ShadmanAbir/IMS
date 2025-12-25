using FluentValidation;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.ValueObjects;
using IMS.Api.Application.Common.DTOs;

namespace IMS.Api.Application.Queries.Warehouses;

/// <summary>
/// Query to get a single warehouse by ID
/// </summary>
public class GetWarehouseQuery : IQuery<Result<WarehouseDto>>
{
    public Guid Id { get; set; }
}

/// <summary>
/// Validator for GetWarehouseQuery
/// </summary>
public class GetWarehouseQueryValidator : AbstractValidator<GetWarehouseQuery>
{
    public GetWarehouseQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Warehouse ID is required");
    }
}

/// <summary>
/// Handler for GetWarehouseQuery
/// </summary>
public class GetWarehouseQueryHandler : IQueryHandler<GetWarehouseQuery, Result<WarehouseDto>>
{
    private readonly IWarehouseRepository _warehouseRepository;

    public GetWarehouseQueryHandler(IWarehouseRepository warehouseRepository)
    {
        _warehouseRepository = warehouseRepository;
    }

    public async Task<Result<WarehouseDto>> Handle(GetWarehouseQuery request, CancellationToken cancellationToken)
    {
        var warehouseId = WarehouseId.Create(request.Id);
        var warehouse = await _warehouseRepository.GetByIdAsync(warehouseId);

        if (warehouse == null)
        {
            return Result<WarehouseDto>.Failure("WAREHOUSE_NOT_FOUND", "Warehouse not found");
        }

        var warehouseDto = new WarehouseDto
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
        };

        return Result<WarehouseDto>.Success(warehouseDto);
    }
}