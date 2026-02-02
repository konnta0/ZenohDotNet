using UnityEngine;
using ZenohDotNet.Unity;
using Cysharp.Threading.Tasks;
using System;

/// <summary>
/// Example Unity component that publishes data using Zenoh
/// </summary>
public class ZenohPublisherExample : MonoBehaviour
{
    [Header("Zenoh Configuration")]
    [SerializeField] private string endpoint = "tcp/localhost:7447";
    [SerializeField] private string keyExpression = "unity/demo/position";
    [SerializeField] private float publishInterval = 0.016f; // Publish at ~60fps

    [Header("Status")]
    [SerializeField] private bool isConnected = false;
    [SerializeField] private int messageCount = 0;

    private Session session;
    private Publisher publisher;

    async void Start()
    {
        try
        {
            Debug.Log($"[Zenoh] Opening session to {endpoint}...");
            var config = new ZenohDotNet.Native.SessionConfig()
                .WithMode(ZenohDotNet.Native.SessionMode.Client)
                .WithConnect(endpoint);
            session = await Session.OpenAsync(config, this.GetCancellationTokenOnDestroy());
            isConnected = true;
            Debug.Log("[Zenoh] Session opened successfully!");

            Debug.Log($"[Zenoh] Declaring publisher on '{keyExpression}'...");
            publisher = await session.DeclarePublisherAsync(keyExpression, this.GetCancellationTokenOnDestroy());
            Debug.Log("[Zenoh] Publisher declared!");

            // Start publishing loop
            PublishLoop(this.GetCancellationTokenOnDestroy()).Forget();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Zenoh] Failed to initialize: {ex.Message}");
            isConnected = false;
        }
    }

    private async UniTaskVoid PublishLoop(System.Threading.CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Generate moving position using sine waves for demo
                var time = Time.time;
                var x = Mathf.Sin(time) * 5f;
                var y = Mathf.Sin(time * 0.7f) * 3f;
                var z = Mathf.Cos(time * 1.3f) * 4f;
                var data = $"{{\"x\":{x:F3},\"y\":{y:F3},\"z\":{z:F3},\"count\":{messageCount}}}";

                await publisher.PutAsync(data, cancellationToken);
                messageCount++;

                await UniTask.Delay(TimeSpan.FromSeconds(publishInterval), cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Zenoh] Publish error: {ex.Message}");
                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);
            }
        }
    }

    void OnDestroy()
    {
        Debug.Log("[Zenoh] Cleaning up...");
        publisher?.Dispose();
        session?.Dispose();
        isConnected = false;
        Debug.Log("[Zenoh] Cleanup complete.");
    }

    // Display status in scene view
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 100));
        GUILayout.Label($"Zenoh Publisher Status:");
        GUILayout.Label($"Connected: {isConnected}");
        GUILayout.Label($"Messages Sent: {messageCount}");
        GUILayout.Label($"Key: {keyExpression}");
        GUILayout.EndArea();
    }
}
