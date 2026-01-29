use std::env;
use std::path::PathBuf;

fn main() {
    println!("cargo:rerun-if-changed=src/lib.rs");
    println!("cargo:rerun-if-changed=build.rs");

    // Generate C# bindings using csbindgen
    let crate_dir = env::var("CARGO_MANIFEST_DIR").unwrap();
    let output_dir = PathBuf::from(&crate_dir).join("../output/generated");

    std::fs::create_dir_all(&output_dir).unwrap();

    csbindgen::Builder::default()
        .input_extern_file("src/lib.rs")
        .csharp_dll_name("zenoh_ffi")
        .csharp_dll_name_if("UNITY_IOS || UNITY_WEBGL", "__Internal")
        .csharp_class_name("NativeMethods")
        .csharp_namespace("Zenoh.Native.FFI")
        .csharp_use_function_pointer(false) // Unity compatibility
        .generate_csharp_file(output_dir.join("NativeMethods.g.cs"))
        .unwrap();

    println!("cargo:warning=C# bindings generated at {:?}", output_dir);
}
