use std::env;
use std::path::PathBuf;

fn main() {
    println!("cargo:rerun-if-changed=src/lib.rs");
    println!("cargo:rerun-if-changed=build.rs");

    // TODO: Uncomment when zenoh-c submodule is added
    // build_zenoh_c();

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

// TODO: Implement zenoh-c build when submodule is added
#[allow(dead_code)]
fn build_zenoh_c() {
    let manifest_dir = env::var("CARGO_MANIFEST_DIR").unwrap();
    let zenoh_c_dir = PathBuf::from(&manifest_dir).join("zenoh-c");

    if !zenoh_c_dir.exists() {
        panic!(
            "zenoh-c submodule not found at {:?}. Please run: git submodule update --init --recursive",
            zenoh_c_dir
        );
    }

    // Build zenoh-c using CMake
    let dst = cmake::Config::new(&zenoh_c_dir)
        .define("CMAKE_BUILD_TYPE", "Release")
        .define("ZENOHC_BUILD_SHARED", "ON")
        .define("ZENOHC_BUILD_STATIC", "OFF")
        .build();

    println!("cargo:rustc-link-search=native={}/lib", dst.display());
    println!("cargo:rustc-link-lib=dylib=zenohc");

    // Platform-specific linker settings
    let target = env::var("TARGET").unwrap();
    if target.contains("windows") {
        println!("cargo:rustc-link-lib=dylib=ws2_32");
        println!("cargo:rustc-link-lib=dylib=userenv");
    }
}
