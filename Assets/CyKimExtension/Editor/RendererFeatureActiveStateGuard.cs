using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// URP 렌더러 피처의 켜짐 상태를 디스크 값으로 되돌린다.
/// Play 중 스크립트가 <c>ScriptableRendererFeature.SetActive</c>로 피처를 끄고 켜면, 이 값은 에셋을 메모리에서만 바꾸고 dirty도 남기지 않는다.
/// Play 종료 복구를 놓치면 꺼진 채 남고, 그대로 빌드하면 URP가 그 피처의 리소스를 뺀다(SSAO가 꺼진 채 빌드돼 기기에서 파이프라인 생성이 매 프레임 실패하고 화면이 깜빡인 적이 있다).
/// Edit 모드 진입과 플레이어 빌드 직전에 dirty가 아닌 피처만 디스크 값으로 맞춘다. 인스펙터에서 바꾸고 저장하지 않은 값은 dirty라 건드리지 않는다.
/// 렌더러 에셋을 텍스트(YAML)로 읽으므로 Asset Serialization이 Force Text여야 한다. 아니면 아무것도 바꾸지 않는다.
/// </summary>
[InitializeOnLoad]
public class RendererFeatureActiveStateGuard : IPreprocessBuildWithReport
{
    private const string BLOCK_HEADER = "--- !u!";
    private const string ACTIVE_KEY = "m_Active:";

    // URP 빌드 전처리가 렌더러 피처를 읽기 전에 돌아야 한다.
    public int callbackOrder => int.MinValue;

    static RendererFeatureActiveStateGuard()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        RestoreFromDisk();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            RestoreFromDisk();
        }
    }

    /// <summary>
    /// 품질 단계·기본 파이프라인이 쓰는 렌더러의 피처 켜짐 상태를 디스크 값으로 맞추고, 바꾼 개수를 돌려준다.
    /// </summary>
    public static int RestoreFromDisk()
    {
        var restoredCount = 0;
        var visited = new HashSet<ScriptableRendererData>();

        foreach (var pipelineAsset in CollectPipelineAssets())
        {
            foreach (var rendererData in pipelineAsset.rendererDataList)
            {
                if (rendererData == null || !visited.Add(rendererData))
                {
                    continue;
                }

                restoredCount += RestoreRenderer(rendererData);
            }
        }

        return restoredCount;
    }

    private static IEnumerable<UniversalRenderPipelineAsset> CollectPipelineAssets()
    {
        if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset defaultAsset)
        {
            yield return defaultAsset;
        }

        for (var i = 0; i < QualitySettings.count; i++)
        {
            if (QualitySettings.GetRenderPipelineAssetAt(i) is UniversalRenderPipelineAsset qualityAsset)
            {
                yield return qualityAsset;
            }
        }
    }

    private static int RestoreRenderer(ScriptableRendererData rendererData)
    {
        var path = AssetDatabase.GetAssetPath(rendererData);
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return 0;
        }

        var diskActiveStates = ReadActiveStates(File.ReadAllText(path));
        var restoredCount = 0;

        foreach (var feature in rendererData.rendererFeatures)
        {
            if (feature == null || EditorUtility.IsDirty(feature))
            {
                continue;
            }

            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId) ||
                !diskActiveStates.TryGetValue(localId, out var diskActive) ||
                feature.isActive == diskActive)
            {
                continue;
            }

            feature.SetActive(diskActive);
            restoredCount++;
            Debug.LogWarning($"[RendererFeatureActiveStateGuard] {path}의 '{feature.name}' 켜짐 상태를 디스크 값({diskActive})으로 되돌렸습니다.");
        }

        return restoredCount;
    }

    // ForceText 에셋을 '--- !u!<classId> &<localId>' 블록으로 나눠 블록마다 m_Active 값을 읽는다.
    private static Dictionary<long, bool> ReadActiveStates(string text)
    {
        var states = new Dictionary<long, bool>();

        foreach (var block in text.Split(new[] { BLOCK_HEADER }, System.StringSplitOptions.RemoveEmptyEntries))
        {
            var idStart = block.IndexOf('&');
            var lineEnd = block.IndexOf('\n');
            if (idStart < 0 || lineEnd < idStart)
            {
                continue;
            }

            var idText = block.Substring(idStart + 1, lineEnd - idStart - 1).Trim();
            var spaceIndex = idText.IndexOf(' ');
            if (spaceIndex >= 0)
            {
                idText = idText.Substring(0, spaceIndex);
            }

            var activeIndex = block.IndexOf(ACTIVE_KEY, System.StringComparison.Ordinal);
            if (activeIndex < 0 || !long.TryParse(idText, out var localId))
            {
                continue;
            }

            var valueStart = activeIndex + ACTIVE_KEY.Length;
            var valueEnd = block.IndexOf('\n', valueStart);
            var valueText = (valueEnd < 0 ? block.Substring(valueStart) : block.Substring(valueStart, valueEnd - valueStart)).Trim();
            states[localId] = valueText == "1";
        }

        return states;
    }
}
