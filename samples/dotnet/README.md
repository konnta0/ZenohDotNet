# Zenoh .NET Samples

This directory contains sample applications demonstrating how to use ZenohDotNet.Client in .NET applications.

## Samples

| Sample | Description |
|--------|-------------|
| [Publisher](Publisher/) | Publishes messages to a key expression |
| [Subscriber](Subscriber/) | Subscribes to messages with wildcard matching |
| [LivelinessToken](LivelinessToken/) | Declares a liveliness token (resource presence) |
| [LivelinessSubscriber](LivelinessSubscriber/) | Monitors liveliness changes |

## Prerequisites

- .NET 8.0 SDK or later
- Zenoh native library built and available

## Building the Samples

From the repository root:

```bash
# Build the native library first
./scripts/build-native.sh
./scripts/copy-bindings.sh

# Build all samples
dotnet build samples/dotnet/Publisher/Publisher.csproj
dotnet build samples/dotnet/Subscriber/Subscriber.csproj
dotnet build samples/dotnet/LivelinessToken/LivelinessToken.csproj
dotnet build samples/dotnet/LivelinessSubscriber/LivelinessSubscriber.csproj
```

## Running the Samples

### Publisher

The publisher sends messages on the `demo/example/zenoh-csharp` key expression:

```bash
cd samples/dotnet/Publisher
dotnet run
```

Output:
```
Zenoh Publisher Example
=======================

Opening Zenoh session...
Session opened successfully!
Declaring publisher on 'demo/example/zenoh-csharp'...
Publisher declared!

Publishing messages (Ctrl+C to stop)...

[14:30:15] Published: Hello Zenoh! #0
[14:30:16] Published: Hello Zenoh! #1
[14:30:17] Published: Hello Zenoh! #2
...
```

### Subscriber

The subscriber receives messages on the `demo/example/**` key expression (wildcard):

```bash
cd samples/dotnet/Subscriber
dotnet run
```

Output:
```
Zenoh Subscriber Example
========================

Opening Zenoh session...
Session opened successfully!
Declaring subscriber on 'demo/example/**'...
Subscriber declared!

Listening for messages (Ctrl+C to stop)...

[14:30:15] Received on 'demo/example/zenoh-csharp': Hello Zenoh! #0
[14:30:16] Received on 'demo/example/zenoh-csharp': Hello Zenoh! #1
[14:30:17] Received on 'demo/example/zenoh-csharp': Hello Zenoh! #2
...
```

### LivelinessToken

Declares a liveliness token to signal resource availability:

```bash
cd samples/dotnet/LivelinessToken
dotnet run
```

Output:
```
Zenoh Liveliness Token Example
==============================

Opening Zenoh session...
Session opened successfully!
Zenoh ID: xxxx

Declaring liveliness token on 'my/liveliness/token'...
Token declared! Resource is now alive.

Press any key to undeclare the token and exit...
```

### LivelinessSubscriber

Monitors liveliness changes on a key expression:

```bash
cd samples/dotnet/LivelinessSubscriber
dotnet run
```

Output:
```
Zenoh Liveliness Subscriber Example
===================================

Opening Zenoh session...
Session opened successfully!
Zenoh ID: xxxx

Subscribing to liveliness on 'my/**'...
Subscriber declared!

Listening for liveliness changes (Ctrl+C to stop)...

[14:30:15] my/liveliness/token is ALIVE
[14:30:20] my/liveliness/token is DEAD
...
```

## Testing Communication

### Pub/Sub

1. Open two terminal windows
2. In the first terminal, run the **Subscriber**:
   ```bash
   cd samples/dotnet/Subscriber
   dotnet run
   ```
3. In the second terminal, run the **Publisher**:
   ```bash
   cd samples/dotnet/Publisher
   dotnet run
   ```

### Liveliness

1. Open two terminal windows
2. In the first terminal, run the **LivelinessSubscriber**:
   ```bash
   cd samples/dotnet/LivelinessSubscriber
   dotnet run
   ```
3. In the second terminal, run the **LivelinessToken**:
   ```bash
   cd samples/dotnet/LivelinessToken
   dotnet run
   ```
4. Press any key in the LivelinessToken terminal to see the token become "dead"

## Key Concepts

### Publisher

```csharp
// Open a session
await using var session = await Session.OpenAsync();

// Declare a publisher
await using var publisher = await session.DeclarePublisherAsync("demo/example/test");

// Publish data
await publisher.PutAsync("Hello, Zenoh!");
await publisher.PutAsync(new byte[] { 1, 2, 3 });
```

### Subscriber

```csharp
// Open a session
await using var session = await Session.OpenAsync();

// Declare a subscriber with a callback
await using var subscriber = await session.DeclareSubscriberAsync("demo/example/**", sample =>
{
    Console.WriteLine($"Received: {sample.GetPayloadAsString()}");
});

// Keep application running to receive messages
await Task.Delay(-1);
```

### Liveliness Token

```csharp
// Open a session
await using var session = await Session.OpenAsync();

// Declare a liveliness token
await using var token = await session.DeclareLivelinessTokenAsync("my/resource");

// Token is alive while it exists, dead when disposed
```

### Liveliness Subscriber

```csharp
// Subscribe to liveliness changes
await using var sub = await session.DeclareLivelinessSubscriberAsync("my/**", (keyExpr, isAlive) =>
{
    Console.WriteLine($"{keyExpr} is {(isAlive ? "ALIVE" : "DEAD")}");
});
```

## Configuration

Zenoh sessions can be configured with JSON:

```csharp
var config = @"{
    ""mode"": ""peer"",
    ""connect"": {
        ""endpoints"": [""tcp/localhost:7447""]
    }
}";

await using var session = await Session.OpenAsync(config);
```

Or with strongly-typed configuration:

```csharp
var config = new SessionConfig
{
    Mode = SessionMode.Peer,
    Connect = new ConnectConfig
    {
        Endpoints = ["tcp/localhost:7447"]
    }
};

await using var session = await Session.OpenAsync(config);
```

## Troubleshooting

### DllNotFoundException: zenoh_ffi

Make sure the native library has been built:

```bash
./scripts/build-native.sh
./scripts/copy-bindings.sh
```

The native libraries should be in `src/ZenohDotNet.Native/runtimes/{rid}/native/`.

### No messages received

- Make sure both Publisher and Subscriber are running
- Check that they're using compatible key expressions
- Verify that Zenoh discovery is working (both applications should be in peer mode by default)

## Additional Resources

- [Zenoh Documentation](https://zenoh.io/docs/)
- [ZenohDotNet.Client API Reference](../../src/ZenohDotNet.Client/README.md)
- [ZenohDotNet.Native API Reference](../../src/ZenohDotNet.Native/README.md)
