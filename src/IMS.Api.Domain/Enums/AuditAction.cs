namespace IMS.Api.Domain.Enums;

/// <summary>
/// Enumeration of audit actions that can be performed in the system
/// </summary>
public enum AuditAction
{
    /// <summary>
    /// Entity was created
    /// </summary>
    Create,

    /// <summary>
    /// Entity was updated/modified
    /// </summary>
    Update,

    /// <summary>
    /// Entity was deleted (soft delete)
    /// </summary>
    Delete,

    /// <summary>
    /// Stock movement was recorded
    /// </summary>
    StockMovement,

    /// <summary>
    /// Reservation was created
    /// </summary>
    ReservationCreate,

    /// <summary>
    /// Reservation was modified
    /// </summary>
    ReservationModify,

    /// <summary>
    /// Reservation was cancelled
    /// </summary>
    ReservationCancel,

    /// <summary>
    /// Reservation expired automatically
    /// </summary>
    ReservationExpire,

    /// <summary>
    /// User logged in
    /// </summary>
    Login,

    /// <summary>
    /// User logged out
    /// </summary>
    Logout,

    /// <summary>
    /// Permission was granted
    /// </summary>
    PermissionGrant,

    /// <summary>
    /// Permission was revoked
    /// </summary>
    PermissionRevoke,

    /// <summary>
    /// Configuration was changed
    /// </summary>
    ConfigurationChange,

    /// <summary>
    /// Data export was performed
    /// </summary>
    DataExport,

    /// <summary>
    /// Data import was performed
    /// </summary>
    DataImport
}