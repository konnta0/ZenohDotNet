using UnityEngine;
using ZenohDotNet.Unity;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

/// <summary>
/// Example Unity component that performs queries using Zenoh
/// </summary>
public class ZenohQueryExample : MonoBehaviour
{
    [Header("Zenoh Configuration")]
    [SerializeField] private string endpoint = "tcp/localhost:7447";
    [SerializeField] private string selector = "unity/demo/**";
    [SerializeField] private float queryInterval = 1.0f; // Query every 1 second

    [Header("Status")]
    [SerializeField] private bool isConnected = false;
    [SerializeField] private int queryCount = 0;
    [SerializeField] private int responseCount = 0;

    [Header("Last Response")]
    [SerializeField] private string lastKey = "";
    [SerializeField] private string lastPayload = "";

    private Session session;
    private List<string> recentResponses = new List<string>();
    private const int MaxRecentResponses = 10;

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

            // Start query loop
            QueryLoop(this.GetCancellationTokenOnDestroy()).Forget();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Zenoh] Failed to initialize: {ex.Message}");
            isConnected = false;
        }
    }

    private async UniTaskVoid QueryLoop(System.Threading.CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                queryCount++;
                Debug.Log($"[Zenoh] Sending query #{queryCount}: {selector}");

                await session.GetAsync(selector, sample =>
                {
                    responseCount++;
                    lastKey = sample.KeyExpression;
                    lastPayload = sample.GetPayloadAsString();

                    var responseText = $"[{sample.KeyExpression}] {lastPayload}";
                    Debug.Log($"[Zenoh] Query response: {responseText}");

                    // Keep recent responses for display
                    recentResponses.Add(responseText);
                    if (recentResponses.Count > MaxRecentResponses)
                    {
                        recentResponses.RemoveAt(0);
                    }
                }, cancellationToken);

                await UniTask.Delay(TimeSpan.FromSeconds(queryInterval), cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Zenoh] Query error: {ex.Message}");
                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);
            }
        }
    }

    void OnDestroy()
    {
        Debug.Log("[Zenoh] Cleaning up...");
        session?.Dispose();
        isConnected = false;
        Debug.Log("[Zenoh] Cleanup complete.");
    }

    // Display status in scene view
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 220, 400, 300));
        GUILayout.Label("Zenoh Query Status:");
        GUILayout.Label($"Connected: {isConnected}");
        GUILayout.Label($"Selector: {selector}");
        GUILayout.Label($"Queries Sent: {queryCount}");
        GUILayout.Label($"Responses Received: {responseCount}");
        
        GUILayout.Space(10);
        GUILayout.Label("Recent Responses:");
        foreach (var response in recentResponses)
        {
            GUILayout.Label($"  {response}");
        }
        GUILayout.EndArea();
    }
}
