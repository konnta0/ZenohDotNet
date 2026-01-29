// This file will be replaced by the generated code from csbindgen
// Run: cargo build in native/zenoh-ffi, then copy-bindings.sh

using System;
using System.Runtime.InteropServices;

namespace Zenoh.Native.FFI
{
    /// <summary>
    /// Native methods for Zenoh FFI. This is a placeholder until csbindgen generates the actual bindings.
    /// </summary>
    public static class NativeMethods
    {
        private const string LibraryName = "zenoh_ffi";

        // Placeholder methods - these will be replaced by csbindgen generated code

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr zenoh_open(IntPtr config_json);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void zenoh_close(IntPtr session);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr zenoh_declare_publisher(IntPtr session, IntPtr key_expr);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int zenoh_publisher_put(IntPtr publisher, IntPtr payload, UIntPtr payload_len);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void zenoh_undeclare_publisher(IntPtr publisher);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr zenoh_declare_subscriber(
            IntPtr session,
            IntPtr key_expr,
            IntPtr callback,
            IntPtr context);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void zenoh_undeclare_subscriber(IntPtr subscriber);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr zenoh_get_error_message();

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void zenoh_free_string(IntPtr s);
    }
}
