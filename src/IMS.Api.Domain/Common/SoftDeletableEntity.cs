using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Domain.Common;

/// <summary>
/// Base class for entities that support soft delete functionality
/// </summary>
/// <typeparam name="TId">Entity identifier type</typeparam>
public abstract class SoftDeletableEntity<TId> : Entity<TId>, ISoftDeletable
    where TId : notnull
{
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }
    public UserId? DeletedBy { get; private set; }

    protected SoftDeletableEntity(TId id) : base(id)
    {
        IsDeleted = false;
        DeletedAtUtc = null;
        DeletedBy = null;
    }

    public virtual void SoftDelete(UserId deletedBy)
    {
        if (deletedBy == null)
            throw new ArgumentNullException(nameof(deletedBy));

        if (IsDeleted)
            throw new InvalidOperationException("Entity is already soft deleted");

        IsDeleted = true;
        DeletedAtUtc = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    public virtual void Restore()
    {
        if (!IsDeleted)
            throw new InvalidOperationException("Entity is not soft deleted");

        IsDeleted = false;
        DeletedAtUtc = null;
        DeletedBy = null;
    }
}