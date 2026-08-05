using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// MCP 작업 전 dirty 씬/프리팹 Stage를 정리해 Save·Discard·Reload 모달 팝업을 예방한다.
/// Agent는 MCP execute_code로 <see cref="PrepareSave"/> / <see cref="PrepareDiscard"/> 를 호출한다.
/// </summary>
public static class AgentEditorDialogGuard
{
    private const string MenuPathSave = "Tools/Agent/Editor/Prepare Save Dirty";
    private const string MenuPathDiscard = "Tools/Agent/Editor/Prepare Discard Dirty";

    private static MethodInfo _clearSceneDirtiness;

    /// <summary>
    /// Dirty 씬·프리팹 Stage를 저장한 뒤 Stage를 닫는다. 씬 로드/프리팹 전환 직전에 호출.
    /// </summary>
    public static string PrepareSave()
    {
        var summary = Prepare(saveChanges: true);
        Debug.Log($"[AgentEditorDialogGuard] {summary}");
        return summary;
    }

    [MenuItem(MenuPathSave)]
    private static void PrepareSaveMenu()
    {
        PrepareSave();
    }

    [MenuItem(MenuPathSave, true)]
    private static bool ValidatePrepareSaveMenu()
    {
        // Agent 전용. 사용자 Tools 메뉴에서는 비활성.
        return false;
    }

    /// <summary>
    /// Dirty 씬·프리팹 Stage 변경을 폐기한 뒤 Stage를 닫는다.
    /// 의도치 않은 dirty만 버릴 때 사용. 의도한 편집은 <see cref="PrepareSave"/> 를 쓴다.
    /// </summary>
    public static string PrepareDiscard()
    {
        var summary = Prepare(saveChanges: false);
        Debug.Log($"[AgentEditorDialogGuard] {summary}");
        return summary;
    }

    [MenuItem(MenuPathDiscard)]
    private static void PrepareDiscardMenu()
    {
        PrepareDiscard();
    }

    [MenuItem(MenuPathDiscard, true)]
    private static bool ValidatePrepareDiscardMenu()
    {
        // Agent 전용. 사용자 Tools 메뉴에서는 비활성.
        return false;
    }

    /// <summary>
    /// Dirty 상태를 저장 또는 폐기한다. execute_code에서 요약을 받으려면 이 메서드나
    /// <see cref="PrepareSave"/> / <see cref="PrepareDiscard"/> 의 반환값을 return 한다.
    /// </summary>
    public static string Prepare(bool saveChanges = true)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return "skipped: editor is in play mode";

        var sb = new StringBuilder();
        sb.Append(saveChanges ? "mode=save" : "mode=discard");

        HandlePrefabStage(saveChanges, sb);
        HandleScenes(saveChanges, sb);

        return sb.ToString();
    }

    private static void HandlePrefabStage(bool saveChanges, StringBuilder sb)
    {
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage == null)
        {
            sb.Append("; prefabStage=none");
            return;
        }

        var path = stage.assetPath;
        var wasDirty = stage.scene.isDirty;

        if (saveChanges && wasDirty)
        {
            PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot, path, out var success);
            if (!success)
            {
                sb.Append($"; prefabStageSaveFailed={path}");
                Debug.LogError($"[AgentEditorDialogGuard] Failed to save prefab stage: {path}");
                return;
            }

            AssetDatabase.SaveAssets();
            sb.Append($"; prefabStageSaved={path}");
        }
        else if (!saveChanges && wasDirty)
        {
            sb.Append($"; prefabStageDiscarded={path}");
        }
        else
        {
            sb.Append($"; prefabStageClean={path}");
        }

        if (stage.scene.isDirty)
            stage.ClearDirtiness();

        StageUtility.GoToMainStage();
        sb.Append("; prefabStageClosed");
    }

    private static void HandleScenes(bool saveChanges, StringBuilder sb)
    {
        var dirtyScenes = new List<Scene>();
        for (var i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.IsValid() && scene.isLoaded && scene.isDirty)
                dirtyScenes.Add(scene);
        }

        if (dirtyScenes.Count == 0)
        {
            sb.Append("; scenes=clean");
            return;
        }

        if (saveChanges)
        {
            if (!EditorSceneManager.SaveOpenScenes())
            {
                sb.Append($"; scenesSaveFailed={dirtyScenes.Count}");
                Debug.LogError("[AgentEditorDialogGuard] SaveOpenScenes failed.");
                return;
            }

            sb.Append($"; scenesSaved={dirtyScenes.Count}");
            return;
        }

        // Unity 6: ClearSceneDirtiness는 public이 아니라 internal이라 리플렉션으로만 호출 가능.
        EnsureClearSceneDirtinessMethod();
        if (_clearSceneDirtiness == null)
        {
            Debug.LogWarning(
                "[AgentEditorDialogGuard] ClearSceneDirtiness unavailable; saving scenes instead of discarding.");
            EditorSceneManager.SaveOpenScenes();
            sb.Append($"; scenesDiscardFallbackSaved={dirtyScenes.Count}");
            return;
        }

        foreach (var scene in dirtyScenes)
        {
            var label = string.IsNullOrEmpty(scene.path) ? scene.name : scene.path;
            _clearSceneDirtiness.Invoke(null, new object[] { scene });
            sb.Append($"; sceneDiscarded={label}");
        }
    }

    private static void EnsureClearSceneDirtinessMethod()
    {
        if (_clearSceneDirtiness != null)
            return;

        _clearSceneDirtiness = typeof(EditorSceneManager).GetMethod(
            "ClearSceneDirtiness",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
            binder: null,
            types: new[] { typeof(Scene) },
            modifiers: null);
    }
}
