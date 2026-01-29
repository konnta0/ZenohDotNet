# ZenohForCSharp

C# bindings for [Zenoh](https://zenoh.io) distributed messaging system with embedded runtime and Unity support.

## Overview

ZenohForCSharp provides three complementary packages for using Zenoh in .NET and Unity applications:

- **Zenoh.Native** - Low-level FFI bindings (.NET Standard 2.1) with embedded Zenoh runtime
- **Zenoh.Client** - High-level async API for .NET 8.0+ with modern C# features
- **Zenoh.Unity** - Unity-optimized wrapper with UniTask integration

### Key Features

- **Embedded Runtime**: Zenoh C library included - no separate installation required
- **Cross-Platform**: Windows, Linux, macOS on x64 and ARM64
- **Unity Support**: First-class Unity integration with UniTask
- **Modern C#**: Leverages .NET 8.0 features (async/await, records, pattern matching)
- **Type Safe**: Strongly-typed APIs with compile-time safety
- **Memory Safe**: Proper resource management with IDisposable/IAsyncDisposable

## Architecture

```
┌─────────────────┐
│  Zenoh C FFI    │  ← zenoh-c (embedded as submodule)
│  (zenoh-c)      │
└────────┬────────┘
         │
┌────────▼────────┐
│  Rust FFI       │  ← csbindgen generates C# code
│  (zenoh-ffi)    │     Dynamic libraries (.dll/.so/.dylib)
└────────┬────────┘
         │
┌────────▼────────┐
│ Zenoh.Native    │  ← Layer 1: Low-level FFI bindings
│ (.NET Std 2.1)  │     NuGet package with embedded runtime
│ [NuGet]         │
└────────┬────────┘
         │
    ┌────┴──────┐
    │           │
┌───▼──────┐ ┌──▼─────────┐
│ Unity    │ │ Client     │
│ Wrapper  │ │ (.NET 8.0) │
│ (.NETStd │ │ [NuGet]    │
│  2.1)    │ └────────────┘
│ [UPM]    │
└──────────┘
```

## Quick Start

### .NET 8.0+ (Zenoh.Client)

```bash
dotnet add package Zenoh.Client
```

```csharp
using Zenoh.Client;

// Open session
await using var session = await Session.OpenAsync();

// Publisher
await using var publisher = await session.DeclarePublisherAsync("demo/example/test");
await publisher.PutAsync("Hello, Zenoh!");

// Subscriber
await using var subscriber = await session.DeclareSubscriberAsync("demo/example/**", sample =>
{
    Console.WriteLine($"Received: {sample.GetPayloadAsString()}");
});

await Task.Delay(-1);
```

### Unity (Zenoh.Unity)

1. Install [NuGet for Unity](https://github.com/GlitchEnzo/NuGetForUnity)
2. Install `Zenoh.Native` via NuGet for Unity
3. Add `com.zenoh.unity` via UPM

```csharp
using UnityEngine;
using Zenoh.Unity;
using Cysharp.Threading.Tasks;

public class ZenohExample : MonoBehaviour
{
    private Session session;
    private Publisher publisher;

    async void Start()
    {
        session = await Session.OpenAsync(this.GetCancellationTokenOnDestroy());
        publisher = await session.DeclarePublisherAsync("unity/example/data");
        Debug.Log("Zenoh initialized");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            publisher.Put($"Hello from Unity at {Time.time}");
        }
    }

    void OnDestroy()
    {
        publisher?.Dispose();
        session?.Dispose();
    }
}
```

## Supported Platforms

| Platform | Architecture | Status |
|----------|-------------|--------|
| Windows | x64 | ✅ Supported |
| Windows | ARM64 | ✅ Supported |
| Linux | x64 | ✅ Supported |
| Linux | ARM64 | ✅ Supported |
| macOS | x64 (Intel) | ✅ Supported |
| macOS | ARM64 (Apple Silicon) | ✅ Supported |

### Unity Platforms

- Windows (x64, ARM64)
- Linux (x64, ARM64)
- macOS (x64, ARM64)
- Android (ARM64) - Coming soon
- iOS (ARM64) - Coming soon

## Building from Source

### Prerequisites

- [Rust](https://rustup.rs/) (latest stable)
- [.NET SDK 8.0+](https://dotnet.microsoft.com/download)
- CMake 3.16+ (for building zenoh-c)
- C/C++ compiler toolchain

### Build Steps

1. **Clone the repository with submodules:**
   ```bash
   git clone --recursive https://github.com/konnta0/ZenohForCSharp-internal.git
   cd ZenohForCSharp-internal
   ```

2. **Build Rust FFI for your platform:**
   ```bash
   # Unix/Linux/macOS
   ./scripts/build-native.sh

   # Windows
   .\scripts\build-native.ps1
   ```

3. **Copy generated bindings:**
   ```bash
   # Unix/Linux/macOS
   ./scripts/copy-bindings.sh

   # Windows
   .\scripts\copy-bindings.ps1
   ```

4. **Build C# projects:**
   ```bash
   dotnet build ZenohForCSharp.sln -c Release
   ```

5. **Create NuGet packages:**
   ```bash
   # Unix/Linux/macOS
   ./scripts/pack-nuget.sh

   # Windows - Similar script available
   ```

6. **Create UPM package (for Unity):**
   ```bash
   ./scripts/pack-upm.sh
   ```

### Cross-Compilation

For building all platforms from a single machine, install [cross](https://github.com/cross-rs/cross):

```bash
cargo install cross
```

The build scripts will automatically use `cross` if available to build for all target platforms.

## Project Structure

```
ZenohForCSharp-internal/
├── native/                      # Rust FFI layer
│   ├── zenoh-ffi/              # Rust project with csbindgen
│   │   ├── zenoh-c/            # zenoh-c submodule
│   │   ├── src/lib.rs          # FFI function definitions
│   │   └── build.rs            # Build script + csbindgen config
│   └── output/                 # Build artifacts (gitignored)
│       ├── generated/          # Generated C# code
│       └── {rid}/              # Native libraries per platform
│
├── src/                        # C# projects
│   ├── Zenoh.Native/           # Low-level bindings (.NET Standard 2.1)
│   ├── Zenoh.Client/           # High-level API (.NET 8.0)
│   └── Zenoh.Unity/            # Unity wrapper (.NET Standard 2.1)
│
├── tests/                      # Test projects
├── samples/                    # Example code
├── scripts/                    # Build automation
├── packages/                   # Build output (gitignored)
│   ├── nuget/                  # NuGet packages
│   └── upm/                    # Unity packages
│
└── ZenohForCSharp.sln          # Solution file
```

## Packages

### Zenoh.Native

[![NuGet](https://img.shields.io/nuget/v/Zenoh.Native.svg)](https://www.nuget.org/packages/Zenoh.Native/)

Low-level P/Invoke bindings to Zenoh C library. Includes native runtime for all supported platforms.

- **Target**: .NET Standard 2.1
- **Use case**: Low-level access, Unity compatibility
- **API style**: Synchronous with IDisposable

[Documentation](src/Zenoh.Native/README.md)

### Zenoh.Client

[![NuGet](https://img.shields.io/nuget/v/Zenoh.Client.svg)](https://www.nuget.org/packages/Zenoh.Client/)

Modern async C# API built on Zenoh.Native.

- **Target**: .NET 8.0+
- **Use case**: Modern .NET applications
- **API style**: Async/await with IAsyncDisposable
- **Features**: JSON support, record types, pattern matching

[Documentation](src/Zenoh.Client/README.md)

### Zenoh.Unity

Unity-optimized wrapper with UniTask integration.

- **Target**: .NET Standard 2.1
- **Use case**: Unity 2021.2+ projects
- **API style**: UniTask async + synchronous methods
- **Features**: Main thread callbacks, Unity lifecycle integration

[Documentation](src/Zenoh.Unity/README.md)

## Comparison with Other Implementations

| Feature | ZenohForCSharp | zenoh-csharp |
|---------|----------------|--------------|
| Embedded Runtime | ✅ Yes | ❌ No (requires external build) |
| Monorepo | ✅ Yes | ❌ No |
| Unity Support | ✅ First-class (UniTask, UPM) | ⚠️ Limited |
| .NET 8.0 Support | ✅ Yes (Zenoh.Client) | ❌ No |
| Cross-platform NuGet | ✅ All platforms in one package | ⚠️ Separate builds |
| Build Automation | ✅ Full CI/CD | ⚠️ Manual |

## Documentation

- [Getting Started Guide](docs/getting-started.md) - Coming soon
- [API Reference](docs/api-reference/) - Coming soon
- [Unity Integration Guide](docs/unity-integration.md) - Coming soon
- [Build Guide](docs/build-guide.md) - Coming soon

## Examples

See the [samples](samples/) directory for complete examples:

- **dotnet/Publisher** - Basic publisher example
- **dotnet/Subscriber** - Basic subscriber example
- **unity/ZenohUnityExample** - Unity project example

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

### Development Workflow

1. Make changes to code
2. Build Rust FFI if needed: `./scripts/build-native.sh`
3. Copy bindings: `./scripts/copy-bindings.sh`
4. Build C#: `dotnet build`
5. Run tests: `dotnet test`

## Roadmap

### v0.1.0 (Current)
- [x] Basic Session, Publisher, Subscriber
- [x] Cross-platform native library packaging
- [x] .NET Standard 2.1 support (Zenoh.Native)
- [x] .NET 8.0 support (Zenoh.Client)
- [x] Unity support with UniTask
- [x] Build automation scripts

### v0.2.0 (Planned)
- [ ] Query/Queryable support
- [ ] Liveliness API
- [ ] Configuration API
- [ ] Android and iOS support for Unity
- [ ] CI/CD pipeline
- [ ] Comprehensive tests

### v1.0.0 (Future)
- [ ] Full Zenoh API coverage
- [ ] Performance optimizations
- [ ] Advanced Unity features (ScriptableObjects, etc.)
- [ ] Extensive documentation and samples

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [Eclipse Zenoh](https://zenoh.io) - The underlying distributed messaging system
- [csbindgen](https://github.com/Cysharp/csbindgen) - Rust FFI to C# binding generator
- [UniTask](https://github.com/Cysharp/UniTask) - Unity async/await support

## Links

- [Zenoh Website](https://zenoh.io)
- [Zenoh GitHub](https://github.com/eclipse-zenoh/zenoh)
- [Zenoh C Library](https://github.com/eclipse-zenoh/zenoh-c)
- [NuGet.org](https://www.nuget.org/)
- [Unity Asset Store](https://assetstore.unity.com/) - Coming soon

## Support

- GitHub Issues: [Report bugs or request features](https://github.com/konnta0/ZenohForCSharp-internal/issues)
- Discussions: [Ask questions and share ideas](https://github.com/konnta0/ZenohForCSharp-internal/discussions)

---

Made with ❤️ for the .NET and Unity communities
