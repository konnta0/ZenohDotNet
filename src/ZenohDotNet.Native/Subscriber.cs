using System;
using System.Runtime.InteropServices;
using System.Text;
using ZenohDotNet.Native.FFI;

namespace ZenohDotNet.Native
{
    /// <summary>
    /// Represents a Zenoh subscriber that receives data on a key expression.
    /// </summary>
    public class Subscriber : IDisposable
    {
        private unsafe void* _handle;
        private readonly Session _session;
        private readonly string _keyExpr;
        private readonly Action<Sample> _callback;
        private GCHandle _callbackHandle;
        private NativeMethods.zenoh_declare_subscriber_callback_delegate? _nativeCallback;
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

            // Create native callback and prevent GC
            _nativeCallback = OnSampleReceived;
            _callbackHandle = GCHandle.Alloc(_nativeCallback);

            fixed (byte* keyPtr = keyBytes)
            {
                _handle = NativeMethods.zenoh_declare_subscriber(
                    session.Handle,
                    keyPtr,
                    _nativeCallback,
                    null);
            }

            if (_handle == null)
            {
                _callbackHandle.Free();
                throw new ZenohException($"Failed to declare subscriber for key expression: {keyExpr}");
            }
        }

        private unsafe void OnSampleReceived(SampleData* samplePtr, void* contextPtr)
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

                if (_callbackHandle.IsAllocated)
                {
                    _callbackHandle.Free();
                }

                _disposed = true;
            }
        }
    }
}
