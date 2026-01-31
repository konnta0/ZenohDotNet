# ZenohDotNet.Client

High-level async Zenoh client library for .NET 8.0+ with modern C# features.

## Overview

ZenohDotNet.Client provides a modern, idiomatic .NET API for Zenoh, built on top of ZenohDotNet.Native. It leverages .NET 8.0 features like async/await, IAsyncDisposable, record types, and more for a superior developer experience.

## Features

- **Async/Await**: Full async API using Task-based patterns
- **CancellationToken Support**: All async methods support cancellation
- **Modern C#**: Utilizes .NET 8.0 features (records, pattern matching, nullable reference types)
- **Memory Efficient**: Leverages Span<T> and modern .NET optimizations
- **JSON Support**: Built-in System.Text.Json integration
- **Type Safe**: Strongly-typed APIs with compile-time safety
- **IAsyncDisposable**: Proper async resource cleanup
- **Liveliness API**: Monitor resource presence with liveliness tokens
- **Attachments**: Send key-value metadata alongside payloads

## Installation

```bash
dotnet add package ZenohDotNet.Client
```

This will automatically install the required ZenohDotNet.Native dependency.

## Requirements

- .NET 8.0 or later
- Supported platforms: Windows, Linux, macOS (x64 and ARM64)

## Usage

### Creating a Session

```csharp
using ZenohDotNet.Client;

// Open a session with default configuration
await using var session = await Session.OpenAsync();

// Or with custom JSON configuration
var config = "{\"mode\":\"peer\"}";
await using var session = await Session.OpenAsync(config);

// Or with strongly-typed configuration
var sessionConfig = new SessionConfig
{
    Mode = SessionMode.Peer,
    Connect = new ConnectConfig
    {
        Endpoints = ["tcp/192.168.1.100:7447"]
    }
};
await using var session = await Session.OpenAsync(sessionConfig);
```

### Publishing Data

```csharp
await using var session = await Session.OpenAsync();
await using var publisher = await session.DeclarePublisherAsync("demo/example/test");

// Publish a string
await publisher.PutAsync("Hello, Zenoh!");

// Publish bytes
byte[] data = [1, 2, 3, 4];
await publisher.PutAsync(data);

// Publish any type (uses ToString)
await publisher.PutAsync(42);
await publisher.PutAsync(DateTime.Now);
```

### Publishing with Attachments

```csharp
// Attachments are key-value metadata sent alongside the payload
var attachment = new Dictionary<string, string>
{
    ["sender"] = "device-001",
    ["priority"] = "high"
};
await session.PutWithAttachmentAsync("demo/example/test", "Hello!", attachment);
```

### Subscribing to Data

```csharp
await using var session = await Session.OpenAsync();
await using var subscriber = await session.DeclareSubscriberAsync("demo/example/**", sample =>
{
    Console.WriteLine($"Received on {sample.KeyExpression}: {sample.GetPayloadAsString()}");
});

// Keep the application running to receive messages
await Task.Delay(-1);
```

### Query with Timeout

```csharp
await using var session = await Session.OpenAsync();

// Query with 5 second timeout
await session.GetAsync("demo/example/**", sample =>
{
    Console.WriteLine($"Reply: {sample.GetPayloadAsString()}");
}, timeout: TimeSpan.FromSeconds(5));
```

### CancellationToken Support

All async methods support `CancellationToken`:

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

await using var session = await Session.OpenAsync(cts.Token);
await using var publisher = await session.DeclarePublisherAsync("demo/test", cts.Token);
await publisher.PutAsync("Hello", cts.Token);
```

### Working with JSON

```csharp
public record SensorData(double Temperature, double Humidity, DateTime Timestamp);

// Publisher
await using var publisher = await session.DeclarePublisherAsync("sensors/room1");
var data = new SensorData(23.5, 45.2, DateTime.UtcNow);
var json = JsonSerializer.Serialize(data);
await publisher.PutAsync(json);

// Subscriber
await using var subscriber = await session.DeclareSubscriberAsync("sensors/**", sample =>
{
    var data = sample.TryGetPayloadAsJson<SensorData>();
    if (data is not null)
    {
        Console.WriteLine($"Temp: {data.Temperature}°C, Humidity: {data.Humidity}%");
    }
});
```

### Liveliness API

Monitor resource presence using liveliness tokens:

```csharp
// Declare a liveliness token (resource is "alive" while token exists)
await using var token = await session.DeclareLivelinessTokenAsync("my/resource/path");

// Subscribe to liveliness changes
await using var liveSub = await session.DeclareLivelinessSubscriberAsync("my/**", (keyExpr, isAlive) =>
{
    if (isAlive)
        Console.WriteLine($"{keyExpr} is now alive");
    else
        Console.WriteLine($"{keyExpr} has died");
});
```

### Pattern Matching

```csharp
await using var subscriber = await session.DeclareSubscriberAsync("demo/**", sample =>
{
    var result = sample.KeyExpression switch
    {
        "demo/test" => HandleTest(sample),
        string key when key.StartsWith("demo/sensors") => HandleSensor(sample),
        _ => HandleDefault(sample)
    };
});
```

## API Reference

### Session

- `OpenAsync(CancellationToken ct = default)` - Opens a session with default configuration
- `OpenAsync(string? configJson, CancellationToken ct = default)` - Opens a session with JSON configuration
- `OpenAsync(SessionConfig config, CancellationToken ct = default)` - Opens a session with typed configuration
- `DeclarePublisherAsync(string keyExpr, CancellationToken ct = default)` - Creates a publisher
- `DeclarePublisherAsync(string keyExpr, PublisherOptions options, CancellationToken ct = default)` - Creates a publisher with options
- `DeclareSubscriberAsync(string keyExpr, Action<Sample> callback, CancellationToken ct = default)` - Creates a subscriber
- `DeclareQueryableAsync(string keyExpr, Action<Query> callback, CancellationToken ct = default)` - Creates a queryable
- `DeclareQuerierAsync(string keyExpr, CancellationToken ct = default)` - Creates a querier
- `GetAsync(string selector, Action<Sample> callback, CancellationToken ct = default)` - Performs a query
- `GetAsync(string selector, Action<Sample> callback, TimeSpan timeout, CancellationToken ct = default)` - Query with timeout
- `PutAsync(string keyExpr, byte[] data, CancellationToken ct = default)` - Direct put without publisher
- `PutWithAttachmentAsync(string keyExpr, byte[] data, IDictionary<string, string> attachment, CancellationToken ct = default)` - Put with attachment
- `DeleteAsync(string keyExpr, CancellationToken ct = default)` - Delete a key expression
- `DeclareLivelinessTokenAsync(string keyExpr, CancellationToken ct = default)` - Declare a liveliness token
- `DeclareLivelinessSubscriberAsync(string keyExpr, Action<string, bool> callback, CancellationToken ct = default)` - Subscribe to liveliness changes
- `GetZenohIdAsync(CancellationToken ct = default)` - Gets the session's Zenoh ID

### Publisher

- `PutAsync(byte[] data, CancellationToken ct = default)` - Publishes binary data
- `PutAsync(string value, CancellationToken ct = default)` - Publishes a UTF-8 string
- `PutAsync<T>(T value, CancellationToken ct = default)` - Publishes a value (uses ToString)
- `KeyExpression` - Gets the key expression

### Subscriber

- Automatically receives data via callback
- `KeyExpression` - Gets the key expression

### Sample (record type)

- `KeyExpression` - The key expression
- `Payload` - Raw byte array payload
- `Kind` - Sample kind (Put or Delete)
- `Encoding` - Payload encoding
- `Timestamp` - Optional timestamp
- `GetPayloadAsString()` - Converts payload to UTF-8 string
- `TryGetPayloadAsJson<T>()` - Tries to deserialize as JSON (returns null on failure)
- `GetPayloadAsJson<T>()` - Deserializes as JSON (throws on failure)

### SessionConfig

Strongly-typed configuration:

```csharp
var config = new SessionConfig
{
    Mode = SessionMode.Peer,  // or Client, Router
    Connect = new ConnectConfig
    {
        Endpoints = ["tcp/192.168.1.100:7447"]
    },
    Listen = new ListenConfig
    {
        Endpoints = ["tcp/0.0.0.0:7448"]
    }
};
```

## Comparison with ZenohDotNet.Native

| Feature | ZenohDotNet.Native | ZenohDotNet.Client |
|---------|--------------|--------------|
| Target | .NET Standard 2.1 | .NET 8.0+ |
| API Style | Synchronous | Async/Await |
| CancellationToken | ❌ | ✅ |
| Resource Cleanup | IDisposable | IAsyncDisposable |
| JSON Support | Manual | Built-in |
| Typed Config | ❌ | ✅ SessionConfig |
| Query Timeout | ❌ | ✅ |
| Modern C# | Limited | Full (records, pattern matching, etc.) |
| Use Case | Low-level, Unity | Modern .NET apps |

## Performance Tips

- Reuse Publisher and Subscriber instances instead of creating new ones
- Use `PutAsync(byte[])` for binary data to avoid encoding overhead
- Consider using `ValueTask` patterns for high-throughput scenarios
- Leverage `Span<T>` when working with payload data

## License

This project is licensed under the MIT License.
