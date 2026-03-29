

namespace EfCoreKit.Abstractions.Exceptions
{

    /// <summary>
    ///  Main exception for all exceptions thrown by EfCoreKit. 
    ///  All exceptions thrown by EfCoreKit should inherit from this class.
    /// </summary>
    public abstract class EfCoreKitException : Exception
    {
        /// <summary>
        ///  Initializes a new instance of the EfCoreKitException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error. This message is used to provide additional context about the
        /// exception.</param>
        protected EfCoreKitException(string message) : base(message)
        { }


        /// <summary>
        /// initializes a new instance of the EfCoreKitException class with a specified
        /// error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        protected EfCoreKitException(string message, Exception inner) : base(message, inner) { }
    }
}