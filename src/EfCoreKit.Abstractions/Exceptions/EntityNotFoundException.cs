namespace EfCoreKit.Abstractions.Exceptions
{
    /// <summary>
    /// Thrown when a requested entity cannot be found in the data store.
    /// </summary>
    public sealed class EntityNotFoundException : EfCoreKitException
    {
        /// <summary>
        /// Gets the type name of the entity that was not found.
        /// </summary>
        public string EntityType { get; }

        /// <summary>
        /// Gets the identifier of the entity that was not found.
        /// </summary>
        public object? EntityId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class.
        /// </summary>
        /// <param name="entityType">The type name of the entity that was not found.</param>
        /// <param name="entityId">The identifier of the entity that was not found.</param>
        public EntityNotFoundException(string entityType, object? entityId)
            : base($"Entity '{entityType}' with ID '{entityId}' was not found.")
        {
            EntityType = entityType;
            EntityId = entityId;
        }
    }
}