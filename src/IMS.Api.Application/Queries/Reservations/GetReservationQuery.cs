using FluentValidation;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.ValueObjects;
using IMS.Api.Application.Common.DTOs;

namespace IMS.Api.Application.Queries.Reservations;

/// <summary>
/// Query to get a single reservation by ID
/// </summary>
public class GetReservationQuery : IQuery<Result<ReservationDto>>
{
    public Guid Id { get; set; }
}

/// <summary>
/// Validator for GetReservationQuery
/// </summary>
public class GetReservationQueryValidator : AbstractValidator<GetReservationQuery>
{
    public GetReservationQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Reservation ID is required");
    }
}

/// <summary>
/// Handler for GetReservationQuery
/// </summary>
public class GetReservationQueryHandler : IQueryHandler<GetReservationQuery, Result<ReservationDto>>
{
    private readonly IReservationRepository _reservationRepository;

    public GetReservationQueryHandler(IReservationRepository reservationRepository)
    {
        _reservationRepository = reservationRepository;
    }

    public async Task<Result<ReservationDto>> Handle(GetReservationQuery request, CancellationToken cancellationToken)
    {
        var reservationId = ReservationId.Create(request.Id);
        var reservation = await _reservationRepository.GetByIdAsync(reservationId);

        if (reservation == null)
        {
            return Result<ReservationDto>.Failure("RESERVATION_NOT_FOUND", "Reservation not found");
        }

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
            UsedAtUtc = reservation.UsedAtUtc,
            UsedBy = reservation.UsedBy?.Value,
            CancelledAtUtc = reservation.CancelledAtUtc,
            CancelledBy = reservation.CancelledBy?.Value,
            TenantId = reservation.TenantId.Value,
            IsDeleted = reservation.IsDeleted,
            DeletedAtUtc = reservation.DeletedAtUtc,
            DeletedBy = reservation.DeletedBy?.Value
        };

        return Result<ReservationDto>.Success(reservationDto);
    }
}