# Zenoh.Unity

Unity-optimized Zenoh client library with UniTask integration for Unity 2021.2+.

## Overview

Zenoh.Unity provides a Unity-friendly API for Zenoh distributed messaging, with full UniTask integration for async operations and automatic main thread marshalling for callbacks.

## Features

- **UniTask Integration**: Modern async/await patterns with UniTask
- **Main Thread Safety**: Callbacks automatically execute on Unity main thread
- **Synchronous API**: Optional sync methods for use in Update loops
- **Unity 2021.2+**: Compatible with Unity's latest C# support
- **Cross-Platform**: Supports Windows, Linux, macOS, Android, iOS
- **Editor Extensions**: Helpful Unity Editor tools

## Installation

### Via UPM (Unity Package Manager)

1. Open Package Manager in Unity
2. Click "+" → "Add package from git URL"
3. Enter: `https://github.com/konnta0/ZenohForCSharp-internal.git?path=/packages/upm/com.zenoh.unity`

### Via NuGet for Unity (Recommended)

1. Install [NuGet for Unity](https://github.com/GlitchEnzo/NuGetForUnity)
2. Open NuGet → Manage NuGet Packages
3. Search and install `Zenoh.Native`
4. Search and install `UniTask`
5. Add the UPM package as described above

## Requirements

- Unity 2021.3 or later
- UniTask 2.5.4+
- Zenoh.Native (automatically included)

## Usage

### Basic Pub/Sub Example

```csharp
using UnityEngine;
using Zenoh.Unity;
using Cysharp.Threading.Tasks;

public class ZenohExample : MonoBehaviour
{
    private Session session;
    private Publisher publisher;
    private Subscriber subscriber;

    async void Start()
    {
        // Open session
        session = await Session.OpenAsync(this.GetCancellationTokenOnDestroy());

        // Create publisher
        publisher = await session.DeclarePublisherAsync(
            "unity/example/data",
            this.GetCancellationTokenOnDestroy());

        // Create subscriber (callback runs on main thread!)
        subscriber = await session.DeclareSubscriberAsync(
            "unity/example/**",
            OnDataReceived,
            this.GetCancellationTokenOnDestroy());

        Debug.Log("Zenoh initialized");
    }

    void Update()
    {
        // Synchronous put for use in Update
        if (Input.GetKeyDown(KeyCode.Space))
        {
            publisher.Put($"Hello from Unity at {Time.time}");
        }
    }

    private void OnDataReceived(Sample sample)
    {
        // This runs on Unity main thread - safe to call Unity APIs
        Debug.Log($"Received: {sample.GetPayloadAsString()}");
    }

    void OnDestroy()
    {
        subscriber?.Dispose();
        publisher?.Dispose();
        session?.Dispose();
    }
}
```

### Async Publishing

```csharp
async UniTask PublishSensorData()
{
    var publisher = await session.DeclarePublisherAsync("sensors/temperature");

    while (!destroyCancellationToken.IsCancellationRequested)
    {
        float temp = GetTemperature();
        await publisher.PutAsync($"{{\"value\": {temp}}}");
        await UniTask.Delay(1000, cancellationToken: destroyCancellationToken);
    }
}
```

### Pattern Matching Subscribers

```csharp
subscriber = await session.DeclareSubscriberAsync("game/**", sample =>
{
    // This runs on main thread
    switch (sample.KeyExpression)
    {
        case "game/player/position":
            UpdatePlayerPosition(sample);
            break;
        case "game/enemy/spawn":
            SpawnEnemy(sample);
            break;
        default:
            Debug.Log($"Unhandled: {sample.KeyExpression}");
            break;
    }
});
```

### Error Handling

```csharp
try
{
    session = await Session.OpenAsync();
    publisher = await session.DeclarePublisherAsync("test/key");
    await publisher.PutAsync("test data");
}
catch (Zenoh.Native.ZenohException ex)
{
    Debug.LogError($"Zenoh error: {ex.Message}");
}
catch (OperationCanceledException)
{
    Debug.Log("Operation cancelled");
}
```

## API Reference

### Session

- `OpenAsync(CancellationToken)` - Opens a session
- `OpenAsync(string? configJson, CancellationToken)` - Opens with config
- `DeclarePublisherAsync(string keyExpr, CancellationToken)` - Creates publisher
- `DeclareSubscriberAsync(string keyExpr, Action<Sample>, CancellationToken)` - Creates subscriber

### Publisher

- `PutAsync(byte[] data, CancellationToken)` - Async publish bytes
- `PutAsync(string value, CancellationToken)` - Async publish string
- `Put(byte[] data)` - Sync publish bytes (for Update loops)
- `Put(string value)` - Sync publish string (for Update loops)
- `KeyExpression` - Gets the key expression

### Subscriber

- Callbacks execute on Unity main thread automatically
- `KeyExpression` - Gets the key expression

### Sample

- `KeyExpression` - The key expression
- `Payload` - Raw byte array payload
- `GetPayloadAsString()` - Converts to UTF-8 string
- `TryGetPayloadAsString(out string)` - Safe string conversion

## Unity Editor Tools

Access via **Tools → Zenoh** menu:

- **About**: Show package information
- **Documentation**: Open online docs
- **Check Native Libraries**: Verify native library installation

## Platform-Specific Notes

### Windows
- Native library: `zenoh_ffi.dll`
- Should be in `Assets/Plugins/x86_64/`

### macOS
- Native library: `libzenoh_ffi.bundle` or `.dylib`
- Should be in `Assets/Plugins/x86_64/`

### Linux
- Native library: `libzenoh_ffi.so`
- Should be in `Assets/Plugins/x86_64/`

### Android
- Native library: `libzenoh_ffi.so`
- Should be in `Assets/Plugins/Android/libs/arm64-v8a/`

### iOS
- Native library: `__Internal` (statically linked)
- Requires IL2CPP backend

## Performance Tips

- Use synchronous `Put()` in Update loops instead of `PutAsync()`
- Reuse Publisher and Subscriber instances
- Cancel long-running operations with CancellationToken
- Use `GetCancellationTokenOnDestroy()` for automatic cleanup

## Troubleshooting

### "DllNotFoundException: zenoh_ffi"

Make sure Zenoh.Native is installed via NuGet for Unity. Check Tools → Zenoh → Check Native Libraries.

### Callbacks not executing

Callbacks automatically run on main thread. If you see threading issues, ensure you're using the Unity build (not pure .NET).

### UniTask not found

Install UniTask via NuGet for Unity or UPM.

## License

This project is licensed under the MIT License.
