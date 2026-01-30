using ZenohDotNet.Client;

Console.WriteLine("Zenoh Subscriber Example");
Console.WriteLine("========================");
Console.WriteLine();

// Open a Zenoh session
Console.WriteLine("Opening Zenoh session...");
await using var session = await Session.OpenAsync();
Console.WriteLine("Session opened successfully!");

// Declare a subscriber
var keyExpr = "demo/example/**";
Console.WriteLine($"Declaring subscriber on '{keyExpr}'...");

await using var subscriber = await session.DeclareSubscriberAsync(keyExpr, sample =>
{
    var timestamp = DateTime.Now.ToString("HH:mm:ss");
    var payload = sample.GetPayloadAsString();
    Console.WriteLine($"[{timestamp}] Received on '{sample.KeyExpression}': {payload}");
});

Console.WriteLine("Subscriber declared!");
Console.WriteLine();
Console.WriteLine("Listening for messages (Ctrl+C to stop)...");
Console.WriteLine();

// Keep the application running
await Task.Delay(-1);
