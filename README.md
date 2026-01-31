# ZenohDotNet

![Development Status](https://img.shields.io/badge/status-alpha-red)
![Version](https://img.shields.io/badge/version-0.1.x-blue)
![License](https://img.shields.io/badge/license-MIT-green)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)

C# bindings for [Zenoh](https://zenoh.io) distributed messaging system with embedded runtime and Unity support.

---

## ⚠️ Project Status

**This project is in early development (v0.1.x) and is NOT ready for production use.**

### Important Notices

- **Breaking Changes**: The API may change significantly between versions without prior notice
- **No Stability Guarantees**: Features may be added, modified, or removed at any time
- **Limited Testing**: The library has not been extensively tested in real-world scenarios
- **No Warranty**: This software is provided "AS IS" without warranty of any kind
- **Production Use**: DO NOT use this library in production environments

### What to Expect

- ✅ Basic functionality works (Session, Publisher, Subscriber, Query/Queryable)
- ⚠️ APIs are subject to change
- ⚠️ Performance has not been optimized
- ⚠️ Documentation may be incomplete or outdated
- ⚠️ Some features are experimental or untested

### Disclaimer

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

See the [LICENSE](LICENSE) file for full details.

---

## Overview

ZenohDotNet provides three complementary packages for using Zenoh in .NET and Unity applications:

- **ZenohDotNet.Native** - Low-level FFI bindings (.NET Standard 2.1) with embedded Zenoh runtime
- **ZenohDotNet.Client** - High-level async API for .NET 8.0+ with modern C# features
- **ZenohDotNet.Unity** - Unity-optimized wrapper with UniTask integration

### Key Features

- **Embedded Runtime**: Zenoh C library included - no separate installation required
- **Cross-Platform**: Windows, Linux, macOS on x64 and ARM64
- **Mobile Support**: Android (arm64-v8a, armeabi-v7a, x86_64) and iOS (arm64)
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
│ ZenohDotNet.Native    │  ← Layer 1: Low-level FFI bindings
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

### .NET 8.0+ (ZenohDotNet.Client)

```bash
dotnet add package ZenohDotNet.Client
```

```csharp
using ZenohDotNet.Client;

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

### Unity (ZenohDotNet.Unity)

1. Install [NuGet for Unity](https://github.com/GlitchEnzo/NuGetForUnity)
2. Install `ZenohDotNet.Native` via NuGet for Unity
3. Add `com.zenohdotnet.unity` via UPM

```csharp
using UnityEngine;
using ZenohDotNet.Unity;
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
   git clone --recursive https://github.com/konnta0/ZenohDotNet.git
   cd ZenohDotNet
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
   dotnet build ZenohDotNet.slnx -c Release
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
ZenohDotNet/
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
│   ├── ZenohDotNet.Native/           # Low-level bindings (.NET Standard 2.1)
│   ├── ZenohDotNet.Client/           # High-level API (.NET 8.0)
│   └── ZenohDotNet.Unity/            # Unity project (package development)
│       └── Assets/Plugins/com.zenohdotnet.unity/  # UPM package
│
├── tests/                      # Test projects
├── samples/                    # Example code
│   ├── dotnet/                 # .NET console samples
│   └── unity/                  # Unity sample projects
│       └── ZenohUnityExample/  # Unity sample project
├── scripts/                    # Build automation
├── packages/                   # Build output (gitignored)
│   ├── nuget/                  # NuGet packages
│   └── upm/                    # Unity packages
│
└── ZenohDotNet.slnx          # Solution file (.NET projects only)
```

## Packages

### ZenohDotNet.Native

[![NuGet](https://img.shields.io/nuget/v/ZenohDotNet.Native.svg)](https://www.nuget.org/packages/ZenohDotNet.Native/)

Low-level P/Invoke bindings to Zenoh C library. Includes native runtime for all supported platforms.

- **Target**: .NET Standard 2.1
- **Use case**: Low-level access, Unity compatibility
- **API style**: Synchronous with IDisposable

[Documentation](src/ZenohDotNet.Native/README.md)

### ZenohDotNet.Client

[![NuGet](https://img.shields.io/nuget/v/ZenohDotNet.Client.svg)](https://www.nuget.org/packages/ZenohDotNet.Client/)

Modern async C# API built on ZenohDotNet.Native.

- **Target**: .NET 8.0+
- **Use case**: Modern .NET applications
- **API style**: Async/await with IAsyncDisposable
- **Features**: JSON support, record types, pattern matching

[Documentation](src/ZenohDotNet.Client/README.md)

### ZenohDotNet.Unity

Unity-optimized wrapper with UniTask integration.

- **Package**: `com.zenohdotnet.unity` (UPM)
- **Target**: .NET Standard 2.1
- **Use case**: Unity 2021.2+ projects
- **API style**: UniTask async + synchronous methods
- **Features**: Main thread callbacks, Unity lifecycle integration
- **Dependencies**: ZenohDotNet.Native (NuGet for Unity), com.cysharp.unitask (UPM)

**Installation via Git URL:**
```
https://github.com/konnta0/ZenohDotNet.git?path=src/ZenohDotNet.Unity/Assets/Plugins/com.zenohdotnet.unity
```

[Documentation](docs/unity-integration.md)

## Comparison with Other Implementations

| Feature | ZenohForCSharp | zenoh-csharp |
|---------|----------------|--------------|
| Embedded Runtime | ✅ Yes | ❌ No (requires external build) |
| Monorepo | ✅ Yes | ❌ No |
| Unity Support | ✅ First-class (UniTask, UPM) | ⚠️ Limited |
| .NET 8.0 Support | ✅ Yes (ZenohDotNet.Client) | ❌ No |
| Cross-platform NuGet | ✅ All platforms in one package | ⚠️ Separate builds |
| Build Automation | ✅ Full CI/CD | ⚠️ Manual |

## Documentation

- [Getting Started Guide](docs/getting-started.md) - Quick start for .NET and Unity
- [Build Guide](docs/build-guide.md) - Building from source
- [Mobile Build Guide](docs/mobile-build-guide.md) - Building for Android and iOS
- [Unity Integration Guide](docs/unity-integration.md) - Unity-specific integration guide
- [API Reference](docs/api-reference/) - Complete API documentation
  - [ZenohDotNet.Native API](docs/api-reference/zenoh-native.md) - Low-level FFI bindings
  - [ZenohDotNet.Client API](docs/api-reference/zenoh-client.md) - High-level async API
  - [ZenohDotNet.Unity API](docs/api-reference/zenoh-unity.md) - Unity-optimized API

## Examples

See the [samples](samples/) directory for complete examples:

- **dotnet/Publisher** - Basic publisher example
- **dotnet/Subscriber** - Basic subscriber example
- **dotnet/LivelinessToken** - Liveliness token example (declare alive resource)
- **dotnet/LivelinessSubscriber** - Liveliness subscriber example (monitor resource presence)
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
- [x] Query/Queryable support (request-response pattern)
- [x] Cross-platform native library packaging
- [x] .NET Standard 2.1 support (ZenohDotNet.Native)
- [x] .NET 8.0 support (ZenohDotNet.Client)
- [x] Unity support with UniTask
- [x] Build automation scripts
- [x] CI/CD pipeline (GitHub Actions)
- [x] Comprehensive documentation and samples
- [x] Liveliness API
- [x] Advanced configuration API
- [x] Android and iOS support for Unity
- [x] Integration tests with running Zenoh instances(p2p only)
- [x] Performance benchmarks
- [x] Full Zenoh API coverage
- [x] Performance optimizations (restricted)
- [x] **Source Generator for typed messages** (ZenohDotNet.Generator)

### v0.2.0 (Future)
- [ ] Advanced Unity features (ScriptableObjects, etc.)
- [ ] Extensive documentation and samples

## Source Generator

ZenohDotNet includes an incremental source generator that provides zero-copy serialization for your message types.

### Installation

```xml
<PackageReference Include="ZenohDotNet.Abstractions" Version="0.1.0" />
<PackageReference Include="ZenohDotNet.Generator" Version="0.1.0" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
```

### Usage

Define your message type with the `[ZenohMessage]` attribute:

```csharp
using ZenohDotNet.Abstractions;

[ZenohMessage("sensor/temperature")]  // Optional default key
public partial struct SensorData
{
    public double Temperature { get; init; }
    public double Humidity { get; init; }
    public DateTime Timestamp { get; init; }
}
```

The generator creates:
- `ToBytes()` - Instance serialization
- `Serialize(in T)` - Static zero-copy serialization  
- `SerializeTo(in T, Span<byte>)` - Stack-allocated serialization
- `Deserialize(byte[])` / `Deserialize(ReadOnlySpan<byte>)`
- `TryDeserialize(ReadOnlySpan<byte>, out T)`
- `DefaultKeyExpression` - Static property (if default key specified)
- `BuildKeyExpression()` - Dynamic key construction (if key parameters specified)
- `SubscriptionPattern` - Wildcard pattern for subscribing

### Dynamic Keys with Placeholders

Use `[ZenohKeyParameter]` for keys that include user IDs or other runtime values:

```csharp
[ZenohMessage("game/player/{PlayerId}/position")]
[ZenohSubscriptionPattern("game/player/*/position")]  // Wildcard for subscribing
public partial struct PlayerPosition
{
    [ZenohKeyParameter]
    public string PlayerId { get; init; }
    
    public float X { get; init; }
    public float Y { get; init; }
}

// Usage
var position = new PlayerPosition { PlayerId = "user123", X = 10, Y = 20 };

// Instance method - builds key from properties
var key = position.BuildKeyExpression();  // "game/player/user123/position"

// Static method - pass parameters directly
var key2 = PlayerPosition.BuildKeyExpression("user456");

// Subscribe to all players
subscriber.Subscribe(PlayerPosition.SubscriptionPattern);  // "game/player/*/position"
```

### Zero-Copy Publishing

```csharp
var data = new SensorData { Temperature = 25.5, Humidity = 60.0, Timestamp = DateTime.UtcNow };

// Heap allocation (simple)
var bytes = data.ToBytes();

// Zero-copy with stackalloc (high-performance)
Span<byte> buffer = stackalloc byte[256];
var written = SensorData.SerializeTo(in data, buffer);
publisher.Put(buffer.Slice(0, written));
```

### Supported Types

- `struct` (recommended for performance - uses `in` parameter)
- `class`
- `record` / `record struct`

### Encoding Options

```csharp
[ZenohMessage(Encoding = ZenohEncoding.Json)]       // Default
[ZenohMessage(Encoding = ZenohEncoding.MessagePack)] // Requires MessagePack package
[ZenohMessage(Encoding = ZenohEncoding.Custom)]      // Custom IZenohSerializer<T>
```

### Unity Integration

The Unity package includes the Source Generator in the `Editor` folder. The generator is automatically available when you import the ZenohDotNet package via UPM.

**Package structure:**
```
com.zenohdotnet.native/
├── Runtime/
│   ├── ZenohDotNet.Native.asmdef
│   └── ZenohDotNet.Abstractions.dll  (attributes for [ZenohMessage])
├── Editor/
│   ├── ZenohDotNet.Generator.asmdef
│   ├── ZenohDotNet.Generator.dll     (source generator)
│   └── ZenohDotNet.Abstractions.dll
└── Plugins/
    └── (native libraries)
```

**Usage in Unity:**
```csharp
using ZenohDotNet.Abstractions;

[ZenohMessage("game/player/position")]
public partial struct PlayerPosition
{
    public float X;
    public float Y;
    public float Z;
}
```

## Benchmarks

Performance benchmarks are available in the `benchmarks/` directory.

### Running Benchmarks

```bash
# Run all benchmarks
dotnet run -c Release --project benchmarks/ZenohDotNet.Benchmarks

# Run specific benchmark class
dotnet run -c Release --project benchmarks/ZenohDotNet.Benchmarks -- --filter "*SessionBenchmarks*"

# Run quick benchmarks (shorter runs)
dotnet run -c Release --project benchmarks/ZenohDotNet.Benchmarks -- --filter "*" --job Short
```

### Available Benchmarks

- **SessionBenchmarks** - Session open/close, resource declaration
- **PubSubBenchmarks** - Publisher/Subscriber throughput with various payload sizes
- **QueryBenchmarks** - Query/Reply latency (single and concurrent)
- **ThroughputBenchmarks** - Maximum messages per second
- **LatencyBenchmarks** - End-to-end message latency
- **AllocationBenchmarks** - Memory allocation patterns
- **SessionConfigBenchmarks** - Configuration serialization

## Performance Tips

### Low-Latency Publishing

For lowest latency, use synchronous `Put()` methods on a dedicated thread:

```csharp
// Zero-copy publishing with ReadOnlySpan
publisher.Put(myData.AsSpan());

// Synchronous string publishing
publisher.Put("Hello");
```

### Memory Efficiency

Use `ReadOnlyMemory<byte>` or `ReadOnlySpan<byte>` to avoid copying:

```csharp
// Async with ReadOnlyMemory (avoids array copy)
await publisher.PutAsync(buffer.AsMemory());

// Sync with ReadOnlySpan (zero-copy)
publisher.Put(buffer.AsSpan());
```

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

- GitHub Issues: [Report bugs or request features](https://github.com/konnta0/ZenohDotNet/issues)
- Discussions: [Ask questions and share ideas](https://github.com/konnta0/ZenohDotNet/discussions)

---

Made with ❤️ for the .NET and Unity communities
