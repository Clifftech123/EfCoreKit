namespace EfCoreKit.Exceptions;

/// <summary>
/// Thrown when a <see cref="EfCoreKit.Models.FilterDescriptor"/> is invalid —
/// e.g. a missing field name or an unsupported operator.
/// </summary>
public sealed class InvalidFilterException : EfCoreException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidFilterException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public InvalidFilterException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidFilterException"/> class
    /// with an inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="inner">The exception that caused this exception.</param>
    public InvalidFilterException(string message, Exception inner) : base(message, inner) { }
}
