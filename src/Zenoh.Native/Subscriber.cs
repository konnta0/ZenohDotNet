using System;
using System.Runtime.InteropServices;
using Zenoh.Native.FFI;

namespace Zenoh.Native
{
    /// <summary>
    /// Represents a Zenoh subscriber that receives data on a key expression.
    /// </summary>
    public class Subscriber : IDisposable
    {
        private IntPtr _handle;
        private readonly Session _session;
        private readonly string _keyExpr;
        private readonly Action<Sample> _callback;
        private GCHandle _callbackHandle;
        private bool _disposed;

        /// <summary>
        /// Gets the key expression this subscriber is listening on.
        /// </summary>
        public string KeyExpression => _keyExpr;

        internal Subscriber(Session session, string keyExpr, Action<Sample> callback)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _keyExpr = keyExpr ?? throw new ArgumentNullException(nameof(keyExpr));
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));

            IntPtr keyPtr = IntPtr.Zero;
            try
            {
                keyPtr = Marshal.StringToHGlobalAnsi(keyExpr);

                // Create a delegate for the native callback and pin it
                SubscriberCallbackDelegate nativeCallback = OnSampleReceived;
                _callbackHandle = GCHandle.Alloc(nativeCallback);

                IntPtr callbackPtr = Marshal.GetFunctionPointerForDelegate(nativeCallback);
                IntPtr contextPtr = GCHandle.ToIntPtr(_callbackHandle);

                _handle = NativeMethods.zenoh_declare_subscriber(
                    session.Handle,
                    keyPtr,
                    callbackPtr,
                    contextPtr);

                if (_handle == IntPtr.Zero)
                {
                    _callbackHandle.Free();
                    throw new ZenohException($"Failed to declare subscriber for key expression: {keyExpr}");
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

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void SubscriberCallbackDelegate(IntPtr sample, IntPtr context);

        private void OnSampleReceived(IntPtr samplePtr, IntPtr contextPtr)
        {
            try
            {
                if (samplePtr == IntPtr.Zero)
                    return;

                // TODO: Marshal the ZenohSample struct from samplePtr
                // For now, create a placeholder sample
                var sample = new Sample(_keyExpr, Array.Empty<byte>());

                _callback?.Invoke(sample);
            }
            catch (Exception ex)
            {
                // Swallow exceptions in callback to prevent crossing native boundary
                Console.Error.WriteLine($"Exception in subscriber callback: {ex}");
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Subscriber));
        }

        /// <summary>
        /// Finalizer for Subscriber.
        /// </summary>
        ~Subscriber()
        {
            Dispose(false);
        }

        /// <summary>
        /// Undeclares the subscriber and releases all associated resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the subscriber.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (_handle != IntPtr.Zero)
                {
                    NativeMethods.zenoh_undeclare_subscriber(_handle);
                    _handle = IntPtr.Zero;
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
