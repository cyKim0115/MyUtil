using UnityEditor;
using UnityEngine;

/// <summary>
/// 에디터 Play Mode에서만 Application.runInBackground를 강제 ON.
/// Player Settings(빌드) 값과 무관하며, 포커스가 없어도 시뮬레이션이 계속 돌아가게 한다.
/// </summary>
[InitializeOnLoad]
public static class EditorPlayModeRunInBackground
{
    static EditorPlayModeRunInBackground()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        ApplyIfPlaying();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Application.runInBackground = true;
        }
    }

    private static void ApplyIfPlaying()
    {
        if (EditorApplication.isPlaying)
        {
            Application.runInBackground = true;
        }
    }
}
