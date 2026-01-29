using System;
using System.Runtime.InteropServices;
using Zenoh.Native.FFI;

namespace Zenoh.Native
{
    /// <summary>
    /// Represents a Zenoh session. This is the entry point for all Zenoh operations.
    /// </summary>
    public class Session : IDisposable
    {
        private IntPtr _handle;
        private bool _disposed;

        /// <summary>
        /// Gets the native handle for this session.
        /// </summary>
        internal IntPtr Handle
        {
            get
            {
                ThrowIfDisposed();
                return _handle;
            }
        }

        /// <summary>
        /// Opens a new Zenoh session with default configuration.
        /// </summary>
        public Session() : this(null)
        {
        }

        /// <summary>
        /// Opens a new Zenoh session with the specified JSON configuration.
        /// </summary>
        /// <param name="configJson">JSON configuration string, or null for default configuration.</param>
        /// <exception cref="ZenohException">Thrown when the session cannot be created.</exception>
        public Session(string? configJson)
        {
            IntPtr configPtr = IntPtr.Zero;
            try
            {
                if (!string.IsNullOrEmpty(configJson))
                {
                    configPtr = Marshal.StringToHGlobalAnsi(configJson);
                }

                _handle = NativeMethods.zenoh_open(configPtr);

                if (_handle == IntPtr.Zero)
                {
                    throw new ZenohException("Failed to open Zenoh session");
                }
            }
            finally
            {
                if (configPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(configPtr);
                }
            }
        }

        /// <summary>
        /// Creates a new publisher for the specified key expression.
        /// </summary>
        /// <param name="keyExpr">The key expression to publish on.</param>
        /// <returns>A new Publisher instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when keyExpr is null or empty.</exception>
        /// <exception cref="ZenohException">Thrown when the publisher cannot be created.</exception>
        public Publisher DeclarePublisher(string keyExpr)
        {
            if (string.IsNullOrEmpty(keyExpr))
                throw new ArgumentNullException(nameof(keyExpr));

            ThrowIfDisposed();
            return new Publisher(this, keyExpr);
        }

        /// <summary>
        /// Creates a new subscriber for the specified key expression.
        /// </summary>
        /// <param name="keyExpr">The key expression to subscribe to.</param>
        /// <param name="callback">The callback to invoke when data is received.</param>
        /// <returns>A new Subscriber instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when keyExpr or callback is null.</exception>
        /// <exception cref="ZenohException">Thrown when the subscriber cannot be created.</exception>
        public Subscriber DeclareSubscriber(string keyExpr, Action<Sample> callback)
        {
            if (string.IsNullOrEmpty(keyExpr))
                throw new ArgumentNullException(nameof(keyExpr));
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            ThrowIfDisposed();
            return new Subscriber(this, keyExpr, callback);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Session));
        }

        /// <summary>
        /// Finalizer for Session.
        /// </summary>
        ~Session()
        {
            Dispose(false);
        }

        /// <summary>
        /// Closes the session and releases all associated resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the session.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (_handle != IntPtr.Zero)
                {
                    NativeMethods.zenoh_close(_handle);
                    _handle = IntPtr.Zero;
                }
                _disposed = true;
            }
        }
    }
}
