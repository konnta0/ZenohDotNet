using System;
using System.Runtime.InteropServices;
using System.Text;
using ZenohDotNet.Native.FFI;

namespace ZenohDotNet.Native
{
    /// <summary>
    /// Represents a Zenoh liveliness token.
    /// A liveliness token signals that a resource is alive.
    /// </summary>
    public class LivelinessToken : IDisposable
    {
        private unsafe void* _handle;
        private readonly Session _session;
        private readonly string _keyExpr;
        private bool _disposed;

        /// <summary>
        /// Gets the key expression this token is bound to.
        /// </summary>
        public string KeyExpression => _keyExpr;

        internal unsafe LivelinessToken(Session session, string keyExpr)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _keyExpr = keyExpr ?? throw new ArgumentNullException(nameof(keyExpr));

            var keyBytes = Encoding.UTF8.GetBytes(keyExpr + "\0");
            fixed (byte* keyPtr = keyBytes)
            {
                _handle = NativeMethods.zenoh_liveliness_declare_token(session.Handle, keyPtr);
            }

            if (_handle == null)
            {
                throw ZenohException.FromLastError("Failed to declare liveliness token for key expression: {keyExpr}");
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LivelinessToken));
        }

        ~LivelinessToken()
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
                    NativeMethods.zenoh_liveliness_undeclare_token(_handle);
                    _handle = null;
                }
                _disposed = true;
            }
        }
    }
}
