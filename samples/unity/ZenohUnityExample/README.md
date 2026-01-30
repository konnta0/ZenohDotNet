# Zenoh Unity Example Project

This is a sample Unity project demonstrating how to use ZenohDotNet.Unity for distributed messaging in Unity applications.

## Prerequisites

- Unity 2021.3 LTS or later
- NuGet for Unity package
- UniTask package

## Setup Instructions

### 1. Install NuGet for Unity

1. Download NuGet for Unity from [GitHub](https://github.com/GlitchEnzo/NuGetForUnity)
2. Import the `.unitypackage` into your project
3. Or install via Package Manager with Git URL:
   ```
   https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity
   ```

### 2. Install ZenohDotNet.Native via NuGet for Unity

1. Open Unity
2. Go to **NuGet → Manage NuGet Packages**
3. Search for `ZenohDotNet.Native`
4. Click **Install**

### 3. Install UniTask

UniTask is already specified in `Packages/manifest.json`. Unity will automatically install it from OpenUPM.

Alternatively, install manually:
1. Go to **Window → Package Manager**
2. Click **+** → **Add package from git URL**
3. Enter: `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`

### 4. Install ZenohDotNet.Unity

Add ZenohDotNet.Unity via UPM:

1. Copy the `com.zenohdotnet.unity` package from `packages/upm/` to your project's `Packages/` directory
2. Or add via Package Manager:
   - **Add package from disk** → Select `packages/upm/com.zenohdotnet.unity/package.json`
   - **Add package from git URL** → Enter your repository URL with path to UPM package

## Example Scripts

### ZenohPublisherExample

Publishes the GameObject's position data to Zenoh.

**Usage:**
1. Create a new GameObject in your scene
2. Attach the `ZenohPublisherExample` script
3. Configure the key expression (default: `unity/demo/position`)
4. Press Play

The script will:
- Open a Zenoh session
- Publish position data as JSON every 100ms
- Display connection status in the Game view

**Inspector Settings:**
- `Key Expression`: The Zenoh key to publish on
- `Publish Interval`: How often to publish (in seconds)

### ZenohSubscriberExample

Subscribes to data from Zenoh and optionally updates a target GameObject's position.

**Usage:**
1. Create a new GameObject in your scene
2. Attach the `ZenohSubscriberExample` script
3. Configure the key expression (default: `unity/demo/**`)
4. Optionally assign a target GameObject to visualize received positions
5. Press Play

The script will:
- Open a Zenoh session
- Subscribe to matching keys
- Display received messages in the Game view
- Update target object position if enabled

**Inspector Settings:**
- `Key Expression`: The Zenoh key pattern to subscribe to (supports wildcards)
- `Target Object`: GameObject to update with received position data
- `Update Position`: Enable/disable position updates

## Testing Pub/Sub Communication

### Test within Unity

1. Create two GameObjects:
   - One with `ZenohPublisherExample`
   - One with `ZenohSubscriberExample` (assign a target object)
2. Make sure the subscriber's key expression matches the publisher's key
3. Press Play
4. Move the publisher GameObject - the subscriber's target should follow

### Test with .NET Application

Run the Publisher alongside the Unity Subscriber:

```bash
# In a terminal
cd samples/dotnet/Publisher
dotnet run
```

In Unity, run the ZenohSubscriberExample with key expression `demo/example/**`.

### Test with Unity Application

Run the Subscriber alongside the Unity Publisher:

```bash
# In a terminal
cd samples/dotnet/Subscriber
dotnet run
```

In Unity, run the ZenohPublisherExample with key expression `demo/example/zenoh-csharp`.

## Project Structure

```
ZenohUnityExample/
├── Assets/
│   ├── Scenes/
│   │   └── SampleScene.unity
│   └── Scripts/
│       ├── ZenohPublisherExample.cs
│       └── ZenohSubscriberExample.cs
├── Packages/
│   └── manifest.json
└── README.md
```

## Troubleshooting

### "DllNotFoundException: zenoh_ffi"

Make sure ZenohDotNet.Native is properly installed:
1. Check **NuGet → Manage NuGet Packages**
2. Verify `ZenohDotNet.Native` is listed
3. Check `Assets/Packages/` for the native libraries

Use Unity's **Tools → Zenoh → Check Native Libraries** menu to verify installation.

### UniTask not found

Install UniTask via OpenUPM or GitHub:
- OpenUPM: Add the scoped registry in `Packages/manifest.json`
- GitHub: Use Package Manager with Git URL

### Callbacks not executing

Make sure you're using UniTask correctly:
- Use `GetCancellationTokenOnDestroy()` for proper cancellation
- Callbacks run on Unity's main thread automatically

## Performance Tips

1. **Adjust Publish Interval**: Higher intervals (e.g., 0.5s) reduce CPU usage
2. **Use Selective Subscriptions**: Use specific key expressions instead of wildcards
3. **Dispose Properly**: Always dispose sessions in `OnDestroy()`
4. **Batch Updates**: Consider batching multiple values into single messages

## Next Steps

- Explore Query/Queryable for request-response patterns
- Implement custom serialization for complex data types
- Add error recovery and reconnection logic
- Create multiplayer game mechanics with Zenoh

## Additional Resources

- [Zenoh Documentation](https://zenoh.io/docs/)
- [ZenohDotNet.Unity API Reference](../../../src/ZenohDotNet.Unity/Assets/Plugins/com.zenohdotnet.unity/README.md)
- [UniTask Documentation](https://github.com/Cysharp/UniTask)
- [NuGet for Unity](https://github.com/GlitchEnzo/NuGetForUnity)
