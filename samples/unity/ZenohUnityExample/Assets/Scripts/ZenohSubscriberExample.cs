using UnityEngine;
using ZenohDotNet.Unity;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

/// <summary>
/// Example Unity component that subscribes to data using Zenoh
/// </summary>
public class ZenohSubscriberExample : MonoBehaviour
{
    [Header("Zenoh Configuration")]
    [SerializeField] private string endpoint = "tcp/localhost:7447";
    [SerializeField] private string keyExpression = "unity/demo/**";

    [Header("Status")]
    [SerializeField] private bool isConnected = false;
    [SerializeField] private int messagesReceived = 0;
    [SerializeField] private string lastMessage = "";

    [Header("Visualization")]
    [SerializeField] private GameObject targetObject;
    [SerializeField] private bool updatePosition = true;

    private Session session;
    private Subscriber subscriber;
    private Queue<string> messageQueue = new Queue<string>();
    private const int MaxDisplayMessages = 5;

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

            Debug.Log($"[Zenoh] Declaring subscriber on '{keyExpression}'...");
            subscriber = await session.DeclareSubscriberAsync(
                keyExpression,
                OnSampleReceived,
                this.GetCancellationTokenOnDestroy()
            );
            Debug.Log("[Zenoh] Subscriber declared!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Zenoh] Failed to initialize: {ex.Message}");
            isConnected = false;
        }
    }

    private void OnSampleReceived(Sample sample)
    {
        try
        {
            string payload = sample.GetPayloadAsString();
            lastMessage = payload;
            messagesReceived++;

            // Add to message queue for display
            messageQueue.Enqueue($"[{sample.KeyExpression}]: {payload}");
            if (messageQueue.Count > MaxDisplayMessages)
            {
                messageQueue.Dequeue();
            }

            Debug.Log($"[Zenoh] Received on '{sample.KeyExpression}': {payload}");

            // Try to parse as position JSON and update target object
            if (updatePosition && targetObject != null)
            {
                TryUpdatePosition(payload);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Zenoh] Error processing sample: {ex.Message}");
        }
    }

    private void TryUpdatePosition(string json)
    {
        try
        {
            // Simple JSON parsing for position
            // In production, use JsonUtility or Newtonsoft.Json
            if (json.Contains("\"x\":") && json.Contains("\"y\":") && json.Contains("\"z\":"))
            {
                var xStart = json.IndexOf("\"x\":") + 4;
                var yStart = json.IndexOf("\"y\":") + 4;
                var zStart = json.IndexOf("\"z\":") + 4;

                var xEnd = json.IndexOf(",", xStart);
                var yEnd = json.IndexOf(",", yStart);
                var zEnd = json.IndexOf(",", zStart);
                if (zEnd < 0) zEnd = json.IndexOf("}", zStart);

                var x = float.Parse(json.Substring(xStart, xEnd - xStart));
                var y = float.Parse(json.Substring(yStart, yEnd - yStart));
                var z = float.Parse(json.Substring(zStart, zEnd - zStart));

                targetObject.transform.position = new Vector3(x, y, z);
            }
        }
        catch
        {
            // Ignore parsing errors
        }
    }

    void OnDestroy()
    {
        Debug.Log("[Zenoh] Cleaning up...");
        subscriber?.Dispose();
        session?.Dispose();
        isConnected = false;
        Debug.Log("[Zenoh] Cleanup complete.");
    }

    // Display status and messages in scene view
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 400, 250));

        GUILayout.Label($"Zenoh Subscriber Status:");
        GUILayout.Label($"Connected: {isConnected}");
        GUILayout.Label($"Messages Received: {messagesReceived}");
        GUILayout.Label($"Key: {keyExpression}");

        GUILayout.Space(10);
        GUILayout.Label("Recent Messages:");

        foreach (var msg in messageQueue)
        {
            GUILayout.Label(msg);
        }

        GUILayout.EndArea();
    }
}
