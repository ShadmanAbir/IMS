using FluentValidation;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.ValueObjects;
using IMS.Api.Application.Common.DTOs;

namespace IMS.Api.Application.Commands.Warehouses;

/// <summary>
/// Command to update an existing warehouse
/// </summary>
public class UpdateWarehouseCommand : ICommand<Result<WarehouseDto>>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid UpdatedBy { get; set; }
}

/// <summary>
/// Validator for UpdateWarehouseCommand
/// </summary>
public class UpdateWarehouseCommandValidator : AbstractValidator<UpdateWarehouseCommand>
{
    public UpdateWarehouseCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Warehouse ID is required");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(255)
            .WithMessage("Warehouse name is required and must not exceed 255 characters");

        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(50)
            .WithMessage("Warehouse code is required and must not exceed 50 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Description must not exceed 1000 characters");

        RuleFor(x => x.Address)
            .NotEmpty()
            .MaximumLength(500)
            .WithMessage("Address is required and must not exceed 500 characters");

        RuleFor(x => x.City)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("City is required and must not exceed 100 characters");

        RuleFor(x => x.State)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("State is required and must not exceed 100 characters");

        RuleFor(x => x.Country)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Country is required and must not exceed 100 characters");

        RuleFor(x => x.PostalCode)
            .NotEmpty()
            .MaximumLength(20)
            .WithMessage("Postal code is required and must not exceed 20 characters");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90)
            .When(x => x.Latitude.HasValue)
            .WithMessage("Latitude must be between -90 and 90 degrees");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180)
            .When(x => x.Longitude.HasValue)
            .WithMessage("Longitude must be between -180 and 180 degrees");
    }
}

/// <summary>
/// Handler for UpdateWarehouseCommand
/// </summary>
public class UpdateWarehouseCommandHandler : ICommandHandler<UpdateWarehouseCommand, Result<WarehouseDto>>
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateWarehouseCommandHandler(
        IWarehouseRepository warehouseRepository,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _warehouseRepository = warehouseRepository;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<WarehouseDto>> Handle(UpdateWarehouseCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var warehouseId = WarehouseId.Create(request.Id);
            var warehouse = await _warehouseRepository.GetByIdAsync(warehouseId);

            if (warehouse == null)
            {
                return Result<WarehouseDto>.Failure("WAREHOUSE_NOT_FOUND", "Warehouse not found");
            }

            var tenantId = _tenantContext.CurrentTenantId;

            // Check if warehouse code already exists (excluding current warehouse)
            var existingWarehouse = await _warehouseRepository.ExistsWithCodeAsync(request.Code, tenantId, warehouseId);
            if (existingWarehouse)
            {
                return Result<WarehouseDto>.Failure("DUPLICATE_CODE", "A warehouse with this code already exists");
            }

            // Update warehouse details
            var updatedBy = UserId.Create(request.UpdatedBy);
            warehouse.UpdateDetails(
                request.Name,
                request.Description,
                request.Address,
                request.City,
                request.State,
                request.Country,
                request.PostalCode,
                request.Latitude,
                request.Longitude,
                updatedBy);

            warehouse.SetActiveStatus(request.IsActive, updatedBy);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Return the updated warehouse as DTO
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
        catch (ArgumentException ex)
        {
            return Result<WarehouseDto>.Failure("INVALID_INPUT", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Result<WarehouseDto>.Failure("BUSINESS_RULE_VIOLATION", ex.Message);
        }
    }
}