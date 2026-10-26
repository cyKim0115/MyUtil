using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 에디터에서 머티리얼의 셰이더 키워드가 빠지는 문제를 바로잡는다. 셰이더가 한 번이라도 그 키워드 없이 다시 만들어지면
/// (병합·브랜치 전환으로 셰이더 그래프가 재임포트되는 도중 등) 이미 불려 있던 머티리얼은 파일에 <c>_ALPHATEST_ON</c> 등을
/// 그대로 가진 채 켜진 키워드(<see cref="Material.enabledKeywords"/>)만 비고, 정상 셰이더로 돌아와도 되살아나지 않는다.
/// 그러면 알파 클립을 쓰는 카드·아이콘 둘레가 검은 사각형으로 나오고 투명·스페큘러 설정도 꺼진다. 파일과 빌드는 정상이다.
/// 셰이더 관련 파일을 임포트한 뒤와 Play에 들어가기 직전에, 불려 있는 Assets 머티리얼 중 그런 것만 디스크에서 다시 불러온다
/// (파일은 바뀌지 않는다).
/// </summary>
[InitializeOnLoad]
public sealed class MaterialKeywordRepair : AssetPostprocessor
{
    private static readonly string[] ShaderSourceExtensions = { ".shadergraph", ".shadersubgraph", ".shader", ".hlsl", ".cginc" };

    static MaterialKeywordRepair()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    /// <summary>불려 있는 머티리얼 중 키워드가 빠진 것을 다시 불러오고 그 수를 돌려준다.</summary>
    public static int Repair()
    {
        var paths = new List<string>();
        foreach (var material in Resources.FindObjectsOfTypeAll<Material>())
        {
            if (!HasMissingKeyword(material))
                continue;

            var path = AssetDatabase.GetAssetPath(material);
            if (!path.StartsWith("Assets/") || !path.EndsWith(".mat"))
                continue;
            if (EditorUtility.IsDirty(material))
            {
                Debug.LogWarning($"[MaterialKeywordRepair] 저장하지 않은 변경이 있어 다시 불러오지 않습니다: {path}");
                continue;
            }

            paths.Add(path);
        }

        foreach (var path in paths)
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        if (paths.Count > 0)
            Debug.Log($"[MaterialKeywordRepair] 셰이더 키워드가 빠진 머티리얼 {paths.Count}개를 다시 불러왔습니다: {string.Join(", ", paths)}");
        return paths.Count;
    }

    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        foreach (var path in importedAssets)
        {
            if (!IsShaderSource(path))
                continue;

            EditorApplication.delayCall += RepairWhenIdle;
            return;
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
            Repair();
    }

    // 임포트·컴파일이 다 끝난 뒤 본다(셰이더 그래프는 연달아 재임포트될 수 있다).
    private static void RepairWhenIdle()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += RepairWhenIdle;
            return;
        }

        Repair();
    }

    // 파일에 켜 둔 키워드가 지금 셰이더에 있는데 켜져 있지 않은가.
    private static bool HasMissingKeyword(Material material)
    {
        var shader = material.shader;
        if (shader == null)
            return false;

        var space = shader.keywordSpace;
        foreach (var name in material.shaderKeywords)
        {
            var keyword = space.FindKeyword(name);
            if (keyword.isValid && !material.IsKeywordEnabled(keyword))
                return true;
        }

        return false;
    }

    private static bool IsShaderSource(string path)
    {
        foreach (var extension in ShaderSourceExtensions)
        {
            if (path.EndsWith(extension))
                return true;
        }

        return false;
    }
}
