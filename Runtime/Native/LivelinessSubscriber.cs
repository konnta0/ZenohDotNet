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
    public unsafe class LivelinessSubscriber : IDisposable
    {
        // IL2CPP (iOS and other AOT targets) cannot marshal delegates that point to
        // instance methods to native code. Use a single static delegate marked with
        // [MonoPInvokeCallback] and dispatch to the instance through the native
        // context pointer (a GCHandle to this instance).
        private static readonly NativeMethods.zenoh_liveliness_declare_subscriber_callback_delegate _staticNativeCallback = OnLivelinessChangeStatic;

        private unsafe void* _handle;
        private readonly Session _session;
        private readonly string _keyExpr;
        private readonly Action<string, bool> _callback;
        private GCHandle _selfHandle;
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

            _selfHandle = GCHandle.Alloc(this);

            var keyBytes = Encoding.UTF8.GetBytes(keyExpr + "\0");
            fixed (byte* keyPtr = keyBytes)
            {
                _handle = NativeMethods.zenoh_liveliness_declare_subscriber(
                    session.Handle,
                    keyPtr,
                    _staticNativeCallback,
                    (void*)GCHandle.ToIntPtr(_selfHandle));
            }

            if (_handle == null)
            {
                _selfHandle.Free();
                throw ZenohException.FromLastError("Failed to declare liveliness subscriber for key expression: {keyExpr}");
            }
        }

#if UNITY_2018_1_OR_NEWER
        [AOT.MonoPInvokeCallback(typeof(NativeMethods.zenoh_liveliness_declare_subscriber_callback_delegate))]
#endif
        private static unsafe void OnLivelinessChangeStatic(byte* keyExprPtr, bool isAlive, void* contextPtr)
        {
            if (contextPtr == null)
                return;

            var handle = GCHandle.FromIntPtr((IntPtr)contextPtr);
            if (handle.Target is LivelinessSubscriber subscriber)
            {
                subscriber.OnLivelinessChange(keyExprPtr, isAlive);
            }
        }

        private unsafe void OnLivelinessChange(byte* keyExprPtr, bool isAlive)
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

                if (_selfHandle.IsAllocated)
                {
                    _selfHandle.Free();
                }

                _disposed = true;
            }
        }
    }
}
