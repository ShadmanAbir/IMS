using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Domain.Common;

/// <summary>
/// Interface for entities that support soft delete functionality
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Indicates whether the entity is soft deleted
    /// </summary>
    bool IsDeleted { get; }
    
    /// <summary>
    /// UTC timestamp when the entity was soft deleted
    /// </summary>
    DateTime? DeletedAtUtc { get; }
    
    /// <summary>
    /// User who performed the soft delete operation
    /// </summary>
    UserId? DeletedBy { get; }
    
    /// <summary>
    /// Marks the entity as soft deleted
    /// </summary>
    /// <param name="deletedBy">User performing the delete operation</param>
    void SoftDelete(UserId deletedBy);
    
    /// <summary>
    /// Restores a soft deleted entity
    /// </summary>
    void Restore();
}