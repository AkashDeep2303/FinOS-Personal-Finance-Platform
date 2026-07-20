namespace FinOS.Common.Exceptions;

/// <summary>
/// Thrown when a requested resource cannot be found.
/// Maps to HTTP 404 Not Found.
/// </summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string entityName, object key)
        : base($"Entity '{entityName}' with key '{key}' was not found.", "NOT_FOUND")
    {
        EntityName = entityName;
        Key = key;
    }

    public NotFoundException(string message)
        : base(message, "NOT_FOUND")
    {
        EntityName = string.Empty;
        Key = string.Empty;
    }

    /// <summary>
    /// Name of the entity type that was not found.
    /// </summary>
    public string EntityName { get; }

    /// <summary>
    /// Key value that was used to look up the entity.
    /// </summary>
    public object Key { get; }
}
