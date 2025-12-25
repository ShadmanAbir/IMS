using IMS.Api.Domain.Common;
using IMS.Api.Domain.Enums;
using IMS.Api.Domain.ValueObjects;
using System.Text.Json;

namespace IMS.Api.Domain.Entities;

/// <summary>
/// Audit log entity representing an immutable record of system actions and changes.
/// Provides complete audit trail for compliance and investigation purposes.
/// </summary>
public class AuditLog : Entity<AuditLogId>
{
    /// <summary>
    /// Gets the type of action that was performed
    /// </summary>
    public AuditAction Action { get; private set; }

    /// <summary>
    /// Gets the name of the entity type that was affected
    /// </summary>
    public string EntityType { get; private set; }

    /// <summary>
    /// Gets the ID of the entity that was affected (if applicable)
    /// </summary>
    public string EntityId { get; private set; }

    /// <summary>
    /// Gets the user who performed the action
    /// </summary>
    public UserId ActorId { get; private set; }

    /// <summary>
    /// Gets the tenant context for this audit entry
    /// </summary>
    public TenantId TenantId { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the action was performed
    /// </summary>
    public DateTime TimestampUtc { get; private set; }

    /// <summary>
    /// Gets a description of the action performed
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Gets the state of the entity before the change (JSON)
    /// </summary>
    public string OldValues { get; private set; }

    /// <summary>
    /// Gets the state of the entity after the change (JSON)
    /// </summary>
    public string NewValues { get; private set; }

    /// <summary>
    /// Gets contextual information about the audit entry
    /// </summary>
    public AuditContext Context { get; private set; }

    /// <summary>
    /// Gets the warehouse ID if the action was warehouse-specific
    /// </summary>
    public WarehouseId? WarehouseId { get; private set; }

    /// <summary>
    /// Gets the variant ID if the action was variant-specific
    /// </summary>
    public VariantId? VariantId { get; private set; }

    /// <summary>
    /// Gets the reason or justification for the action
    /// </summary>
    public string Reason { get; private set; }

    // Private constructor for domain logic
    private AuditLog(
        AuditLogId id,
        AuditAction action,
        string entityType,
        string entityId,
        UserId actorId,
        TenantId tenantId,
        string description,
        string oldValues,
        string newValues,
        AuditContext context,
        WarehouseId? warehouseId,
        VariantId? variantId,
        string reason) : base(id)
    {
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        ActorId = actorId;
        TenantId = tenantId;
        TimestampUtc = DateTime.UtcNow;
        Description = description;
        OldValues = oldValues;
        NewValues = newValues;
        Context = context;
        WarehouseId = warehouseId;
        VariantId = variantId;
        Reason = reason;
    }

    /// <summary>
    /// Creates a new audit log entry for entity changes
    /// </summary>
    /// <param name="action">The action performed</param>
    /// <param name="entityType">The type of entity affected</param>
    /// <param name="entityId">The ID of the entity affected</param>
    /// <param name="actorId">The user who performed the action</param>
    /// <param name="tenantId">The tenant context</param>
    /// <param name="description">Description of the action</param>
    /// <param name="oldValues">Previous state of the entity</param>
    /// <param name="newValues">New state of the entity</param>
    /// <param name="context">Contextual information</param>
    /// <param name="warehouseId">Warehouse ID if applicable</param>
    /// <param name="variantId">Variant ID if applicable</param>
    /// <param name="reason">Reason for the action</param>
    /// <returns>A new AuditLog instance</returns>
    public static AuditLog Create(
        AuditAction action,
        string entityType,
        string entityId,
        UserId actorId,
        TenantId tenantId,
        string description,
        object oldValues = null,
        object newValues = null,
        AuditContext context = null,
        WarehouseId? warehouseId = null,
        VariantId? variantId = null,
        string reason = null)
    {
        if (actorId == null)
            throw new ArgumentNullException(nameof(actorId));

        if (tenantId == null)
            throw new ArgumentNullException(nameof(tenantId));

        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("Entity type cannot be null or empty", nameof(entityType));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be null or empty", nameof(description));

        var oldValuesJson = oldValues != null ? JsonSerializer.Serialize(oldValues) : null;
        var newValuesJson = newValues != null ? JsonSerializer.Serialize(newValues) : null;

        return new AuditLog(
            AuditLogId.CreateNew(),
            action,
            entityType.Trim(),
            entityId?.Trim(),
            actorId,
            tenantId,
            description.Trim(),
            oldValuesJson,
            newValuesJson,
            context ?? AuditContext.Empty(),
            warehouseId,
            variantId,
            reason?.Trim());
    }

    /// <summary>
    /// Creates a stock movement audit log entry
    /// </summary>
    /// <param name="stockMovement">The stock movement that was recorded</param>
    /// <param name="actorId">The user who performed the movement</param>
    /// <param name="tenantId">The tenant context</param>
    /// <param name="warehouseId">The warehouse where the movement occurred</param>
    /// <param name="variantId">The variant that was moved</param>
    /// <param name="context">Contextual information</param>
    /// <returns>A new AuditLog instance for the stock movement</returns>
    public static AuditLog CreateForStockMovement(
        StockMovement stockMovement,
        UserId actorId,
        TenantId tenantId,
        WarehouseId warehouseId,
        VariantId variantId,
        AuditContext context = null)
    {
        if (stockMovement == null)
            throw new ArgumentNullException(nameof(stockMovement));

        var description = $"Stock movement: {stockMovement.Type} of {stockMovement.GetAbsoluteQuantity()} units";
        
        var movementData = new
        {
            MovementType = stockMovement.Type.ToString(),
            Quantity = stockMovement.Quantity,
            RunningBalance = stockMovement.RunningBalance,
            ReferenceNumber = stockMovement.ReferenceNumber,
            EntryType = stockMovement.EntryType.ToString(),
            PairedMovementId = stockMovement.PairedMovementId?.ToString()
        };

        return Create(
            AuditAction.StockMovement,
            nameof(StockMovement),
            stockMovement.Id.ToString(),
            actorId,
            tenantId,
            description,
            null,
            movementData,
            context,
            warehouseId,
            variantId,
            stockMovement.Reason);
    }

    /// <summary>
    /// Creates a user action audit log entry
    /// </summary>
    /// <param name="action">The action performed</param>
    /// <param name="actorId">The user who performed the action</param>
    /// <param name="tenantId">The tenant context</param>
    /// <param name="description">Description of the action</param>
    /// <param name="context">Contextual information</param>
    /// <returns>A new AuditLog instance for the user action</returns>
    public static AuditLog CreateForUserAction(
        AuditAction action,
        UserId actorId,
        TenantId tenantId,
        string description,
        AuditContext context = null)
    {
        return Create(
            action,
            "UserAction",
            null,
            actorId,
            tenantId,
            description,
            null,
            null,
            context);
    }

    /// <summary>
    /// Gets the old values as a dictionary
    /// </summary>
    /// <returns>Dictionary containing old values</returns>
    public Dictionary<string, object> GetOldValuesAsDictionary()
    {
        if (string.IsNullOrEmpty(OldValues))
            return new Dictionary<string, object>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(OldValues) 
                   ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Gets the new values as a dictionary
    /// </summary>
    /// <returns>Dictionary containing new values</returns>
    public Dictionary<string, object> GetNewValuesAsDictionary()
    {
        if (string.IsNullOrEmpty(NewValues))
            return new Dictionary<string, object>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(NewValues) 
                   ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Checks if this audit entry is related to a specific warehouse
    /// </summary>
    /// <param name="warehouseId">The warehouse ID to check</param>
    /// <returns>True if the audit entry is related to the warehouse</returns>
    public bool IsRelatedToWarehouse(WarehouseId warehouseId)
    {
        return WarehouseId != null && WarehouseId.Equals(warehouseId);
    }

    /// <summary>
    /// Checks if this audit entry is related to a specific variant
    /// </summary>
    /// <param name="variantId">The variant ID to check</param>
    /// <returns>True if the audit entry is related to the variant</returns>
    public bool IsRelatedToVariant(VariantId variantId)
    {
        return VariantId != null && VariantId.Equals(variantId);
    }

    /// <summary>
    /// Checks if this audit entry was performed by a specific user
    /// </summary>
    /// <param name="userId">The user ID to check</param>
    /// <returns>True if the audit entry was performed by the user</returns>
    public bool IsPerformedBy(UserId userId)
    {
        return ActorId.Equals(userId);
    }
}