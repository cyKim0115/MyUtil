using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 열린 씬·Prefab Stage의 dirty 상태를 Temp/AgentSceneDirty.json에 적어 둔다.
/// 씬 리로드 같은 네이티브 모달이 메인 스레드를 막으면 MCP로 상태를 물을 수 없어서,
/// 바깥 도구(unity-modal.py)가 이 파일로 Reload를 눌러도 되는지 판단한다.
/// Temp/는 에디터를 닫으면 지워지고, 파일에 pid가 있어 다른 에디터의 것과 섞이지 않는다.
/// 이 스크립트의 경로도 함께 적는다. 브랜치 전환으로 스크립트가 빠지면 에디터는 더 적지 않지만 파일은 남으므로,
/// 바깥 도구는 그 경로에 스크립트가 없으면 기록을 믿지 않는다. 도메인 리로드 중에는 씬 dirty 상태가 그대로라
/// 리로드 직전 기록이 유효하다(리로드 중 뜬 모달도 처리할 수 있게 지우지 않는다).
/// </summary>
[InitializeOnLoad]
public static class AgentSceneDirtyBeacon
{
    private const string FILE_PATH = "Temp/AgentSceneDirty.json";
    private const double POLL_INTERVAL_SECONDS = 1.0;

    private static readonly int _pid = System.Diagnostics.Process.GetCurrentProcess().Id;
    private static readonly string _scriptPath = GetScriptPath();
    private static double _nextPollTime;
    private static string _lastJson;

    static AgentSceneDirtyBeacon()
    {
        // 에셋 임포트 워커도 에디터 스크립트를 로드한다. 워커가 같은 파일을 자기 pid로 덮어쓰면
        // 메인 에디터는 상태가 바뀔 때까지 다시 적지 않으므로, 모달이 뜨는 메인 에디터에서만 돈다.
        if (AssetDatabase.IsAssetImportWorkerProcess() || Application.isBatchMode)
            return;

        EditorApplication.update += Poll;
        // dirty가 되는 순간은 폴링을 기다리지 않고 바로 적는다. 편집 직후 refresh로 모달이 뜨면
        // 1초 폴링으로는 「저장됨」이 남아 Reload가 그 편집을 버릴 수 있다.
        EditorSceneManager.sceneDirtied += _ => Write();
        PrefabStage.prefabStageDirtied += _ => Write();
        EditorApplication.playModeStateChanged += _ => Write();
        Write();
    }

    private static void Poll()
    {
        if (EditorApplication.timeSinceStartup < _nextPollTime)
            return;
        _nextPollTime = EditorApplication.timeSinceStartup + POLL_INTERVAL_SECONDS;
        Write();
    }

    private static void Write()
    {
        var json = JsonUtility.ToJson(Capture());
        if (json == _lastJson)
            return;

        try
        {
            File.WriteAllText(FILE_PATH, json);
            _lastJson = json;
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
        {
            // 옛 「저장됨」이 남지 않게 지운다. 파일이 없으면 바깥 도구는 상태 모름으로 보고 누르지 않는다.
            Debug.LogWarning($"[AgentSceneDirtyBeacon] {e.Message}");
            Delete();
        }
    }

    private static void Delete()
    {
        _lastJson = null;
        try
        {
            File.Delete(FILE_PATH);
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
        {
            Debug.LogWarning($"[AgentSceneDirtyBeacon] {e.Message}");
        }
    }

    private static string GetScriptPath([CallerFilePath] string path = "") => path;

    private static Snapshot Capture()
    {
        var snapshot = new Snapshot
        {
            Pid = _pid,
            ScriptPath = _scriptPath,
            IsPlaying = EditorApplication.isPlayingOrWillChangePlaymode,
        };

        for (var i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.IsValid())
                continue;
            snapshot.Scenes.Add(new SceneState { Path = scene.path, IsDirty = scene.isDirty });
        }

        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null)
        {
            snapshot.PrefabStagePath = stage.assetPath;
            snapshot.PrefabStageDirty = stage.scene.isDirty;
        }

        return snapshot;
    }

    [Serializable]
    private class Snapshot
    {
        public int Pid;
        public string ScriptPath;
        public bool IsPlaying;
        public List<SceneState> Scenes = new();
        public string PrefabStagePath;
        public bool PrefabStageDirty;
    }

    [Serializable]
    private class SceneState
    {
        public string Path;
        public bool IsDirty;
    }
}
