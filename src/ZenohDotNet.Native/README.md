# ZenohDotNet.Native

Low-level Zenoh FFI bindings for .NET with embedded runtime.

## Overview

ZenohDotNet.Native provides P/Invoke bindings to the Zenoh native library (zenoh-ffi), allowing .NET applications to use Zenoh's distributed messaging capabilities. The native Zenoh runtime is embedded in the NuGet package, so no separate installation is required.

## Features

- **Embedded Runtime**: Zenoh native library is included in the package
- **Cross-Platform**: Supports Windows, Linux, and macOS on x64 and ARM64
- **Mobile Support**: Android (arm64-v8a, armeabi-v7a, x86_64) and iOS (arm64)
- **Memory Safe**: Proper IDisposable implementation for resource management
- **.NET Standard 2.1**: Compatible with .NET Core 3.0+, .NET 5+, and Unity 2021.2+
- **Liveliness API**: Monitor resource presence with liveliness tokens
- **Attachments**: Send key-value metadata alongside payloads

## Supported Platforms

- Windows (x64, ARM64)
- Linux (x64, ARM64)
- macOS (x64, ARM64)
- Android (arm64-v8a, armeabi-v7a, x86_64)
- iOS (arm64)

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

// Publish with encoding
publisher.Put(data, PayloadEncoding.ApplicationJson);
```

### Publishing with Attachments

```csharp
// Attachments are key-value metadata sent alongside the payload
var attachment = new Dictionary<string, string>
{
    ["sender"] = "device-001",
    ["priority"] = "high"
};
session.Put("demo/example/test", "Hello!", attachment);
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

### Query/Queryable (Request-Response)

```csharp
// Queryable (server)
using var queryable = session.DeclareQueryable("demo/query", query =>
{
    Console.WriteLine($"Query received: {query.Selector}");
    query.Reply("demo/query", "Response data");
});

// Query (client)
session.Get("demo/query", sample =>
{
    Console.WriteLine($"Reply: {sample.GetPayloadAsString()}");
});
```

### Liveliness API

Monitor resource presence using liveliness tokens:

```csharp
// Declare a liveliness token (resource is "alive" while token exists)
using var token = session.DeclareLivelinessToken("my/resource/path");

// Subscribe to liveliness changes
using var liveSub = session.DeclareLivelinessSubscriber("my/**", (keyExpr, isAlive) =>
{
    if (isAlive)
        Console.WriteLine($"{keyExpr} is now alive");
    else
        Console.WriteLine($"{keyExpr} has died");
});
```

### Delete

```csharp
// Delete a key expression
session.Delete("demo/example/test");
```

## API Reference

### Session

- `Session()` - Opens a session with default configuration
- `Session(string configJson)` - Opens a session with JSON configuration
- `DeclarePublisher(string keyExpr)` - Creates a publisher
- `DeclarePublisher(string keyExpr, PublisherOptions options)` - Creates a publisher with options
- `DeclareSubscriber(string keyExpr, Action<Sample> callback)` - Creates a subscriber
- `DeclareQueryable(string keyExpr, Action<Query> callback)` - Creates a queryable
- `DeclareQuerier(string keyExpr)` - Creates a querier for repeated queries
- `Get(string selector, Action<Sample> callback)` - Performs a query
- `Put(string keyExpr, byte[] data)` - Direct put without publisher
- `Put(string keyExpr, byte[] data, IDictionary<string, string> attachment)` - Put with attachment
- `Delete(string keyExpr)` - Delete a key expression
- `DeclareLivelinessToken(string keyExpr)` - Declare a liveliness token
- `DeclareLivelinessSubscriber(string keyExpr, Action<string, bool> callback)` - Subscribe to liveliness changes
- `GetZenohId()` - Gets the session's Zenoh ID

### Publisher

- `Put(byte[] data)` - Publishes binary data
- `Put(string value)` - Publishes a UTF-8 string
- `Put(byte[] data, PayloadEncoding encoding)` - Publishes with encoding
- `KeyExpression` - Gets the key expression

### Subscriber

- Automatically receives data via callback
- Callback receives `Sample` objects

### Sample

- `KeyExpression` - The key expression of the sample
- `Payload` - Raw byte array payload
- `Kind` - Sample kind (Put or Delete)
- `Encoding` - Payload encoding
- `Timestamp` - Optional timestamp
- `GetPayloadAsString()` - Converts payload to UTF-8 string

### Query

- `Selector` - The query selector
- `Payload` - Optional query payload
- `Reply(string keyExpr, byte[] data)` - Reply to the query
- `Reply(string keyExpr, string value)` - Reply with a string

### LivelinessToken

- `KeyExpression` - The token's key expression
- Disposing the token signals the resource is no longer alive

### LivelinessSubscriber

- Receives `(string keyExpr, bool isAlive)` callbacks
- `isAlive = true` when a token is declared
- `isAlive = false` when a token is dropped

## Building from Source

See the main repository README for build instructions.

## License

This project is licensed under the MIT License.
