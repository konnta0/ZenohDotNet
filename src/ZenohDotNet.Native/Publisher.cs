using System;
using System.Runtime.InteropServices;
using System.Text;
using ZenohDotNet.Native.FFI;

namespace ZenohDotNet.Native
{
    /// <summary>
    /// Represents a Zenoh publisher that can publish data on a key expression.
    /// </summary>
    public class Publisher : IDisposable
    {
        private unsafe void* _handle;
        private readonly Session _session;
        private readonly string _keyExpr;
        private bool _disposed;

        /// <summary>
        /// Gets the key expression this publisher is bound to.
        /// </summary>
        public string KeyExpression => _keyExpr;

        internal unsafe Publisher(Session session, string keyExpr, PublisherOptions? options = null)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _keyExpr = keyExpr ?? throw new ArgumentNullException(nameof(keyExpr));

            var keyBytes = Encoding.UTF8.GetBytes(keyExpr + "\0");
            fixed (byte* keyPtr = keyBytes)
            {
                if (options != null)
                {
                    var nativeOpts = new FFI.PublisherOptions
                    {
                        congestion_control = (ZenohCongestionControl)options.CongestionControl,
                        priority = (ZenohPriority)options.Priority,
                        is_express = options.IsExpress
                    };
                    _handle = NativeMethods.zenoh_declare_publisher_with_options(session.Handle, keyPtr, &nativeOpts);
                }
                else
                {
                    _handle = NativeMethods.zenoh_declare_publisher(session.Handle, keyPtr);
                }
            }

            if (_handle == null)
            {
                throw new ZenohException($"Failed to declare publisher for key expression: {keyExpr}");
            }
        }

        /// <summary>
        /// Publishes a byte array.
        /// </summary>
        public unsafe void Put(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            ThrowIfDisposed();

            fixed (byte* dataPtr = data)
            {
                var result = NativeMethods.zenoh_publisher_put(_handle, dataPtr, (nuint)data.Length);
                if (result != ZenohError.Ok)
                {
                    throw new ZenohException($"Failed to put data: error code {result}");
                }
            }
        }

        /// <summary>
        /// Publishes a string as UTF-8 encoded bytes.
        /// </summary>
        public void Put(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            byte[] data = Encoding.UTF8.GetBytes(value);
            Put(data);
        }

        /// <summary>
        /// Publishes data with encoding.
        /// </summary>
        public unsafe void Put(byte[] data, PayloadEncoding encoding)
        {
            ThrowIfDisposed();

            if (data == null)
                throw new ArgumentNullException(nameof(data));

            fixed (byte* dataPtr = data)
            {
                var result = NativeMethods.zenoh_publisher_put_with_encoding(
                    _handle,
                    dataPtr,
                    (nuint)data.Length,
                    (ZenohEncodingId)encoding);

                if (result != ZenohError.Ok)
                {
                    throw new ZenohException($"Failed to put with encoding: error code {result}");
                }
            }
        }

        /// <summary>
        /// Publishes a string with encoding.
        /// </summary>
        public void Put(string value, PayloadEncoding encoding)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            Put(Encoding.UTF8.GetBytes(value), encoding);
        }

        /// <summary>
        /// Sends a delete operation for the key expression.
        /// </summary>
        public unsafe void Delete()
        {
            ThrowIfDisposed();

            var result = NativeMethods.zenoh_publisher_delete(_handle);
            if (result != ZenohError.Ok)
            {
                throw new ZenohException($"Failed to delete: error code {result}");
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Publisher));
        }

        ~Publisher()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual unsafe void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (_handle != null)
                {
                    NativeMethods.zenoh_undeclare_publisher(_handle);
                    _handle = null;
                }
                _disposed = true;
            }
        }
    }
}
