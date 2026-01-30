using ZenohDotNet.Client;

Console.WriteLine("Zenoh Publisher Example");
Console.WriteLine("=======================");
Console.WriteLine();

// Open a Zenoh session
Console.WriteLine("Opening Zenoh session...");
await using var session = await Session.OpenAsync();
Console.WriteLine("Session opened successfully!");

// Declare a publisher
var keyExpr = "demo/example/zenoh-csharp";
Console.WriteLine($"Declaring publisher on '{keyExpr}'...");
await using var publisher = await session.DeclarePublisherAsync(keyExpr);
Console.WriteLine("Publisher declared!");

// Publish messages in a loop
Console.WriteLine();
Console.WriteLine("Publishing messages (Ctrl+C to stop)...");
Console.WriteLine();

var count = 0;
while (true)
{
    var message = $"Hello Zenoh! #{count}";
    await publisher.PutAsync(message);

    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Published: {message}");

    count++;
    await Task.Delay(1000);
}
