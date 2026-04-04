namespace EfCoreKit.Exceptions;

/// <summary>
/// Thrown when there is a mismatch between the tenant in the current context and the tenant
/// associated with the entity being accessed or modified.
/// </summary>
public sealed class TenantMismatchException : EfCoreException
{
    /// <summary>
    /// Gets the tenant identifier that was expected based on the current context.
    /// </summary>
    public string? ExpectedTenant { get; }

    /// <summary>
    /// Gets the tenant identifier that was found on the entity.
    /// </summary>
    public string? ActualTenant { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantMismatchException"/> class.
    /// </summary>
    /// <param name="expected">The expected tenant identifier from the current context.</param>
    /// <param name="actual">The actual tenant identifier found on the entity.</param>
    public TenantMismatchException(string? expected, string? actual)
        : base($"Tenant mismatch. Expected '{expected}', got '{actual}'.")
    {
        ExpectedTenant = expected;
        ActualTenant = actual;
    }
}