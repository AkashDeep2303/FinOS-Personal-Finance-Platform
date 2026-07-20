namespace FinOS.Common.Interfaces;

/// <summary>
/// Marks an entity as auditable with standard tracking fields
/// for creation and last modification metadata.
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// UTC timestamp when the entity was created.
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// Identifier (user ID or service name) of the entity creator.
    /// </summary>
    string CreatedBy { get; set; }

    /// <summary>
    /// UTC timestamp when the entity was last modified.
    /// </summary>
    DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Identifier (user ID or service name) of the last modifier.
    /// </summary>
    string? UpdatedBy { get; set; }
}
