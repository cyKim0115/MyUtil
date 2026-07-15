using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class RandomPrefabScatterWindow : EditorWindow
{
    const string EditorPrefsKey = "RandomPrefabScatterWindow_PrefabGUIDs";
    const float MinPanelHeight = 90f;
    const float SplitterHeight = 6f;
    const float ExecuteButtonHeight = 28f;

    readonly List<GameObject> prefabs = new List<GameObject>();
    readonly List<GameObject> sceneObjects = new List<GameObject>();
    readonly HashSet<int> sceneObjectIds = new HashSet<int>();

    Vector2 prefabScroll;
    Vector2 sceneScroll;
    float topPanelRatio = 0.45f;
    bool isDraggingSplitter;

    [MenuItem("Tools/Prefab/Random Prefab Scatter")]
    public static void ShowWindow()
    {
        var window = GetWindow<RandomPrefabScatterWindow>("Random Prefab Scatter");
        window.titleContent = new GUIContent("Random Prefab Scatter", EditorGUIUtility.IconContent("d_Prefab Icon").image);
        window.minSize = new Vector2(360f, 320f);
    }

    void OnEnable()
    {
        LoadPrefabList();
    }

    void OnDisable()
    {
        SavePrefabList();
    }

    void OnGUI()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorGUILayout.HelpBox("플레이 모드에서는 사용할 수 없습니다.", MessageType.Warning);
            return;
        }

        var contentRect = new Rect(0f, 0f, position.width, position.height - ExecuteButtonHeight);
        var topHeight = Mathf.Clamp(
            contentRect.height * topPanelRatio,
            MinPanelHeight,
            Mathf.Max(MinPanelHeight, contentRect.height - MinPanelHeight - SplitterHeight));

        var topRect = new Rect(0f, 0f, contentRect.width, topHeight);
        var splitterRect = new Rect(0f, topRect.yMax, contentRect.width, SplitterHeight);
        var bottomRect = new Rect(0f, splitterRect.yMax, contentRect.width, contentRect.height - splitterRect.yMax);

        DrawPrefabPanel(topRect);
        DrawSplitter(splitterRect, contentRect.height);
        DrawSceneObjectPanel(bottomRect);
        DrawExecuteButton();
    }

    void DrawPrefabPanel(Rect rect)
    {
        GUILayout.BeginArea(rect, EditorStyles.helpBox);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField($"Prefabs ({prefabs.Count})", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(prefabs.Count == 0))
            {
                if (GUILayout.Button("Clear", GUILayout.Width(70f)))
                    ClearPrefabs();
            }
        }

        var dropRect = GUILayoutUtility.GetRect(0f, 50f, GUILayout.ExpandWidth(true));
        GUI.Box(dropRect, "프리팹을 여기에 드래그 앤 드롭", EditorStyles.helpBox);
        HandlePrefabDragAndDrop(dropRect);

        prefabScroll = EditorGUILayout.BeginScrollView(prefabScroll);
        for (var i = prefabs.Count - 1; i >= 0; i--)
        {
            var prefab = prefabs[i];
            if (prefab == null)
            {
                prefabs.RemoveAt(i);
                continue;
            }

            DrawPrefabRow(prefab);
        }

        EditorGUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    void DrawPrefabRow(GameObject prefab)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            var preview = AssetPreview.GetAssetPreview(prefab);
            if (preview != null)
                GUILayout.Label(preview, GUILayout.Width(32f), GUILayout.Height(32f));
            else
                GUILayout.Space(32f);

            EditorGUILayout.LabelField(prefab.name, GUILayout.ExpandWidth(true));

            if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                RemovePrefab(prefab);
        }
    }

    void DrawSceneObjectPanel(Rect rect)
    {
        GUILayout.BeginArea(rect, EditorStyles.helpBox);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField($"Scene Objects ({sceneObjects.Count})", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(sceneObjects.Count == 0))
            {
                if (GUILayout.Button("Clear", GUILayout.Width(70f)))
                    ClearSceneObjects();
            }
        }

        var dropRect = GUILayoutUtility.GetRect(0f, 50f, GUILayout.ExpandWidth(true));
        GUI.Box(dropRect, "씬의 게임오브젝트를 여기에 드래그 앤 드롭", EditorStyles.helpBox);
        HandleSceneObjectDragAndDrop(dropRect);

        sceneScroll = EditorGUILayout.BeginScrollView(sceneScroll);
        for (var i = sceneObjects.Count - 1; i >= 0; i--)
        {
            var sceneObject = sceneObjects[i];
            if (sceneObject == null)
            {
                RemoveSceneObjectAt(i);
                continue;
            }

            DrawSceneObjectRow(sceneObject);
        }

        EditorGUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    void DrawSceneObjectRow(GameObject sceneObject)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.ObjectField(sceneObject, typeof(GameObject), true);

            if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                RemoveSceneObject(sceneObject);
        }
    }

    void DrawSplitter(Rect rect, float totalHeight)
    {
        EditorGUI.DrawRect(rect, new Color(0.18f, 0.18f, 0.18f));
        EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeVertical);

        var evt = Event.current;
        switch (evt.type)
        {
            case EventType.MouseDown:
                if (rect.Contains(evt.mousePosition))
                {
                    isDraggingSplitter = true;
                    evt.Use();
                }

                break;
            case EventType.MouseDrag:
                if (isDraggingSplitter)
                {
                    var minY = MinPanelHeight;
                    var maxY = Mathf.Max(minY, totalHeight - MinPanelHeight - SplitterHeight);
                    topPanelRatio = Mathf.Clamp(evt.mousePosition.y, minY, maxY) / totalHeight;
                    Repaint();
                    evt.Use();
                }

                break;
            case EventType.MouseUp:
                if (isDraggingSplitter)
                {
                    isDraggingSplitter = false;
                    evt.Use();
                }

                break;
        }
    }

    void DrawExecuteButton()
    {
        var canExecute = prefabs.Count > 0 && sceneObjects.Count > 0;
        var executeRect = new Rect(0f, position.height - ExecuteButtonHeight, position.width, ExecuteButtonHeight);

        using (new EditorGUI.DisabledScope(!canExecute))
        {
            if (GUI.Button(executeRect, "실행"))
                OnExecute();
        }
    }

    void HandlePrefabDragAndDrop(Rect dropRect)
    {
        var evt = Event.current;
        if (!dropRect.Contains(evt.mousePosition))
            return;

        if (evt.type == EventType.DragUpdated)
        {
            DragAndDrop.visualMode = HasSupportedDraggedPrefab()
                ? DragAndDropVisualMode.Copy
                : DragAndDropVisualMode.Rejected;
            evt.Use();
        }
        else if (evt.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            AddDraggedPrefabs(DragAndDrop.objectReferences);
            evt.Use();
        }
    }

    void HandleSceneObjectDragAndDrop(Rect dropRect)
    {
        var evt = Event.current;
        if (!dropRect.Contains(evt.mousePosition))
            return;

        if (evt.type == EventType.DragUpdated)
        {
            DragAndDrop.visualMode = HasSupportedDraggedSceneObject()
                ? DragAndDropVisualMode.Copy
                : DragAndDropVisualMode.Rejected;
            evt.Use();
        }
        else if (evt.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            AddDraggedSceneObjects(DragAndDrop.objectReferences);
            evt.Use();
        }
    }

    static bool HasSupportedDraggedPrefab()
    {
        foreach (var obj in DragAndDrop.objectReferences)
        {
            if (obj is GameObject go && PrefabUtility.IsPartOfPrefabAsset(go))
                return true;
        }

        return false;
    }

    static bool HasSupportedDraggedSceneObject()
    {
        foreach (var obj in DragAndDrop.objectReferences)
        {
            if (IsSceneGameObject(obj as GameObject))
                return true;
        }

        return false;
    }

    static bool IsSceneGameObject(GameObject go)
    {
        return go != null && !EditorUtility.IsPersistent(go);
    }

    void AddDraggedPrefabs(Object[] draggedObjects)
    {
        var changed = false;

        foreach (var obj in draggedObjects)
        {
            if (obj is not GameObject go || !PrefabUtility.IsPartOfPrefabAsset(go))
                continue;

            if (prefabs.Contains(go))
                continue;

            prefabs.Add(go);
            changed = true;
        }

        if (changed)
            SavePrefabList();
    }

    void AddDraggedSceneObjects(Object[] draggedObjects)
    {
        foreach (var obj in draggedObjects)
        {
            if (!IsSceneGameObject(obj as GameObject))
                continue;

            var go = (GameObject)obj;
            if (!sceneObjectIds.Add(go.GetInstanceID()))
                continue;

            sceneObjects.Add(go);
        }
    }

    void RemovePrefab(GameObject prefab)
    {
        if (!prefabs.Remove(prefab))
            return;

        SavePrefabList();
    }

    void ClearPrefabs()
    {
        prefabs.Clear();
        SavePrefabList();
    }

    void RemoveSceneObject(GameObject sceneObject)
    {
        var index = sceneObjects.IndexOf(sceneObject);
        if (index < 0)
            return;

        RemoveSceneObjectAt(index);
    }

    void RemoveSceneObjectAt(int index)
    {
        sceneObjectIds.Remove(sceneObjects[index].GetInstanceID());
        sceneObjects.RemoveAt(index);
    }

    void ClearSceneObjects()
    {
        sceneObjects.Clear();
        sceneObjectIds.Clear();
    }

    void OnExecute()
    {
        var validPrefabs = prefabs.Where(p => p != null).ToList();
        var validSceneObjects = sceneObjects.Where(go => go != null).ToList();

        if (validPrefabs.Count == 0 || validSceneObjects.Count == 0)
            return;

        var positions = validSceneObjects.Select(go => go.transform.position).ToList();
        var targetScene = validSceneObjects[0].scene;
        var parent = GetPlacementParent();

        Undo.SetCurrentGroupName("Random Prefab Scatter");
        var undoGroup = Undo.GetCurrentGroup();

        foreach (var sceneObject in validSceneObjects)
            Undo.DestroyObjectImmediate(sceneObject);

        foreach (var position in positions)
        {
            var prefab = validPrefabs[Random.Range(0, validPrefabs.Count)];
            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                continue;

            Undo.RegisterCreatedObjectUndo(instance, "Random Prefab Scatter");
            instance.transform.SetParent(parent, true);
            instance.transform.position = position;
        }

        Undo.CollapseUndoOperations(undoGroup);
        ClearSceneObjects();

        if (targetScene.IsValid())
            EditorSceneManager.MarkSceneDirty(targetScene);
        SceneView.RepaintAll();
    }

    static Transform GetPlacementParent()
    {
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        return stage != null ? stage.prefabContentsRoot.transform : null;
    }

    void SavePrefabList()
    {
        if (prefabs.Count == 0)
        {
            EditorPrefs.DeleteKey(EditorPrefsKey);
            return;
        }

        var guidList = prefabs
            .Where(p => p != null)
            .Select(p => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(p)))
            .ToList();
        EditorPrefs.SetString(EditorPrefsKey, string.Join(",", guidList));
    }

    void LoadPrefabList()
    {
        prefabs.Clear();

        if (!EditorPrefs.HasKey(EditorPrefsKey))
            return;

        var guidString = EditorPrefs.GetString(EditorPrefsKey);
        if (string.IsNullOrEmpty(guidString))
            return;

        foreach (var guid in guidString.Split(','))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
                prefabs.Add(prefab);
        }
    }
}
