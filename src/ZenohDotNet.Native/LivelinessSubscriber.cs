using System;
using System.Runtime.InteropServices;
using System.Text;
using ZenohDotNet.Native.FFI;

namespace ZenohDotNet.Native
{
    /// <summary>
    /// Represents a Zenoh liveliness subscriber.
    /// Receives notifications when liveliness tokens are created or dropped.
    /// </summary>
    public class LivelinessSubscriber : IDisposable
    {
        private unsafe void* _handle;
        private readonly Session _session;
        private readonly string _keyExpr;
        private readonly Action<string, bool> _callback;
        private GCHandle _callbackHandle;
        private NativeMethods.zenoh_liveliness_declare_subscriber_callback_delegate _nativeCallback;
        private bool _disposed;

        /// <summary>
        /// Gets the key expression this subscriber is bound to.
        /// </summary>
        public string KeyExpression => _keyExpr;

        internal unsafe LivelinessSubscriber(Session session, string keyExpr, Action<string, bool> callback)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _keyExpr = keyExpr ?? throw new ArgumentNullException(nameof(keyExpr));
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));

            _nativeCallback = OnLivelinessChange;
            _callbackHandle = GCHandle.Alloc(_nativeCallback);

            var keyBytes = Encoding.UTF8.GetBytes(keyExpr + "\0");
            fixed (byte* keyPtr = keyBytes)
            {
                _handle = NativeMethods.zenoh_liveliness_declare_subscriber(session.Handle, keyPtr, _nativeCallback, null);
            }

            if (_handle == null)
            {
                _callbackHandle.Free();
                throw new ZenohException($"Failed to declare liveliness subscriber for key expression: {keyExpr}");
            }
        }

        private unsafe void OnLivelinessChange(byte* keyExprPtr, bool isAlive, void* context)
        {
            if (keyExprPtr == null)
                return;

            try
            {
                string keyExpr = Marshal.PtrToStringUTF8((IntPtr)keyExprPtr) ?? string.Empty;
                _callback(keyExpr, isAlive);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception in liveliness callback: {ex}");
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LivelinessSubscriber));
        }

        ~LivelinessSubscriber()
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
                    NativeMethods.zenoh_undeclare_subscriber(_handle);
                    _handle = null;
                }

                if (_callbackHandle.IsAllocated)
                {
                    _callbackHandle.Free();
                }

                _disposed = true;
            }
        }
    }
}
