using FluentValidation;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.ValueObjects;
using IMS.Api.Application.Common.DTOs;

namespace IMS.Api.Application.Queries.Reservations;

/// <summary>
/// Query to get a paginated list of reservations
/// </summary>
public class GetReservationsQuery : IQuery<Result<PagedResult<ReservationDto>>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public Guid? VariantId { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? Status { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool IncludeExpired { get; set; } = true;
    public bool IncludeDeleted { get; set; } = false;
}

/// <summary>
/// Validator for GetReservationsQuery
/// </summary>
public class GetReservationsQueryValidator : AbstractValidator<GetReservationsQuery>
{
    public GetReservationsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(100)
            .WithMessage("Page size must be between 1 and 100");

        RuleFor(x => x.Status)
            .Must(BeValidStatus)
            .When(x => !string.IsNullOrEmpty(x.Status))
            .WithMessage("Status must be one of: Active, Expired, Used, Cancelled");

        RuleFor(x => x.ReferenceNumber)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.ReferenceNumber))
            .WithMessage("Reference number must not exceed 100 characters");

        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("ToDate must be greater than or equal to FromDate");
    }

    private static bool BeValidStatus(string status)
    {
        return Enum.TryParse<Domain.Enums.ReservationStatus>(status, true, out _);
    }
}

/// <summary>
/// Handler for GetReservationsQuery
/// </summary>
public class GetReservationsQueryHandler : IQueryHandler<GetReservationsQuery, Result<PagedResult<ReservationDto>>>
{
    private readonly IReservationRepository _reservationRepository;

    public GetReservationsQueryHandler(IReservationRepository reservationRepository)
    {
        _reservationRepository = reservationRepository;
    }

    public async Task<Result<PagedResult<ReservationDto>>> Handle(GetReservationsQuery request, CancellationToken cancellationToken)
    {
        VariantId? variantId = request.VariantId.HasValue ? VariantId.Create(request.VariantId.Value) : null;
        WarehouseId? warehouseId = request.WarehouseId.HasValue ? WarehouseId.Create(request.WarehouseId.Value) : null;

        Domain.Enums.ReservationStatus? status = null;
        if (!string.IsNullOrEmpty(request.Status) && 
            Enum.TryParse<Domain.Enums.ReservationStatus>(request.Status, true, out var parsedStatus))
        {
            status = parsedStatus;
        }

        var pagedReservations = await _reservationRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            variantId,
            warehouseId,
            status,
            request.ReferenceNumber,
            request.FromDate,
            request.ToDate,
            request.IncludeExpired,
            request.IncludeDeleted);

        var reservationDtos = pagedReservations.Items.Select(reservation => new ReservationDto
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
        }).ToList();

        var result = new PagedResult<ReservationDto>(
            reservationDtos,
            pagedReservations.TotalCount,
            pagedReservations.PageNumber,
            pagedReservations.PageSize);

        return Result<PagedResult<ReservationDto>>.Success(result);
    }
}