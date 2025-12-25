using FluentValidation;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.Aggregates;
using IMS.Api.Domain.ValueObjects;
using IMS.Api.Application.Common.DTOs;

namespace IMS.Api.Application.Commands.Reservations;

/// <summary>
/// Command to create a new reservation
/// </summary>
public class CreateReservationCommand : ICommand<Result<ReservationDto>>
{
    public Guid VariantId { get; set; }
    public Guid WarehouseId { get; set; }
    public decimal Quantity { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
}

/// <summary>
/// Validator for CreateReservationCommand
/// </summary>
public class CreateReservationCommandValidator : AbstractValidator<CreateReservationCommand>
{
    public CreateReservationCommandValidator()
    {
        RuleFor(x => x.VariantId)
            .NotEmpty()
            .WithMessage("VariantId is required");

        RuleFor(x => x.WarehouseId)
            .NotEmpty()
            .WithMessage("WarehouseId is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Reservation quantity must be positive");

        RuleFor(x => x.ExpiresAtUtc)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Expiry date must be in the future");

        RuleFor(x => x.ReferenceNumber)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Reference number is required and must not exceed 100 characters");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(255)
            .WithMessage("Reason is required and must not exceed 255 characters");

        RuleFor(x => x.CreatedBy)
            .NotEmpty()
            .WithMessage("CreatedBy user ID is required");
    }
}

/// <summary>
/// Handler for CreateReservationCommand
/// </summary>
public class CreateReservationCommandHandler : ICommandHandler<CreateReservationCommand, Result<ReservationDto>>
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public CreateReservationCommandHandler(
        IReservationRepository reservationRepository,
        IInventoryRepository inventoryRepository,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _reservationRepository = reservationRepository;
        _inventoryRepository = inventoryRepository;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ReservationDto>> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var variantId = VariantId.Create(request.VariantId);
            var warehouseId = WarehouseId.Create(request.WarehouseId);
            var createdBy = UserId.Create(request.CreatedBy);
            var tenantId = _tenantContext.CurrentTenantId;

            // Check if sufficient stock is available
            var inventoryItem = await _inventoryRepository.GetByVariantAndWarehouseAsync(variantId, warehouseId);
            if (inventoryItem == null)
            {
                return Result<ReservationDto>.Failure("INVENTORY_NOT_FOUND", "Inventory item not found");
            }

            if (inventoryItem.AvailableStock < request.Quantity)
            {
                return Result<ReservationDto>.Failure("INSUFFICIENT_STOCK", 
                    $"Insufficient available stock. Available: {inventoryItem.AvailableStock}, Requested: {request.Quantity}");
            }

            // Create the reservation
            var reservation = Reservation.Create(
                variantId,
                warehouseId,
                request.Quantity,
                request.ExpiresAtUtc,
                request.ReferenceNumber,
                request.Reason,
                tenantId,
                createdBy);

            // Reserve the stock in inventory
            inventoryItem.ReserveStock(request.Quantity, reservation.Id);

            // Save the reservation
            await _reservationRepository.AddAsync(reservation);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Return the created reservation as DTO
            var reservationDto = new ReservationDto
            {
                Id = reservation.Id.Value,
                VariantId = reservation.VariantId.Value,
                WarehouseId = reservation.WarehouseId.Value,
                Quantity = reservation.Quantity,
                ExpiresAtUtc = reservation.ExpiresAtUtc,
                Status = reservation.Status.ToString(),
                ReferenceNumber = reservation.ReferenceNumber,
                Reason = reservation.Reason,
                CreatedBy = reservation.CreatedBy.Value,
                CreatedAtUtc = reservation.CreatedAtUtc,
                TenantId = reservation.TenantId.Value,
                IsDeleted = reservation.IsDeleted,
                DeletedAtUtc = reservation.DeletedAtUtc,
                DeletedBy = reservation.DeletedBy?.Value
            };

            return Result<ReservationDto>.Success(reservationDto);
        }
        catch (ArgumentException ex)
        {
            return Result<ReservationDto>.Failure("INVALID_INPUT", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Result<ReservationDto>.Failure("BUSINESS_RULE_VIOLATION", ex.Message);
        }
    }
}