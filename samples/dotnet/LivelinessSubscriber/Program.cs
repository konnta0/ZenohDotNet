using ZenohDotNet.Client;

Console.WriteLine("Zenoh Liveliness Subscriber Example");
Console.WriteLine("===================================");
Console.WriteLine();

// Open a Zenoh session
Console.WriteLine("Opening Zenoh session...");
await using var session = await Session.OpenAsync();
Console.WriteLine("Session opened successfully!");

// Declare a liveliness subscriber
var keyExpr = "group1/**";
Console.WriteLine($"Subscribing to liveliness changes on '{keyExpr}'...");

await using var subscriber = await session.DeclareLivelinessSubscriberAsync(keyExpr, (tokenKeyExpr, isAlive) =>
{
    var status = isAlive ? "ALIVE" : "DEAD";
    var symbol = isAlive ? "✓" : "✗";
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {symbol} '{tokenKeyExpr}' is now {status}");
});

Console.WriteLine("Liveliness subscriber declared!");
Console.WriteLine();
Console.WriteLine("Waiting for liveliness changes...");
Console.WriteLine("Run the LivelinessToken example in another terminal to see changes.");
Console.WriteLine("Press Ctrl+C to exit...");
Console.WriteLine();

// Keep the subscriber active
await Task.Delay(Timeout.Infinite);
