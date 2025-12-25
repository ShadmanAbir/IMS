using FluentValidation;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Application.Commands.Reservations;

/// <summary>
/// Command to cancel a reservation
/// </summary>
public class CancelReservationCommand : ICommand<Result>
{
    public Guid Id { get; set; }
    public string CancellationReason { get; set; } = string.Empty;
    public Guid CancelledBy { get; set; }
}

/// <summary>
/// Validator for CancelReservationCommand
/// </summary>
public class CancelReservationCommandValidator : AbstractValidator<CancelReservationCommand>
{
    public CancelReservationCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Reservation ID is required");

        RuleFor(x => x.CancellationReason)
            .NotEmpty()
            .MaximumLength(255)
            .WithMessage("Cancellation reason is required and must not exceed 255 characters");

        RuleFor(x => x.CancelledBy)
            .NotEmpty()
            .WithMessage("CancelledBy user ID is required");
    }
}

/// <summary>
/// Handler for CancelReservationCommand
/// </summary>
public class CancelReservationCommandHandler : ICommandHandler<CancelReservationCommand, Result>
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelReservationCommandHandler(
        IReservationRepository reservationRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork)
    {
        _reservationRepository = reservationRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(CancelReservationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var reservationId = ReservationId.Create(request.Id);
            var reservation = await _reservationRepository.GetByIdAsync(reservationId);

            if (reservation == null)
            {
                return Result.Failure("RESERVATION_NOT_FOUND", "Reservation not found");
            }

            if (reservation.Status != Domain.Enums.ReservationStatus.Active)
            {
                return Result.Failure("RESERVATION_NOT_ACTIVE", "Only active reservations can be cancelled");
            }

            var cancelledBy = UserId.Create(request.CancelledBy);

            // Release the reserved stock
            var inventoryItem = await _inventoryRepository.GetByVariantAndWarehouseAsync(
                reservation.VariantId, reservation.WarehouseId);

            if (inventoryItem != null)
            {
                inventoryItem.ReleaseReservedStock(reservation.Quantity, reservation.Id);
            }

            // Cancel the reservation
            reservation.Cancel(cancelledBy, request.CancellationReason);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            return Result.Failure("INVALID_INPUT", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure("BUSINESS_RULE_VIOLATION", ex.Message);
        }
    }
}