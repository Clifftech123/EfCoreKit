namespace EfCore.Extensions.Abstractions.Exceptions;

/// <summary>
/// Thrown when a concurrency conflict occurs during an update or delete operation in Entity Framework Core.
/// </summary>
public sealed class ConcurrencyConflictException : EfCoreException
{
    /// <summary>
    /// Gets the type name of the entity that caused the concurrency conflict.
    /// </summary>
    public string EntityType { get; }

    /// <summary>
    /// Gets the identifier of the entity that caused the concurrency conflict.
    /// </summary>
    public object? EntityId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyConflictException"/> class.
    /// </summary>
    /// <param name="entityType">The type name of the entity that caused the conflict.</param>
    /// <param name="entityId">The identifier of the entity that caused the conflict.</param>
    public ConcurrencyConflictException(string entityType, object? entityId)
        : base($"Concurrency conflict on entity '{entityType}' with ID '{entityId}'.")
    {
        EntityType = entityType;
        EntityId = entityId;
    }
}