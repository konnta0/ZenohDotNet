#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ZenohDotNet.Unity.Editor
{
    /// <summary>
    /// Unity Editor extensions for Zenoh.
    /// </summary>
    public static class ZenohUnityEditor
    {
        [MenuItem("Tools/Zenoh/About")]
        private static void ShowAbout()
        {
            EditorUtility.DisplayDialog(
                "Zenoh for Unity",
                "Zenoh distributed messaging system integration for Unity.\n\n" +
                "Version: 0.1.0\n" +
                "Website: https://zenoh.io\n\n" +
                "Features:\n" +
                "- Pub/Sub messaging\n" +
                "- Distributed queries\n" +
                "- UniTask integration\n" +
                "- Cross-platform support",
                "OK"
            );
        }

        [MenuItem("Tools/Zenoh/Documentation")]
        private static void OpenDocumentation()
        {
            Application.OpenURL("https://github.com/konnta0/ZenohDotNet");
        }

        [MenuItem("Tools/Zenoh/Check Native Libraries")]
        private static void CheckNativeLibraries()
        {
            bool hasWindows = System.IO.File.Exists("Assets/Plugins/x86_64/zenoh_ffi.dll");
            bool hasLinux = System.IO.File.Exists("Assets/Plugins/x86_64/libzenoh_ffi.so");
            bool hasMacOS = System.IO.File.Exists("Assets/Plugins/x86_64/libzenoh_ffi.bundle");

            string message = "Native Library Status:\n\n";
            message += $"Windows (x64): {(hasWindows ? "✓ Found" : "✗ Missing")}\n";
            message += $"Linux (x64): {(hasLinux ? "✓ Found" : "✗ Missing")}\n";
            message += $"macOS (x64): {(hasMacOS ? "✓ Found" : "✗ Missing")}\n\n";

            if (!hasWindows && !hasLinux && !hasMacOS)
            {
                message += "No native libraries found. Please install ZenohDotNet.Native via NuGet for Unity.";
            }

            EditorUtility.DisplayDialog("Zenoh Native Libraries", message, "OK");
        }
    }
}
#endif
