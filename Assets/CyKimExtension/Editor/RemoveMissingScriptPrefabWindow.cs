using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

public class RemoveMissingScriptPrefabWindow : EditorWindow
{
    private readonly List<GameObject> _prefabs = new List<GameObject>();
    private VisualElement _prefabListContainer;
    private ScrollView _scrollView;
    private Button _applyButton;

    [MenuItem("Tools/Prefab/Remove Missing Scripts")]
    public static void ShowWindow()
    {
        var window = GetWindow<RemoveMissingScriptPrefabWindow>("Remove Missing Scripts");
        window.titleContent = new GUIContent("Remove Missing Scripts", EditorGUIUtility.IconContent("d_Prefab Icon").image);
        window.minSize = new Vector2(360f, 280f);
    }

    private void CreateGUI()
    {
        var root = rootVisualElement;
        root.style.paddingTop = root.style.paddingBottom = root.style.paddingLeft = root.style.paddingRight = 10;

        var toolbar = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                marginBottom = 8
            }
        };

        var clearButton = new Button(ClearPrefabList)
        {
            text = "리스트 클리어",
            style = { flexGrow = 1, marginRight = 4 }
        };
        toolbar.Add(clearButton);

        _applyButton = new Button(ApplyRemoveMissingScripts)
        {
            text = "적용",
            style = { flexGrow = 1 }
        };
        toolbar.Add(_applyButton);
        root.Add(toolbar);

        var dropArea = new VisualElement
        {
            style =
            {
                height = 50,
                backgroundColor = new Color(0.2f, 0.2f, 0.2f),
                justifyContent = Justify.Center,
                marginBottom = 8
            }
        };
        dropArea.Add(new Label("프리팹을 여기에 드래그 앤 드롭하세요")
        {
            style = { alignSelf = Align.Center, color = Color.white }
        });
        root.Add(dropArea);

        _scrollView = new ScrollView(ScrollViewMode.Vertical)
        {
            style = { flexGrow = 1 }
        };
        _prefabListContainer = new VisualElement();
        _scrollView.Add(_prefabListContainer);
        root.Add(_scrollView);

        RegisterDragAndDrop(dropArea);
        RegisterDragAndDrop(_scrollView);
        RegisterDragAndDrop(_prefabListContainer);

        UpdateApplyButtonState();
    }

    private void RegisterDragAndDrop(VisualElement target)
    {
        target.RegisterCallback<DragUpdatedEvent>(_ =>
        {
            DragAndDrop.visualMode = HasSupportedDraggedPrefab()
                ? DragAndDropVisualMode.Copy
                : DragAndDropVisualMode.Rejected;
        });

        target.RegisterCallback<DragPerformEvent>(_ =>
        {
            DragAndDrop.AcceptDrag();
            AddDraggedPrefabs(DragAndDrop.objectReferences);
        });
    }

    private static bool HasSupportedDraggedPrefab()
    {
        foreach (var obj in DragAndDrop.objectReferences)
        {
            if (TryGetPrefabAsset(obj, out _))
                return true;
        }

        return false;
    }

    private void AddDraggedPrefabs(Object[] draggedObjects)
    {
        foreach (var obj in draggedObjects)
        {
            if (!TryGetPrefabAsset(obj, out var prefabAsset))
                continue;

            if (_prefabs.Contains(prefabAsset))
                continue;

            _prefabs.Add(prefabAsset);
            AddPrefabElement(prefabAsset);
        }

        UpdateApplyButtonState();
    }

    private static bool TryGetPrefabAsset(Object obj, out GameObject prefabAsset)
    {
        prefabAsset = null;
        if (obj is not GameObject go)
            return false;

        if (PrefabUtility.IsPartOfPrefabAsset(go))
        {
            var path = AssetDatabase.GetAssetPath(go);
            prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefabAsset != null;
        }

        if (PrefabUtility.IsPartOfAnyPrefab(go))
        {
            var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
            if (string.IsNullOrEmpty(path))
                return false;

            prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefabAsset != null;
        }

        return false;
    }

    private void AddPrefabElement(GameObject prefab)
    {
        var element = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                alignItems = Align.Center,
                paddingTop = 5,
                paddingBottom = 5,
                paddingLeft = 5,
                paddingRight = 5,
                marginBottom = 2,
                backgroundColor = new Color(0.15f, 0.15f, 0.15f)
            }
        };

        element.Add(new Image
        {
            image = AssetPreview.GetAssetPreview(prefab) ?? EditorGUIUtility.IconContent("d_Prefab Icon").image,
            style = { width = 32, height = 32, marginRight = 5 }
        });

        var nameLabel = new Label(prefab.name)
        {
            style = { flexGrow = 1, color = Color.white }
        };
        nameLabel.RegisterCallback<ClickEvent>(_ =>
        {
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        });
        element.Add(nameLabel);

        element.Add(new Button(() =>
        {
            PrefabStageUtility.OpenPrefab(AssetDatabase.GetAssetPath(prefab));
        })
        {
            text = "편집",
            style = { width = 60, marginRight = 5 }
        });

        element.Add(new Button(() => RemovePrefabFromList(prefab, element))
        {
            text = "X",
            style = { width = 20 }
        });

        _prefabListContainer.Add(element);
    }

    private void RemovePrefabFromList(GameObject prefab, VisualElement element)
    {
        _prefabs.Remove(prefab);
        _prefabListContainer.Remove(element);
        UpdateApplyButtonState();
    }

    private void ClearPrefabList()
    {
        _prefabs.Clear();
        _prefabListContainer.Clear();
        UpdateApplyButtonState();
    }

    private void UpdateApplyButtonState()
    {
        if (_applyButton == null)
            return;

        _applyButton.SetEnabled(_prefabs.Count > 0);
    }

    private void ApplyRemoveMissingScripts()
    {
        if (_prefabs.Count == 0)
            return;

        var processedPrefabCount = 0;
        var modifiedPrefabCount = 0;
        var totalRemovedCount = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            for (var i = 0; i < _prefabs.Count; i++)
            {
                var prefab = _prefabs[i];
                if (prefab == null)
                    continue;

                var path = AssetDatabase.GetAssetPath(prefab);
                EditorUtility.DisplayProgressBar(
                    "Remove Missing Scripts",
                    $"Processing {prefab.name} ({i + 1}/{_prefabs.Count})",
                    (float)(i + 1) / _prefabs.Count);

                var removedCount = RemoveMissingScriptsFromPrefab(path);
                processedPrefabCount++;

                if (removedCount <= 0)
                    continue;

                modifiedPrefabCount++;
                totalRemovedCount += removedCount;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Remove Missing Scripts",
            $"처리한 프리팹: {processedPrefabCount}\n" +
            $"수정된 프리팹: {modifiedPrefabCount}\n" +
            $"제거된 Missing Script: {totalRemovedCount}",
            "확인");
    }

    private static int RemoveMissingScriptsFromPrefab(string prefabPath)
    {
        if (string.IsNullOrEmpty(prefabPath))
            return 0;

        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var removedCount = RemoveMissingScriptsRecursive(root);
            if (removedCount > 0)
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

            return removedCount;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static int RemoveMissingScriptsRecursive(GameObject go)
    {
        var removedCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
        if (removedCount > 0)
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);

        foreach (Transform child in go.transform)
            removedCount += RemoveMissingScriptsRecursive(child.gameObject);

        return removedCount;
    }
}
