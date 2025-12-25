using FluentValidation;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.ValueObjects;
using IMS.Api.Application.Common.DTOs;

namespace IMS.Api.Application.Commands.Reservations;

/// <summary>
/// Command to modify an existing reservation
/// </summary>
public class ModifyReservationCommand : ICommand<Result<ReservationDto>>
{
    public Guid Id { get; set; }
    public decimal? NewQuantity { get; set; }
    public DateTime? NewExpiresAtUtc { get; set; }
    public string? NewReason { get; set; }
    public Guid ModifiedBy { get; set; }
}

/// <summary>
/// Validator for ModifyReservationCommand
/// </summary>
public class ModifyReservationCommandValidator : AbstractValidator<ModifyReservationCommand>
{
    public ModifyReservationCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Reservation ID is required");

        RuleFor(x => x.NewQuantity)
            .GreaterThan(0)
            .When(x => x.NewQuantity.HasValue)
            .WithMessage("New quantity must be positive");

        RuleFor(x => x.NewExpiresAtUtc)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.NewExpiresAtUtc.HasValue)
            .WithMessage("New expiry date must be in the future");

        RuleFor(x => x.NewReason)
            .MaximumLength(255)
            .When(x => !string.IsNullOrEmpty(x.NewReason))
            .WithMessage("New reason must not exceed 255 characters");

        RuleFor(x => x.ModifiedBy)
            .NotEmpty()
            .WithMessage("ModifiedBy user ID is required");

        RuleFor(x => x)
            .Must(x => x.NewQuantity.HasValue || x.NewExpiresAtUtc.HasValue || !string.IsNullOrEmpty(x.NewReason))
            .WithMessage("At least one field must be provided for modification");
    }
}

/// <summary>
/// Handler for ModifyReservationCommand
/// </summary>
public class ModifyReservationCommandHandler : ICommandHandler<ModifyReservationCommand, Result<ReservationDto>>
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ModifyReservationCommandHandler(
        IReservationRepository reservationRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork)
    {
        _reservationRepository = reservationRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ReservationDto>> Handle(ModifyReservationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var reservationId = ReservationId.Create(request.Id);
            var reservation = await _reservationRepository.GetByIdAsync(reservationId);

            if (reservation == null)
            {
                return Result<ReservationDto>.Failure("RESERVATION_NOT_FOUND", "Reservation not found");
            }

            if (reservation.Status != Domain.Enums.ReservationStatus.Active)
            {
                return Result<ReservationDto>.Failure("RESERVATION_NOT_ACTIVE", "Only active reservations can be modified");
            }

            var modifiedBy = UserId.Create(request.ModifiedBy);

            // Handle quantity change
            if (request.NewQuantity.HasValue && request.NewQuantity.Value != reservation.Quantity)
            {
                var inventoryItem = await _inventoryRepository.GetByVariantAndWarehouseAsync(
                    reservation.VariantId, reservation.WarehouseId);

                if (inventoryItem == null)
                {
                    return Result<ReservationDto>.Failure("INVENTORY_NOT_FOUND", "Inventory item not found");
                }

                var quantityDifference = request.NewQuantity.Value - reservation.Quantity;

                if (quantityDifference > 0)
                {
                    // Increasing reservation - check if sufficient stock is available
                    if (inventoryItem.AvailableStock < quantityDifference)
                    {
                        return Result<ReservationDto>.Failure("INSUFFICIENT_STOCK", 
                            $"Insufficient available stock for increase. Available: {inventoryItem.AvailableStock}, Additional needed: {quantityDifference}");
                    }

                    inventoryItem.ReserveStock(quantityDifference, reservation.Id);
                }
                else
                {
                    // Decreasing reservation - release stock
                    inventoryItem.ReleaseReservedStock(Math.Abs(quantityDifference), reservation.Id);
                }

                reservation.ModifyQuantity(request.NewQuantity.Value, modifiedBy);
            }

            // Handle expiry date change
            if (request.NewExpiresAtUtc.HasValue)
            {
                reservation.ExtendExpiry(request.NewExpiresAtUtc.Value, modifiedBy);
            }

            // Handle reason change
            if (!string.IsNullOrEmpty(request.NewReason))
            {
                reservation.UpdateReason(request.NewReason, modifiedBy);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Return the updated reservation as DTO
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