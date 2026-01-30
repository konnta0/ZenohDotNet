using System;
using System.Runtime.InteropServices;
using System.Text;
using ZenohDotNet.Native.FFI;

namespace ZenohDotNet.Native
{
    /// <summary>
    /// Represents a Zenoh queryable that responds to get queries.
    /// </summary>
    public class Queryable : IDisposable
    {
        private unsafe void* _handle;
        private readonly Session _session;
        private readonly string _keyExpr;
        private readonly Action<Query> _callback;
        private GCHandle _callbackHandle;
        private NativeMethods.zenoh_declare_queryable_callback_delegate? _nativeCallback;
        private bool _disposed;

        /// <summary>
        /// Gets the key expression this queryable is listening on.
        /// </summary>
        public string KeyExpression => _keyExpr;

        internal unsafe Queryable(Session session, string keyExpr, Action<Query> callback)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _keyExpr = keyExpr ?? throw new ArgumentNullException(nameof(keyExpr));
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));

            var keyBytes = Encoding.UTF8.GetBytes(keyExpr + "\0");

            _nativeCallback = OnQueryReceived;
            _callbackHandle = GCHandle.Alloc(_nativeCallback);

            fixed (byte* keyPtr = keyBytes)
            {
                _handle = NativeMethods.zenoh_declare_queryable(
                    session.Handle,
                    keyPtr,
                    _nativeCallback,
                    null);
            }

            if (_handle == null)
            {
                _callbackHandle.Free();
                throw new ZenohException($"Failed to declare queryable for key expression: {keyExpr}");
            }
        }

        private unsafe void OnQueryReceived(void* queryPtr, void* contextPtr)
        {
            try
            {
                if (queryPtr == null)
                    return;

                var query = new Query(queryPtr);
                _callback?.Invoke(query);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception in queryable callback: {ex}");
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Queryable));
        }

        ~Queryable()
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
                    NativeMethods.zenoh_undeclare_queryable(_handle);
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

    /// <summary>
    /// Represents a query received by a queryable.
    /// </summary>
    public class Query
    {
        private unsafe void* _handle;
        private bool _replied;

        internal unsafe Query(void* handle)
        {
            _handle = handle;
        }

        /// <summary>
        /// Gets the selector (key expression) of the query.
        /// </summary>
        public unsafe string Selector
        {
            get
            {
                byte* selectorPtr = NativeMethods.zenoh_query_selector(_handle);
                if (selectorPtr == null)
                    return string.Empty;

                try
                {
                    return Marshal.PtrToStringUTF8((IntPtr)selectorPtr) ?? string.Empty;
                }
                finally
                {
                    NativeMethods.zenoh_free_string(selectorPtr);
                }
            }
        }

        /// <summary>
        /// Replies to the query with data.
        /// </summary>
        public unsafe void Reply(string keyExpr, byte[] data)
        {
            if (_replied)
                throw new InvalidOperationException("Query has already been replied to");

            if (string.IsNullOrEmpty(keyExpr))
                throw new ArgumentNullException(nameof(keyExpr));

            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var keyBytes = Encoding.UTF8.GetBytes(keyExpr + "\0");

            fixed (byte* keyPtr = keyBytes)
            fixed (byte* dataPtr = data)
            {
                var result = NativeMethods.zenoh_query_reply(_handle, keyPtr, dataPtr, (nuint)data.Length);
                if (result != ZenohError.Ok)
                {
                    throw new ZenohException($"Failed to reply to query: error code {result}");
                }
            }

            _replied = true;
        }

        /// <summary>
        /// Replies to the query with a string.
        /// </summary>
        public void Reply(string keyExpr, string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            byte[] data = Encoding.UTF8.GetBytes(value);
            Reply(keyExpr, data);
        }
    }
}
