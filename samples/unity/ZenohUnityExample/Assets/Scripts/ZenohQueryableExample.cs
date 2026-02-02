using UnityEngine;
using ZenohDotNet.Unity;
using Cysharp.Threading.Tasks;
using System;

/// <summary>
/// Example Unity component that responds to queries using Zenoh Queryable
/// </summary>
public class ZenohQueryableExample : MonoBehaviour
{
    [Header("Zenoh Configuration")]
    [SerializeField] private string endpoint = "tcp/localhost:7447";
    [SerializeField] private string keyExpression = "unity/demo/status";

    [Header("Status")]
    [SerializeField] private bool isConnected = false;
    [SerializeField] private int queriesReceived = 0;

    [Header("Response Data")]
    [SerializeField] private string statusMessage = "Unity Zenoh Queryable is running!";

    private Session session;
    private Queryable queryable;

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

            Debug.Log($"[Zenoh] Declaring queryable on '{keyExpression}'...");
            queryable = await session.DeclareQueryableAsync(keyExpression, OnQuery, this.GetCancellationTokenOnDestroy());
            Debug.Log("[Zenoh] Queryable declared! Waiting for queries...");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Zenoh] Failed to initialize: {ex.Message}");
            isConnected = false;
        }
    }

    private void OnQuery(Query query)
    {
        queriesReceived++;
        Debug.Log($"[Zenoh] Received query #{queriesReceived} on '{query.Selector}'");

        try
        {
            var jsonResponse = JsonUtility.ToJson(new ResponseData
            {
                status = "ok",
                message = statusMessage,
                timestamp = DateTime.UtcNow.ToString("o"),
                posX = transform.position.x,
                posY = transform.position.y,
                posZ = transform.position.z,
                queriesReceived = queriesReceived
            });

            query.Reply(keyExpression, jsonResponse);
            Debug.Log($"[Zenoh] Replied with: {jsonResponse}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Zenoh] Failed to reply: {ex.Message}");
        }
    }

    void OnDestroy()
    {
        Debug.Log("[Zenoh] Cleaning up...");
        queryable?.Dispose();
        session?.Dispose();
        isConnected = false;
        Debug.Log("[Zenoh] Cleanup complete.");
    }

    // Display status in scene view
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(320, 10, 300, 150));
        GUILayout.Label("Zenoh Queryable Status:");
        GUILayout.Label($"Connected: {isConnected}");
        GUILayout.Label($"Key: {keyExpression}");
        GUILayout.Label($"Queries Received: {queriesReceived}");
        GUILayout.Label($"Status Message: {statusMessage}");
        GUILayout.EndArea();
    }

    [Serializable]
    private class ResponseData
    {
        public string status;
        public string message;
        public string timestamp;
        public float posX;
        public float posY;
        public float posZ;
        public int queriesReceived;
    }
}
