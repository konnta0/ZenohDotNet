using System;
using System.Runtime.InteropServices;
using System.Text;
using ZenohDotNet.Native.FFI;

namespace ZenohDotNet.Native
{
    /// <summary>
    /// Represents a Zenoh subscriber that receives data on a key expression.
    /// </summary>
    public unsafe class Subscriber : IDisposable
    {
        // IL2CPP (iOS and other AOT targets) cannot marshal delegates that point to
        // instance methods to native code. Use a single static delegate marked with
        // [MonoPInvokeCallback] and dispatch to the instance through the native
        // context pointer (a GCHandle to this instance).
        private static readonly NativeMethods.zenoh_declare_subscriber_callback_delegate _staticNativeCallback = OnSampleReceivedStatic;

        private unsafe void* _handle;
        private readonly Session _session;
        private readonly string _keyExpr;
        private readonly Action<Sample> _callback;
        private GCHandle _selfHandle;
        private bool _disposed;

        /// <summary>
        /// Gets the key expression this subscriber is listening on.
        /// </summary>
        public string KeyExpression => _keyExpr;

        internal unsafe Subscriber(Session session, string keyExpr, Action<Sample> callback)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _keyExpr = keyExpr ?? throw new ArgumentNullException(nameof(keyExpr));
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));

            var keyBytes = Encoding.UTF8.GetBytes(keyExpr + "\0");

            // Root this instance and pass it as the native context so the static
            // callback can dispatch back to it
            _selfHandle = GCHandle.Alloc(this);

            fixed (byte* keyPtr = keyBytes)
            {
                _handle = NativeMethods.zenoh_declare_subscriber(
                    session.Handle,
                    keyPtr,
                    _staticNativeCallback,
                    (void*)GCHandle.ToIntPtr(_selfHandle));
            }

            if (_handle == null)
            {
                _selfHandle.Free();
                throw ZenohException.FromLastError($"Failed to declare subscriber for key expression: {keyExpr}");
            }
        }

#if UNITY_2018_1_OR_NEWER
        [AOT.MonoPInvokeCallback(typeof(NativeMethods.zenoh_declare_subscriber_callback_delegate))]
#endif
        private static unsafe void OnSampleReceivedStatic(SampleData* samplePtr, void* contextPtr)
        {
            if (contextPtr == null)
                return;

            var handle = GCHandle.FromIntPtr((IntPtr)contextPtr);
            if (handle.Target is Subscriber subscriber)
            {
                subscriber.OnSampleReceived(samplePtr);
            }
        }

        private unsafe void OnSampleReceived(SampleData* samplePtr)
        {
            try
            {
                if (samplePtr == null)
                    return;

                string keyExpr = Marshal.PtrToStringUTF8((IntPtr)samplePtr->key_expr) ?? string.Empty;

                int payloadLength = (int)samplePtr->payload_len;
                byte[] payload = new byte[payloadLength];
                if (payloadLength > 0)
                {
                    Marshal.Copy((IntPtr)samplePtr->payload_data, payload, 0, payloadLength);
                }

                var kind = (SampleKind)samplePtr->kind;
                var encoding = (PayloadEncoding)samplePtr->encoding_id;

                Timestamp? timestamp = null;
                if (samplePtr->timestamp_valid)
                {
                    byte[] id = new byte[16];
                    for (int i = 0; i < 16; i++)
                        id[i] = samplePtr->timestamp.id[i];
                    timestamp = new Timestamp(samplePtr->timestamp.time_ntp64, id);
                }

                var sample = new Sample(keyExpr, payload, kind, encoding, timestamp);
                _callback?.Invoke(sample);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception in subscriber callback: {ex}");
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Subscriber));
        }

        ~Subscriber()
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
