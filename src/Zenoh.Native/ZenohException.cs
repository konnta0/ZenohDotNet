using System;

namespace Zenoh.Native
{
    /// <summary>
    /// Exception thrown when a Zenoh operation fails.
    /// </summary>
    public class ZenohException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the ZenohException class.
        /// </summary>
        public ZenohException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the ZenohException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ZenohException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ZenohException class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ZenohException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
