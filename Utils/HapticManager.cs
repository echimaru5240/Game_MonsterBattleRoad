using UnityEngine;

public static class HapticManager
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject vibrator;

    static HapticManager()
    {
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
        }
    }
#endif

    public static void Vibrate(long milliseconds = 50)
    {
        Debug.Log("Vibrate");
#if UNITY_ANDROID && !UNITY_EDITOR
        if (vibrator != null)
        {
            vibrator.Call("vibrate", milliseconds);
        }
#elif UNITY_IOS && !UNITY_EDITOR
        // iOSは基本的にプラグインが必要（Taptic Engine/CoreHaptics）
        Handheld.Vibrate();
#endif
    }
}
