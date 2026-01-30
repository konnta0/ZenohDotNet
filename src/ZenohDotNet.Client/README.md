# ZenohDotNet.Client

High-level async Zenoh client library for .NET 8.0+ with modern C# features.

## Overview

ZenohDotNet.Client provides a modern, idiomatic .NET API for Zenoh, built on top of ZenohDotNet.Native. It leverages .NET 8.0 features like async/await, IAsyncDisposable, record types, and more for a superior developer experience.

## Features

- **Async/Await**: Full async API using Task-based patterns
- **Modern C#**: Utilizes .NET 8.0 features (records, pattern matching, nullable reference types)
- **Memory Efficient**: Leverages Span<T> and modern .NET optimizations
- **JSON Support**: Built-in System.Text.Json integration
- **Type Safe**: Strongly-typed APIs with compile-time safety
- **IAsyncDisposable**: Proper async resource cleanup

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

- `OpenAsync()` - Opens a session with default configuration
- `OpenAsync(string? configJson)` - Opens a session with JSON configuration
- `DeclarePublisherAsync(string keyExpr)` - Creates a publisher
- `DeclareSubscriberAsync(string keyExpr, Action<Sample> callback)` - Creates a subscriber

### Publisher

- `PutAsync(byte[] data)` - Publishes binary data
- `PutAsync(string value)` - Publishes a UTF-8 string
- `PutAsync<T>(T value)` - Publishes a value (uses ToString)
- `KeyExpression` - Gets the key expression

### Subscriber

- Automatically receives data via callback
- `KeyExpression` - Gets the key expression

### Sample (record type)

- `KeyExpression` - The key expression
- `Payload` - Raw byte array payload
- `GetPayloadAsString()` - Converts payload to UTF-8 string
- `TryGetPayloadAsJson<T>()` - Tries to deserialize as JSON (returns null on failure)
- `GetPayloadAsJson<T>()` - Deserializes as JSON (throws on failure)

## Comparison with ZenohDotNet.Native

| Feature | ZenohDotNet.Native | ZenohDotNet.Client |
|---------|--------------|--------------|
| Target | .NET Standard 2.1 | .NET 8.0+ |
| API Style | Synchronous | Async/Await |
| Resource Cleanup | IDisposable | IAsyncDisposable |
| JSON Support | Manual | Built-in |
| Modern C# | Limited | Full (records, pattern matching, etc.) |
| Use Case | Low-level, Unity | Modern .NET apps |

## Performance Tips

- Reuse Publisher and Subscriber instances instead of creating new ones
- Use `PutAsync(byte[])` for binary data to avoid encoding overhead
- Consider using `ValueTask` patterns for high-throughput scenarios
- Leverage `Span<T>` when working with payload data

## License

This project is licensed under the MIT License.
