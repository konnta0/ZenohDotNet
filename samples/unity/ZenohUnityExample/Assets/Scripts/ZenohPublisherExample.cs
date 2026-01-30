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
    [SerializeField] private string keyExpression = "unity/demo/position";
    [SerializeField] private float publishInterval = 0.1f; // Publish every 100ms

    [Header("Status")]
    [SerializeField] private bool isConnected = false;
    [SerializeField] private int messageCount = 0;

    private Session session;
    private Publisher publisher;

    async void Start()
    {
        try
        {
            Debug.Log("[Zenoh] Opening session...");
            session = await Session.OpenAsync(this.GetCancellationTokenOnDestroy());
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
                // Publish transform position as JSON
                var position = transform.position;
                var data = $"{{\"x\":{position.x},\"y\":{position.y},\"z\":{position.z},\"count\":{messageCount}}}";

                publisher.Put(data);
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
