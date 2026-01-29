# Zenoh .NET Samples

This directory contains sample applications demonstrating how to use Zenoh.Client in .NET applications.

## Prerequisites

- .NET 8.0 SDK or later
- Zenoh native library built and available

## Building the Samples

From the repository root:

```bash
# Build the native library first
./scripts/build-native.sh
./scripts/copy-bindings.sh

# Build the samples
dotnet build samples/dotnet/Publisher/Publisher.csproj
dotnet build samples/dotnet/Subscriber/Subscriber.csproj
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

## Testing Communication

To test the pub/sub communication:

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

You should see messages appear in the Subscriber terminal as the Publisher sends them.

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

## Troubleshooting

### DllNotFoundException: zenoh_ffi

Make sure the native library has been built:

```bash
./scripts/build-native.sh
./scripts/copy-bindings.sh
```

The native libraries should be in `src/Zenoh.Native/runtimes/{rid}/native/`.

### No messages received

- Make sure both Publisher and Subscriber are running
- Check that they're using compatible key expressions
- Verify that Zenoh discovery is working (both applications should be in peer mode by default)

## Additional Resources

- [Zenoh Documentation](https://zenoh.io/docs/)
- [Zenoh.Client API Reference](../../src/Zenoh.Client/README.md)
- [Zenoh.Native API Reference](../../src/Zenoh.Native/README.md)
