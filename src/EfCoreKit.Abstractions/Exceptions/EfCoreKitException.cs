namespace EfCoreKit.Abstractions.Exceptions;

/// <summary>
/// Base exception for all exceptions thrown by EfCoreKit.
/// All custom exceptions in EfCoreKit should inherit from this class.
/// </summary>
public abstract class EfCoreException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    protected EfCoreException(string message) : base(message)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreException"/> class with a specified
    /// error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="inner">The exception that is the cause of the current exception.</param>
    protected EfCoreException(string message, Exception inner) : base(message, inner) { }
}