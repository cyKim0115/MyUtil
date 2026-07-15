using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class GameStateUtil
{
    private static bool isExiting = false;

    public static bool IsSafeToAccess => Application.isPlaying && !isExiting;

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        // 기존 이벤트 핸들러 제거 후 등록
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            isExiting = true;
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            isExiting = false;
        }
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RuntimeInitialize()
    {
        // 빌드 환경에서 초기화 및 이벤트 등록
        Application.quitting -= OnApplicationQuitting;
        Application.quitting += OnApplicationQuitting;
    }

    private static void OnApplicationQuitting()
    {
        isExiting = true;

        // 이벤트 정리
        Application.quitting -= OnApplicationQuitting;
    }
}
