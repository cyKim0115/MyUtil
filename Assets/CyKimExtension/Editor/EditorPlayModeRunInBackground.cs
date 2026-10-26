using UnityEditor;
using UnityEngine;

/// <summary>
/// 에디터 Play Mode에서만 Application.runInBackground를 강제 ON. 포커스가 없어도 시뮬레이션이 계속 돌아가게 한다.
/// 에디터에서는 이 값이 Player Settings의 Run In Background와 같은 칸이라, 켠 채 두면 ProjectSettings.asset에 저장돼 빌드까지 따라간다.
/// 그래서 Play에 들어가기 전 값을 SessionState에 적어 두고 Edit 모드로 돌아오면 그 값으로 되돌린다.
/// </summary>
[InitializeOnLoad]
public static class EditorPlayModeRunInBackground
{
    private const string SAVED_KEY = "EditorPlayModeRunInBackground.Saved";
    private const string ORIGINAL_KEY = "EditorPlayModeRunInBackground.Original";

    static EditorPlayModeRunInBackground()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        ApplyIfPlaying();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        switch (state)
        {
            // 도메인 리로드 전에 적어 둔다. 리로드 뒤 정적 생성자가 먼저 켜 버리기 때문이다.
            case PlayModeStateChange.ExitingEditMode:
                SaveOriginal();
                break;
            case PlayModeStateChange.EnteredPlayMode:
                Application.runInBackground = true;
                break;
            case PlayModeStateChange.EnteredEditMode:
                RestoreOriginal();
                break;
        }
    }

    private static void ApplyIfPlaying()
    {
        if (EditorApplication.isPlaying)
        {
            Application.runInBackground = true;
        }
    }

    private static void SaveOriginal()
    {
        if (SessionState.GetBool(SAVED_KEY, false))
        {
            return;
        }

        SessionState.SetBool(ORIGINAL_KEY, Application.runInBackground);
        SessionState.SetBool(SAVED_KEY, true);
    }

    private static void RestoreOriginal()
    {
        if (!SessionState.GetBool(SAVED_KEY, false))
        {
            return;
        }

        Application.runInBackground = SessionState.GetBool(ORIGINAL_KEY, false);
        SessionState.EraseBool(SAVED_KEY);
        SessionState.EraseBool(ORIGINAL_KEY);
    }
}
