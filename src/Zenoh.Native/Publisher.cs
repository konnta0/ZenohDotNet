using System;
using System.Runtime.InteropServices;
using System.Text;
using Zenoh.Native.FFI;

namespace Zenoh.Native
{
    /// <summary>
    /// Represents a Zenoh publisher that can publish data on a key expression.
    /// </summary>
    public class Publisher : IDisposable
    {
        private IntPtr _handle;
        private readonly Session _session;
        private readonly string _keyExpr;
        private bool _disposed;

        /// <summary>
        /// Gets the key expression this publisher is bound to.
        /// </summary>
        public string KeyExpression => _keyExpr;

        internal Publisher(Session session, string keyExpr)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _keyExpr = keyExpr ?? throw new ArgumentNullException(nameof(keyExpr));

            IntPtr keyPtr = IntPtr.Zero;
            try
            {
                keyPtr = Marshal.StringToHGlobalAnsi(keyExpr);
                _handle = NativeMethods.zenoh_declare_publisher(session.Handle, keyPtr);

                if (_handle == IntPtr.Zero)
                {
                    throw new ZenohException($"Failed to declare publisher for key expression: {keyExpr}");
                }
            }
            finally
            {
                if (keyPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(keyPtr);
                }
            }
        }

        /// <summary>
        /// Publishes a byte array.
        /// </summary>
        /// <param name="data">The data to publish.</param>
        /// <exception cref="ArgumentNullException">Thrown when data is null.</exception>
        /// <exception cref="ZenohException">Thrown when the put operation fails.</exception>
        public void Put(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            ThrowIfDisposed();

            IntPtr dataPtr = IntPtr.Zero;
            try
            {
                dataPtr = Marshal.AllocHGlobal(data.Length);
                Marshal.Copy(data, 0, dataPtr, data.Length);

                int result = NativeMethods.zenoh_publisher_put(_handle, dataPtr, (UIntPtr)data.Length);
                if (result != 0)
                {
                    throw new ZenohException($"Failed to put data: error code {result}");
                }
            }
            finally
            {
                if (dataPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(dataPtr);
                }
            }
        }

        /// <summary>
        /// Publishes a string as UTF-8 encoded bytes.
        /// </summary>
        /// <param name="value">The string to publish.</param>
        /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
        /// <exception cref="ZenohException">Thrown when the put operation fails.</exception>
        public void Put(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            byte[] data = Encoding.UTF8.GetBytes(value);
            Put(data);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Publisher));
        }

        /// <summary>
        /// Finalizer for Publisher.
        /// </summary>
        ~Publisher()
        {
            Dispose(false);
        }

        /// <summary>
        /// Undeclares the publisher and releases all associated resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the publisher.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (_handle != IntPtr.Zero)
                {
                    NativeMethods.zenoh_undeclare_publisher(_handle);
                    _handle = IntPtr.Zero;
                }
                _disposed = true;
            }
        }
    }
}
