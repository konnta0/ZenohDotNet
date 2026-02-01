using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using ZenohDotNet.Native.FFI;

namespace ZenohDotNet.Native
{
    /// <summary>
    /// Represents a Zenoh session. This is the entry point for all Zenoh operations.
    /// </summary>
    public class Session : IDisposable
    {
        private unsafe void* _handle;
        private bool _disposed;

        /// <summary>
        /// Gets the native handle for this session.
        /// </summary>
        internal unsafe void* Handle
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
        public Session() : this((string?)null)
        {
        }

        /// <summary>
        /// Opens a new Zenoh session with the specified configuration.
        /// </summary>
        /// <param name="config">The session configuration.</param>
        /// <exception cref="ZenohException">Thrown when the session cannot be created.</exception>
        public Session(SessionConfig config) : this(config?.ToJson())
        {
        }

        /// <summary>
        /// Opens a new Zenoh session with the specified JSON configuration.
        /// </summary>
        /// <param name="configJson">JSON configuration string, or null for default configuration.</param>
        /// <exception cref="ZenohException">Thrown when the session cannot be created.</exception>
        public unsafe Session(string? configJson)
        {
            byte* configPtr = null;
            byte[]? configBytes = null;
            
            try
            {
                if (!string.IsNullOrEmpty(configJson))
                {
                    configBytes = Encoding.UTF8.GetBytes(configJson + "\0");
                    fixed (byte* p = configBytes)
                    {
                        _handle = NativeMethods.zenoh_open(p);
                    }
                }
                else
                {
                    _handle = NativeMethods.zenoh_open(null);
                }

                if (_handle == null)
                {
                    throw ZenohException.FromLastError("Failed to open Zenoh session");
                }
            }
            finally
            {
            }
        }

        /// <summary>
        /// Creates a new publisher for the specified key expression.
        /// </summary>
        public Publisher DeclarePublisher(string keyExpr)
        {
            if (string.IsNullOrEmpty(keyExpr))
                throw new ArgumentNullException(nameof(keyExpr));

            ThrowIfDisposed();
            return new Publisher(this, keyExpr);
        }

        /// <summary>
        /// Creates a new publisher for the specified key expression with options.
        /// </summary>
        public Publisher DeclarePublisher(string keyExpr, PublisherOptions options)
        {
            if (string.IsNullOrEmpty(keyExpr))
                throw new ArgumentNullException(nameof(keyExpr));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            ThrowIfDisposed();
            return new Publisher(this, keyExpr, options);
        }

        /// <summary>
        /// Puts data directly on the session (without declaring a publisher).
        /// </summary>
        public unsafe void Put(string keyExpr, byte[] data)
        {
            if (string.IsNullOrEmpty(keyExpr))
                throw new ArgumentNullException(nameof(keyExpr));
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            ThrowIfDisposed();

            var keyBytes = Encoding.UTF8.GetBytes(keyExpr + "\0");

            fixed (byte* keyPtr = keyBytes)
            fixed (byte* dataPtr = data)
            {
                var result = NativeMethods.zenoh_put(_handle, keyPtr, dataPtr, (nuint)data.Length);
                if (result != ZenohError.Ok)
                {
                    throw ZenohException.FromLastError("Failed to put data: error code {result}");
                }
            }
        }

        /// <summary>
        /// Puts a string directly on the session.
        /// </summary>
        public void Put(string keyExpr, string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            Put(keyExpr, Encoding.UTF8.GetBytes(value));
        }

        /// <summary>
        /// Deletes data for a key expression.
        /// </summary>
        public unsafe void Delete(string keyExpr)
        {
            if (string.IsNullOrEmpty(keyExpr))
                throw new ArgumentNullException(nameof(keyExpr));

            ThrowIfDisposed();

            var keyBytes = Encoding.UTF8.GetBytes(keyExpr + "\0");

            fixed (byte* keyPtr = keyBytes)
            {
                var result = NativeMethods.zenoh_delete(_handle, keyPtr);
                if (result != ZenohError.Ok)
                {
                    throw ZenohException.FromLastError("Failed to delete: error code {result}");
                }
            }
        }

        /// <summary>
        /// Puts data with encoding on a key expression.
        /// </summary>
        public unsafe void Put(string keyExpr, byte[] data, PayloadEncoding encoding)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(keyExpr))
                throw new ArgumentNullException(nameof(keyExpr));
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var keyBytes = Encoding.UTF8.GetBytes(keyExpr + "\0");

            fixed (byte* keyPtr = keyBytes)
            fixed (byte* dataPtr = data)
            {
                var result = NativeMethods.zenoh_put_with_encoding(
                    _handle,
                    keyPtr,
                    dataPtr,
                    (nuint)data.Length,
                    (ZenohEncodingId)encoding);

                if (result != ZenohError.Ok)
                {
                    throw ZenohException.FromLastError("Failed to put with encoding: error code {result}");
                }
            }
        }

        /// <summary>
        /// Puts a string with encoding on a key expression.
        /// </summary>
        public void Put(string keyExpr, string value, PayloadEncoding encoding)
        {
            Put(keyExpr, Encoding.UTF8.GetBytes(value), encoding);
        }

        /// <summary>
        /// Puts data with attachment on a key expression.
        /// </summary>
        public unsafe void Put(string keyExpr, byte[] data, IDictionary<string, string> attachment)
        {
            ThrowIfDisposed();
            
            if (string.IsNullOrEmpty(keyExpr))
                throw new ArgumentNullException(nameof(keyExpr));
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (attachment == null)
                throw new ArgumentNullException(nameof(attachment));

            var keyBytes = Encoding.UTF8.GetBytes(keyExpr + "\0");
            var items = new ZenohAttachmentItem[attachment.Count];
            var keyBuffers = new byte[attachment.Count][];  // null-terminated strings
            var valueBuffers = new byte[attachment.Count][];
            
            int i = 0;
            foreach (var kv in attachment)
            {
                keyBuffers[i] = Encoding.UTF8.GetBytes(kv.Key + "\0");  // null-terminated
                valueBuffers[i] = Encoding.UTF8.GetBytes(kv.Value);
                i++;
            }

            fixed (byte* keyPtr = keyBytes)
            fixed (byte* dataPtr = data)
            fixed (ZenohAttachmentItem* itemsPtr = items)
            {
                var handles = new GCHandle[attachment.Count * 2];
                try
                {
                    for (i = 0; i < attachment.Count; i++)
                    {
                        handles[i * 2] = GCHandle.Alloc(keyBuffers[i], GCHandleType.Pinned);
                        handles[i * 2 + 1] = GCHandle.Alloc(valueBuffers[i], GCHandleType.Pinned);
                        items[i].key = (byte*)handles[i * 2].AddrOfPinnedObject();
                        items[i].value = (byte*)handles[i * 2 + 1].AddrOfPinnedObject();
                        items[i].value_len = (nuint)valueBuffers[i].Length;
                    }

                    var result = NativeMethods.zenoh_put_with_attachment(
                        _handle,
                        keyPtr,
                        dataPtr,
                        (nuint)data.Length,
                        itemsPtr,
                        (nuint)attachment.Count);

                    if (result != ZenohError.Ok)
                    {
                        throw ZenohException.FromLastError("Failed to put with attachment: error code {result}");
                    }
                }
                finally
                {
                    foreach (var handle in handles)
                    {
                        if (handle.IsAllocated)
                            handle.Free();
                    }
                }
            }
        }

        /// <summary>
        /// Declares a querier for repeated queries on the same key expression.
        /// </summary>
        public Querier DeclareQuerier(string keyExpr)
        {
            if (string.IsNullOrEmpty(keyExpr))
                throw new ArgumentNullException(nameof(keyExpr));

            ThrowIfDisposed();
            return new Querier(this, keyExpr);
        }

        /// <summary>
        /// Declares a liveliness token for the given key expression.
        /// </summary>
        public LivelinessToken DeclareLivelinessToken(string keyExpr)
        {
            if (string.IsNullOrEmpty(keyExpr))
                throw new ArgumentNullException(nameof(keyExpr));

            ThrowIfDisposed();
            return new LivelinessToken(this, keyExpr);
        }

        /// <summary>
        /// Declares a liveliness subscriber for the given key expression.
        /// </summary>
        public LivelinessSubscriber DeclareLivelinessSubscriber(string keyExpr, Action<string, bool> callback)
        {
            if (string.IsNullOrEmpty(keyExpr))
                throw new ArgumentNullException(nameof(keyExpr));
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            ThrowIfDisposed();
            return new LivelinessSubscriber(this, keyExpr, callback);
        }

        /// <summary>
        /// Gets the Zenoh ID of this session.
        /// </summary>
        public unsafe string GetZenohId()
        {
            ThrowIfDisposed();

            var zidPtr = NativeMethods.zenoh_session_zid(_handle);
            if (zidPtr == null)
            {
                throw ZenohException.FromLastError("Failed to get Zenoh ID");
            }

            try
            {
                return Marshal.PtrToStringUTF8((IntPtr)zidPtr) ?? string.Empty;
            }
            finally
            {
                NativeMethods.zenoh_free_string(zidPtr);
            }
        }

        /// <summary>
        /// Creates a new subscriber for the specified key expression.
        /// </summary>
        public Subscriber DeclareSubscriber(string keyExpr, Action<Sample> callback)
        {
            if (string.IsNullOrEmpty(keyExpr))
                throw new ArgumentNullException(nameof(keyExpr));
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            ThrowIfDisposed();
            return new Subscriber(this, keyExpr, callback);
        }

        /// <summary>
        /// Creates a new queryable for the specified key expression.
        /// </summary>
        public Queryable DeclareQueryable(string keyExpr, Action<Query> callback)
        {
            if (string.IsNullOrEmpty(keyExpr))
                throw new ArgumentNullException(nameof(keyExpr));
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            ThrowIfDisposed();
            return new Queryable(this, keyExpr, callback);
        }

        /// <summary>
        /// Performs a get query (request-response pattern).
        /// </summary>
        public unsafe void Get(string selector, Action<Sample> callback)
        {
            if (string.IsNullOrEmpty(selector))
                throw new ArgumentNullException(nameof(selector));
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            ThrowIfDisposed();

            var selectorBytes = Encoding.UTF8.GetBytes(selector + "\0");

            // Keep delegate alive
            NativeMethods.zenoh_get_callback_delegate nativeCallback = (samplePtr, contextPtr) =>
            {
                if (samplePtr == null)
                    return;

                try
                {
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
                    callback?.Invoke(sample);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Exception in get callback: {ex}");
                }
            };

            // Pin the delegate to prevent GC
            var callbackHandle = GCHandle.Alloc(nativeCallback);

            try
            {
                fixed (byte* selectorPtr = selectorBytes)
                {
                    var result = NativeMethods.zenoh_get(_handle, selectorPtr, nativeCallback, null);

                    if (result != ZenohError.Ok)
                    {
                        throw ZenohException.FromLastError("Get query failed with error code: {result}");
                    }
                }
            }
            finally
            {
                callbackHandle.Free();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Session));
        }

        ~Session()
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
                    NativeMethods.zenoh_close(_handle);
                    _handle = null;
                }
                _disposed = true;
            }
        }
    }
}
