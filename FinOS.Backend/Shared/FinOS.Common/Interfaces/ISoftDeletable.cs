namespace FinOS.Common.Interfaces;

/// <summary>
/// Marks an entity as soft-deletable, meaning deletion sets a flag
/// rather than physically removing the row from the database.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Indicates whether the entity has been soft-deleted.
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// UTC timestamp when the entity was soft-deleted.
    /// </summary>
    DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Identifier (user ID or service name) that performed the soft delete.
    /// </summary>
    string? DeletedBy { get; set; }
}
