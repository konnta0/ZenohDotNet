using System;

namespace ZenohDotNet.Native
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

        /// <summary>
        /// Gets the last error message from the native Zenoh library.
        /// </summary>
        /// <returns>The error message, or null if no error occurred.</returns>
        public static unsafe string? GetLastNativeError()
        {
            var errorPtr = FFI.NativeMethods.zenoh_last_error();
            if (errorPtr == null)
                return null;
            return System.Runtime.InteropServices.Marshal.PtrToStringUTF8((IntPtr)errorPtr);
        }

        /// <summary>
        /// Creates a ZenohException with the last native error message appended.
        /// </summary>
        /// <param name="message">The base error message.</param>
        /// <returns>A new ZenohException with detailed error information.</returns>
        public static ZenohException FromLastError(string message)
        {
            var nativeError = GetLastNativeError();
            if (string.IsNullOrEmpty(nativeError))
                return new ZenohException(message);
            return new ZenohException($"{message}: {nativeError}");
        }
    }
}
