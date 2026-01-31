using ZenohDotNet.Client;

Console.WriteLine("Zenoh Liveliness Token Example");
Console.WriteLine("==============================");
Console.WriteLine();

// Open a Zenoh session
Console.WriteLine("Opening Zenoh session...");
await using var session = await Session.OpenAsync();
Console.WriteLine("Session opened successfully!");

// Declare a liveliness token
var keyExpr = "group1/zenoh-csharp";
Console.WriteLine($"Declaring liveliness token for '{keyExpr}'...");
await using var token = await session.DeclareLivelinessTokenAsync(keyExpr);
Console.WriteLine("Liveliness token declared!");
Console.WriteLine();
Console.WriteLine("This token signals that this resource is alive.");
Console.WriteLine("Other Zenoh nodes can subscribe to liveliness changes");
Console.WriteLine("to detect when this resource appears or disappears.");
Console.WriteLine();
Console.WriteLine("Press Ctrl+C to undeclare the token and exit...");

// Keep the token alive
await Task.Delay(Timeout.Infinite);
