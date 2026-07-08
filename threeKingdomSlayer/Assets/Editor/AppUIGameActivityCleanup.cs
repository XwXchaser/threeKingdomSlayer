#if UNITY_ANDROID && !UNITY_2023_2_OR_NEWER
using UnityEditor.Android;
using System.IO;
using UnityEngine;

public class AppUIGameActivityCleanup : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 1;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        var gameActivityPath = Path.Combine(path, "src/main/java/com/unity3d/player/appui/AppUIGameActivity.java");
        if (File.Exists(gameActivityPath))
        {
            File.Delete(gameActivityPath);
            Debug.Log($"[AppUIGameActivityCleanup] Deleted incompatible AppUIGameActivity.java (requires Unity 2023.2+)");
        }
    }
}
#endif
