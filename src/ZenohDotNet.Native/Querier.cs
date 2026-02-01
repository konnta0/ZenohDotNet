using System;
using System.Runtime.InteropServices;
using ZenohDotNet.Native.FFI;

namespace ZenohDotNet.Native
{
    /// <summary>
    /// Represents a Zenoh querier for repeated queries on the same key expression.
    /// </summary>
    public class Querier : IDisposable
    {
        private unsafe void* _handle;
        private bool _disposed;
        private readonly string _keyExpr;

        /// <summary>
        /// Gets the key expression this querier is bound to.
        /// </summary>
        public string KeyExpression => _keyExpr;

        internal unsafe Querier(Session session, string keyExpr)
        {
            _keyExpr = keyExpr ?? throw new ArgumentNullException(nameof(keyExpr));

            fixed (byte* keyPtr = System.Text.Encoding.UTF8.GetBytes(keyExpr + '\0'))
            {
                _handle = NativeMethods.zenoh_declare_querier(session.Handle, keyPtr);
            }

            if (_handle == null)
            {
                throw ZenohException.FromLastError("Failed to declare querier for key expression: {keyExpr}");
            }
        }

        /// <summary>
        /// Performs a get query and invokes the callback for each reply.
        /// </summary>
        /// <param name="callback">Callback invoked for each reply sample.</param>
        public unsafe void Get(Action<Sample> callback)
        {
            ThrowIfDisposed();

            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            NativeMethods.zenoh_querier_get_callback_delegate nativeCallback = (samplePtr, ctx) =>
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
                callback(sample);
            };

            var result = NativeMethods.zenoh_querier_get(_handle, nativeCallback, null);
            GC.KeepAlive(nativeCallback);

            if (result != ZenohError.Ok)
            {
                throw ZenohException.FromLastError("Querier get failed with error: {result}");
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Querier));
        }

        /// <summary>
        /// Disposes the querier and releases all resources.
        /// </summary>
        public unsafe void Dispose()
        {
            if (!_disposed)
            {
                if (_handle != null)
                {
                    NativeMethods.zenoh_undeclare_querier(_handle);
                    _handle = null;
                }
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        ~Querier()
        {
            Dispose();
        }
    }
}
