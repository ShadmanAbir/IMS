using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Domain.Entities;
using IMS.Api.Domain.Enums;
using IMS.Api.Domain.ValueObjects;
using IMS.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace IMS.Api.Infrastructure.Services;

/// <summary>
/// Service implementation for audit logging functionality
/// </summary>
public class AuditService : IAuditService
{
    private readonly IAuditRepository _auditRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        IAuditRepository auditRepository,
        ITenantContext tenantContext,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditService> logger)
    {
        _auditRepository = auditRepository;
        _tenantContext = tenantContext;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogAsync(
        AuditAction action,
        string entityType,
        string entityId,
        string description,
        object oldValues = null,
        object newValues = null,
        WarehouseId? warehouseId = null,
        VariantId? variantId = null,
        string reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorId = GetCurrentUserId();
            var tenantId = GetCurrentTenantId();
            var context = CreateAuditContext();

            var auditLog = AuditLog.Create(
                action,
                entityType,
                entityId,
                actorId,
                tenantId,
                description,
                oldValues,
                newValues,
                context,
                warehouseId,
                variantId,
                reason);

            await _auditRepository.AddAsync(auditLog, cancellationToken);

            _logger.LogInformation(
                "Audit log created: Action={Action}, EntityType={EntityType}, EntityId={EntityId}, Actor={ActorId}",
                action, entityType, entityId, actorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to create audit log: Action={Action}, EntityType={EntityType}, EntityId={EntityId}",
                action, entityType, entityId);
            
            // Don't throw - audit logging should not break the main operation
        }
    }

    public async Task LogStockMovementAsync(
        StockMovement stockMovement,
        WarehouseId warehouseId,
        VariantId variantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorId = GetCurrentUserId();
            var tenantId = GetCurrentTenantId();
            var context = CreateAuditContext();

            var auditLog = AuditLog.CreateForStockMovement(
                stockMovement,
                actorId,
                tenantId,
                warehouseId,
                variantId,
                context);

            await _auditRepository.AddAsync(auditLog, cancellationToken);

            _logger.LogInformation(
                "Stock movement audit log created: MovementId={MovementId}, Type={MovementType}, Quantity={Quantity}, Warehouse={WarehouseId}, Variant={VariantId}",
                stockMovement.Id, stockMovement.Type, stockMovement.Quantity, warehouseId, variantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to create stock movement audit log: MovementId={MovementId}",
                stockMovement.Id);
        }
    }

    public async Task LogUserActionAsync(
        AuditAction action,
        string description,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorId = GetCurrentUserId();
            var tenantId = GetCurrentTenantId();
            var context = CreateAuditContext();

            var auditLog = AuditLog.CreateForUserAction(
                action,
                actorId,
                tenantId,
                description,
                context);

            await _auditRepository.AddAsync(auditLog, cancellationToken);

            _logger.LogInformation(
                "User action audit log created: Action={Action}, Description={Description}, Actor={ActorId}",
                action, description, actorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to create user action audit log: Action={Action}, Description={Description}",
                action, description);
        }
    }

    public async Task LogReservationActionAsync(
        AuditAction action,
        ReservationId reservationId,
        VariantId variantId,
        WarehouseId warehouseId,
        string description,
        object oldValues = null,
        object newValues = null,
        string reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actorId = GetCurrentUserId();
            var tenantId = GetCurrentTenantId();
            var context = CreateAuditContext();

            var auditLog = AuditLog.Create(
                action,
                nameof(Reservation),
                reservationId.ToString(),
                actorId,
                tenantId,
                description,
                oldValues,
                newValues,
                context,
                warehouseId,
                variantId,
                reason);

            await _auditRepository.AddAsync(auditLog, cancellationToken);

            _logger.LogInformation(
                "Reservation audit log created: Action={Action}, ReservationId={ReservationId}, Variant={VariantId}, Warehouse={WarehouseId}",
                action, reservationId, variantId, warehouseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to create reservation audit log: Action={Action}, ReservationId={ReservationId}",
                action, reservationId);
        }
    }

    private UserId GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return UserId.Create(userId);
            }
        }

        // Return system user ID if no authenticated user
        return UserId.Create(Guid.Empty);
    }

    private TenantId GetCurrentTenantId()
    {
        return _tenantContext.CurrentTenantId ?? TenantId.Create(Guid.Empty);
    }

    private AuditContext CreateAuditContext()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return AuditContext.Empty();
        }

        var ipAddress = GetClientIpAddress(httpContext);
        var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault();
        var correlationId = httpContext.TraceIdentifier;

        var additionalData = new Dictionary<string, object>();

        // Add request method and path
        if (httpContext.Request != null)
        {
            additionalData["RequestMethod"] = httpContext.Request.Method;
            additionalData["RequestPath"] = httpContext.Request.Path.Value;
        }

        // Add any custom headers that might be useful for auditing
        if (httpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            additionalData["ForwardedFor"] = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        }

        return AuditContext.Create(ipAddress, userAgent, correlationId, additionalData);
    }

    private string GetClientIpAddress(HttpContext httpContext)
    {
        // Check for forwarded IP first (in case of proxy/load balancer)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // Take the first IP if there are multiple
            return forwardedFor.Split(',')[0].Trim();
        }

        // Check for real IP header
        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fall back to connection remote IP
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}