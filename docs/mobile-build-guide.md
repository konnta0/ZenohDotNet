# Mobile Platform Build Guide

This guide explains how to build ZenohDotNet native libraries for Android and iOS platforms.

## Prerequisites

### Android

1. **Android NDK** - Install via Android Studio or standalone
   ```bash
   # Set environment variable
   export ANDROID_NDK_HOME=/path/to/android-ndk
   # or
   export NDK_HOME=/path/to/android-ndk
   ```

2. **Rust Android targets**
   ```bash
   rustup target add aarch64-linux-android
   rustup target add armv7-linux-androideabi
   rustup target add x86_64-linux-android
   ```

### iOS

1. **Xcode** - Install from Mac App Store (macOS only)

2. **Rust iOS targets**
   ```bash
   rustup target add aarch64-apple-ios          # Device
   rustup target add aarch64-apple-ios-sim      # Simulator (Apple Silicon)
   rustup target add x86_64-apple-ios           # Simulator (Intel)
   ```

## Building

Run the build script which will automatically detect available toolchains:

```bash
./scripts/build-native.sh
```

The script will:
1. Build for the native platform
2. Cross-compile for desktop platforms (if `cross` is available)
3. Build Android libraries (if `ANDROID_NDK_HOME` is set)
4. Build iOS libraries (if on macOS with Xcode)

## Output

After building, libraries are placed in:

```
native/output/
├── android-arm64/      # Android ARM64 (arm64-v8a)
│   └── libzenoh_ffi.so
├── android-arm/        # Android ARMv7 (armeabi-v7a)
│   └── libzenoh_ffi.so
├── android-x86_64/     # Android x86_64
│   └── libzenoh_ffi.so
├── ios-arm64/          # iOS Device
│   └── libzenoh_ffi.a
├── ios-sim-arm64/      # iOS Simulator (Apple Silicon)
│   └── libzenoh_ffi.a
├── ios-sim-x64/        # iOS Simulator (Intel)
│   └── libzenoh_ffi.a
└── ios-sim-universal/  # iOS Simulator Universal
    └── libzenoh_ffi.a
```

## Copying to Unity Package

After building, run:

```bash
./scripts/copy-bindings.sh
```

This copies libraries to the Unity package structure:

```
packages/com.zenohdotnet.native/Plugins/
├── Android/
│   ├── arm64-v8a/
│   │   └── libzenoh_ffi.so
│   ├── armeabi-v7a/
│   │   └── libzenoh_ffi.so
│   └── x86_64/
│       └── libzenoh_ffi.so
└── iOS/
    └── libzenoh_ffi.a
```

## Unity Configuration

### Android

The `.meta` files are pre-configured for each Android architecture:

- `arm64-v8a` → CPU: ARM64
- `armeabi-v7a` → CPU: ARMv7
- `x86_64` → CPU: X86_64

Unity will automatically select the correct library based on the target device.

### iOS

The iOS library is a static library (`.a` file). Unity will link it during the Xcode build process.

**Important iOS Notes:**
- The library is built for `arm64` architecture (required for modern iOS devices)
- IL2CPP backend is required for iOS builds
- Ensure "Allow unsafe code" is enabled in Player Settings

## Troubleshooting

### Android: "cannot find -lzenoh_ffi"

Make sure the library exists in the correct architecture folder and the `.meta` file has the correct CPU setting.

### iOS: Undefined symbols

1. Check that the static library is included in the Xcode project
2. Verify the architecture matches (arm64)
3. Ensure all Zenoh dependencies are linked

### Build fails for Android

1. Verify `ANDROID_NDK_HOME` points to a valid NDK installation
2. Check NDK version compatibility (r21+ recommended)
3. Ensure the linker paths in `build-native.sh` match your NDK structure

### Build fails for iOS

1. Ensure Xcode command line tools are installed: `xcode-select --install`
2. Check that iOS targets are installed: `rustup target list | grep ios`
3. For simulator builds, both arm64 and x86_64 targets may be needed

## Minimum Supported Versions

- **Android**: API Level 21 (Android 5.0 Lollipop)
- **iOS**: iOS 12.0
- **Unity**: 2021.3 LTS or newer
