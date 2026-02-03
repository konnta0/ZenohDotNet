#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

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
            var status = new System.Text.StringBuilder();
            status.AppendLine("Native Library Status:\n");

            // Check in package location
            var packagePath = GetPackagePath();
            var checks = new[]
            {
                ("Windows x64", $"{packagePath}/Plugins/Windows/x86_64/zenoh_ffi.dll"),
                ("Linux x64", $"{packagePath}/Plugins/Linux/x86_64/libzenoh_ffi.so"),
                ("macOS", $"{packagePath}/Plugins/macOS/libzenoh_ffi.dylib"),
            };

            int found = 0;
            foreach (var (name, path) in checks)
            {
                bool exists = File.Exists(path);
                if (exists) found++;
                status.AppendLine($"{name}: {(exists ? "✓ Found" : "✗ Missing")}");
            }

            status.AppendLine();
            if (found == 0)
            {
                status.AppendLine("No native libraries found.\n");
                status.AppendLine("If installed via UPM, native libraries should be included.");
                status.AppendLine("For development, build with: cargo build --release");
            }
            else
            {
                status.AppendLine($"{found} of {checks.Length} desktop platforms available.");
            }

            EditorUtility.DisplayDialog("Zenoh Native Libraries", status.ToString(), "OK");
        }

        private static string GetPackagePath()
        {
            // Try to find the package path
            var guids = AssetDatabase.FindAssets("t:asmdef ZenohDotNet.Unity.Runtime");
            if (guids.Length > 0)
            {
                var asmdefPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                return Path.GetDirectoryName(Path.GetDirectoryName(asmdefPath));
            }
            return "Packages/com.zenohdotnet.unity";
        }
    }
}
#endif
