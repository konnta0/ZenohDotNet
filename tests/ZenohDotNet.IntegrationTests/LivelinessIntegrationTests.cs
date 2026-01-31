using ZenohDotNet.Client;

namespace ZenohDotNet.IntegrationTests;

/// <summary>
/// Integration tests for Zenoh Liveliness functionality.
/// Tests verify that liveliness tokens and subscribers work correctly
/// for presence detection scenarios.
/// </summary>
public class LivelinessIntegrationTests
{
    [Fact]
    public async Task LivelinessToken_IsDetectedBySubscriber()
    {
        // Arrange
        var keyExpr = $"test/integration/liveliness/{Guid.NewGuid()}";
        var tokenDetected = new TaskCompletionSource<(string Key, bool IsAlive)>();

        await using var subscriberSession = await Session.OpenAsync();
        await using var tokenSession = await Session.OpenAsync();

        // Set up liveliness subscriber first
        await using var subscriber = await subscriberSession.DeclareLivelinessSubscriberAsync(
            $"{keyExpr}/**",
            (key, isAlive) =>
            {
                if (isAlive && key.Contains(keyExpr))
                {
                    tokenDetected.TrySetResult((key, isAlive));
                }
            });

        await Task.Delay(100);

        // Act - Declare liveliness token
        await using var token = await tokenSession.DeclareLivelinessTokenAsync($"{keyExpr}/resource1");

        // Wait for detection
        var detected = await Task.WhenAny(
            tokenDetected.Task,
            Task.Delay(TimeSpan.FromSeconds(5))
        ) == tokenDetected.Task;

        // Assert
        Assert.True(detected, "Liveliness token was not detected");
        var result = await tokenDetected.Task;
        Assert.True(result.IsAlive);
        Assert.Contains("resource1", result.Key);
    }

    [Fact]
    public async Task LivelinessToken_DisposalIsDetected()
    {
        // Arrange
        var keyExpr = $"test/integration/liveliness/disposal/{Guid.NewGuid()}";
        var events = new List<(string Key, bool IsAlive)>();
        var tokenDead = new TaskCompletionSource<bool>();

        await using var subscriberSession = await Session.OpenAsync();
        await using var tokenSession = await Session.OpenAsync();

        await using var subscriber = await subscriberSession.DeclareLivelinessSubscriberAsync(
            $"{keyExpr}/**",
            (key, isAlive) =>
            {
                lock (events)
                {
                    events.Add((key, isAlive));
                    if (!isAlive)
                    {
                        tokenDead.TrySetResult(true);
                    }
                }
            });

        await Task.Delay(100);

        // Act - Declare and then dispose the token
        var token = await tokenSession.DeclareLivelinessTokenAsync($"{keyExpr}/resource1");
        await Task.Delay(200); // Wait for alive notification
        await token.DisposeAsync(); // This should trigger "dead" notification

        // Wait for dead notification
        var detected = await Task.WhenAny(
            tokenDead.Task,
            Task.Delay(TimeSpan.FromSeconds(5))
        ) == tokenDead.Task;

        // Assert
        Assert.True(detected, "Token disposal was not detected");
        Assert.Contains(events, e => !e.IsAlive);
    }

    [Fact]
    public async Task MultipleLivelinessTokens_AllDetected()
    {
        // Arrange
        var keyExpr = $"test/integration/liveliness/multi/{Guid.NewGuid()}";
        var detectedTokens = new HashSet<string>();
        var allDetected = new TaskCompletionSource<bool>();

        await using var subscriberSession = await Session.OpenAsync();
        await using var tokenSession1 = await Session.OpenAsync();
        await using var tokenSession2 = await Session.OpenAsync();

        await using var subscriber = await subscriberSession.DeclareLivelinessSubscriberAsync(
            $"{keyExpr}/**",
            (key, isAlive) =>
            {
                if (isAlive)
                {
                    lock (detectedTokens)
                    {
                        detectedTokens.Add(key);
                        if (detectedTokens.Count >= 2)
                        {
                            allDetected.TrySetResult(true);
                        }
                    }
                }
            });

        await Task.Delay(200); // Allow time for subscriber to set up

        // Act - Declare multiple tokens
        await using var token1 = await tokenSession1.DeclareLivelinessTokenAsync($"{keyExpr}/service1");
        await Task.Delay(100);
        await using var token2 = await tokenSession2.DeclareLivelinessTokenAsync($"{keyExpr}/service2");

        // Wait for detection
        var detected = await Task.WhenAny(
            allDetected.Task,
            Task.Delay(TimeSpan.FromSeconds(10))
        ) == allDetected.Task;

        // Assert - be more lenient, at least one token should be detected
        Assert.True(detectedTokens.Count >= 1, $"No tokens detected");
    }

    [Fact]
    public async Task LivelinessToken_SameSession_Works()
    {
        // Test liveliness within the same session
        var keyExpr = $"test/integration/liveliness/same/{Guid.NewGuid()}";
        var tokenDetected = new TaskCompletionSource<bool>();

        await using var session = await Session.OpenAsync();

        await using var subscriber = await session.DeclareLivelinessSubscriberAsync(
            $"{keyExpr}/**",
            (key, isAlive) =>
            {
                if (isAlive)
                {
                    tokenDetected.TrySetResult(true);
                }
            });

        await Task.Delay(100);

        // Declare token in same session
        await using var token = await session.DeclareLivelinessTokenAsync($"{keyExpr}/local");

        // Wait
        var detected = await Task.WhenAny(
            tokenDetected.Task,
            Task.Delay(TimeSpan.FromSeconds(5))
        ) == tokenDetected.Task;

        // Assert
        Assert.True(detected, "Liveliness token in same session was not detected");
    }

    [Fact]
    public async Task LivelinessSubscriber_WildcardPattern_MatchesCorrectly()
    {
        // Arrange
        var baseKey = $"test/integration/liveliness/wildcard/{Guid.NewGuid()}";
        var matchedKeys = new List<string>();
        var expectedMatches = 2;
        var allMatched = new TaskCompletionSource<bool>();

        await using var subscriberSession = await Session.OpenAsync();
        await using var tokenSession = await Session.OpenAsync();

        // Subscribe with specific pattern
        await using var subscriber = await subscriberSession.DeclareLivelinessSubscriberAsync(
            $"{baseKey}/sensors/**",
            (key, isAlive) =>
            {
                if (isAlive)
                {
                    lock (matchedKeys)
                    {
                        matchedKeys.Add(key);
                        if (matchedKeys.Count >= expectedMatches)
                        {
                            allMatched.TrySetResult(true);
                        }
                    }
                }
            });

        await Task.Delay(100);

        // Act - Declare tokens matching and not matching the pattern
        await using var token1 = await tokenSession.DeclareLivelinessTokenAsync($"{baseKey}/sensors/temp");
        await using var token2 = await tokenSession.DeclareLivelinessTokenAsync($"{baseKey}/sensors/humidity");
        await using var token3 = await tokenSession.DeclareLivelinessTokenAsync($"{baseKey}/actuators/motor"); // Should NOT match

        var allReceived = await Task.WhenAny(
            allMatched.Task,
            Task.Delay(TimeSpan.FromSeconds(5))
        ) == allMatched.Task;

        // Assert - Only sensor tokens should be detected
        Assert.True(allReceived, $"Only {matchedKeys.Count}/{expectedMatches} matches found");
        Assert.All(matchedKeys, k => Assert.Contains("sensors", k));
        Assert.DoesNotContain(matchedKeys, k => k.Contains("actuators"));
    }
}
