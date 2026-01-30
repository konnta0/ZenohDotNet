# ZenohDotNet.Native

Low-level Zenoh FFI bindings for .NET with embedded runtime.

## Overview

ZenohDotNet.Native provides P/Invoke bindings to the Zenoh C library, allowing .NET applications to use Zenoh's distributed messaging capabilities. The native Zenoh runtime is embedded in the NuGet package, so no separate installation is required.

## Features

- **Embedded Runtime**: Zenoh C library is included in the package
- **Cross-Platform**: Supports Windows, Linux, and macOS on x64 and ARM64
- **Memory Safe**: Proper IDisposable implementation for resource management
- **.NET Standard 2.1**: Compatible with .NET Core 3.0+, .NET 5+, and Unity 2021.2+

## Supported Platforms

- Windows (x64, ARM64)
- Linux (x64, ARM64)
- macOS (x64, ARM64)

## Installation

```bash
dotnet add package ZenohDotNet.Native
```

## Usage

### Creating a Session

```csharp
using ZenohDotNet.Native;

// Open a session with default configuration
using var session = new Session();

// Or with custom JSON configuration
var config = "{\"mode\":\"peer\"}";
using var session = new Session(config);
```

### Publishing Data

```csharp
using var session = new Session();
using var publisher = session.DeclarePublisher("demo/example/test");

// Publish a string
publisher.Put("Hello, Zenoh!");

// Publish bytes
byte[] data = { 1, 2, 3, 4 };
publisher.Put(data);
```

### Subscribing to Data

```csharp
using var session = new Session();
using var subscriber = session.DeclareSubscriber("demo/example/**", sample =>
{
    Console.WriteLine($"Received on {sample.KeyExpression}: {sample.GetPayloadAsString()}");
});

// Keep the application running to receive messages
Console.ReadLine();
```

## API Reference

### Session

- `Session()` - Opens a session with default configuration
- `Session(string configJson)` - Opens a session with JSON configuration
- `DeclarePublisher(string keyExpr)` - Creates a publisher
- `DeclareSubscriber(string keyExpr, Action<Sample> callback)` - Creates a subscriber

### Publisher

- `Put(byte[] data)` - Publishes binary data
- `Put(string value)` - Publishes a UTF-8 string

### Subscriber

- Automatically receives data via callback
- Callback receives `Sample` objects

### Sample

- `KeyExpression` - The key expression of the sample
- `Payload` - Raw byte array payload
- `GetPayloadAsString()` - Converts payload to UTF-8 string

## Building from Source

See the main repository README for build instructions.

## License

This project is licensed under the MIT License.
