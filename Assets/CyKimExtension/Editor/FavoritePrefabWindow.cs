using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;
using System.Linq;
using System;

public class FavoritePrefabWindow : EditorWindow
{
    private const string EditorPrefsKeyLegacy = "FavoritePrefabWindow_PrefabGUIDs";
    private const string EditorPrefsKeyStore = "FavoritePrefabWindow_Store_v1";
    private const string DefaultSetName = "Default";

    private FavoritePrefabStore _store;
    private List<GameObject> _prefabs = new List<GameObject>();
    private VisualElement _prefabListContainer;
    private VisualElement _setChipRow;
    private DropdownField _setDropdown;
    private TextField _renameField;
    private Label _setStatusLabel;
    private bool _suppressSetChange;
    private bool _isRenaming;

#if UNITY_EDITOR_OSX
    [MenuItem("Tools/Prefab/Favorite Prefab %#f")]
#else
    [MenuItem("Tools/Prefab/Favorite Prefab %#f")]
#endif
    public static void ShowWindow()
    {
        var window = GetWindow<FavoritePrefabWindow>("Favorite Prefab");
        window.titleContent = new GUIContent("Favorite Prefab", EditorGUIUtility.IconContent("Favorite Icon").image);
    }

    private void OnEnable()
    {
        LoadStore();
        LoadActiveSetPrefabs();
    }

    private void OnDisable()
    {
        PersistActiveSetAndSave();
    }

    private void CreateGUI()
    {
        var root = rootVisualElement;
        root.style.paddingTop = root.style.paddingBottom = root.style.paddingLeft = root.style.paddingRight = 10;

        root.Add(BuildSetToolbar());
        _setChipRow = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                flexWrap = Wrap.Wrap,
                marginBottom = 8,
            }
        };
        root.Add(_setChipRow);

        var dropArea = new VisualElement
        {
            style = { height = 50, backgroundColor = new Color(0.2f, 0.2f, 0.2f), justifyContent = Justify.Center, marginBottom = 6 }
        };
        var dropLabel = new Label("프리팹을 여기에 놓으세요")
        {
            style = { alignSelf = Align.Center, color = Color.white }
        };
        dropArea.Add(dropLabel);
        root.Add(dropArea);

        _prefabListContainer = new VisualElement();
        root.Add(_prefabListContainer);

        dropArea.RegisterCallback<DragUpdatedEvent>(_ =>
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
        });

        dropArea.RegisterCallback<DragPerformEvent>(_ =>
        {
            DragAndDrop.AcceptDrag();
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is GameObject go && PrefabUtility.IsPartOfPrefabAsset(go))
                {
                    if (!_prefabs.Contains(go))
                    {
                        _prefabs.Add(go);
                        AddPrefabElement(go);
                        PersistActiveSetAndSave();
                    }
                }
            }
        });

        RefreshSetUi();
        RefreshPrefabListUI();
    }

    private VisualElement BuildSetToolbar()
    {
        var toolbar = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                alignItems = Align.Center,
                marginBottom = 6,
                flexWrap = Wrap.Wrap,
            }
        };

        var setLabel = new Label("세트")
        {
            style = { marginRight = 6, color = Color.white, unityFontStyleAndWeight = FontStyle.Bold }
        };
        toolbar.Add(setLabel);

        _setDropdown = new DropdownField
        {
            style = { minWidth = 120, flexGrow = 1, marginRight = 4 }
        };
        _setDropdown.RegisterValueChangedCallback(OnSetDropdownChanged);
        toolbar.Add(_setDropdown);

        _renameField = new TextField
        {
            style = { minWidth = 120, flexGrow = 1, marginRight = 4, display = DisplayStyle.None }
        };
        _renameField.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                CommitRename();
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.Escape)
            {
                CancelRename();
                evt.StopPropagation();
            }
        });
        _renameField.RegisterCallback<FocusOutEvent>(_ =>
        {
            if (_isRenaming)
            {
                CommitRename();
            }
        });
        toolbar.Add(_renameField);

        toolbar.Add(MakeToolbarButton("◀", () => MoveActiveSet(-1)));
        toolbar.Add(MakeToolbarButton("▶", () => MoveActiveSet(1)));
        toolbar.Add(MakeToolbarButton("새 세트", CreateNewSet));
        toolbar.Add(MakeToolbarButton("이름", BeginRename));
        toolbar.Add(MakeToolbarButton("복제", DuplicateActiveSet));
        toolbar.Add(MakeToolbarButton("삭제", DeleteActiveSet));

        _setStatusLabel = new Label
        {
            style = { marginLeft = 6, color = new Color(0.75f, 0.75f, 0.75f), flexGrow = 1 }
        };
        toolbar.Add(_setStatusLabel);

        return toolbar;
    }

    private static Button MakeToolbarButton(string text, Action onClick)
    {
        return new Button(onClick)
        {
            text = text,
            style = { marginRight = 2, paddingLeft = 6, paddingRight = 6 }
        };
    }

    private void OnSetDropdownChanged(ChangeEvent<string> evt)
    {
        if (_suppressSetChange || _store == null)
        {
            return;
        }

        var target = _store.sets.FirstOrDefault(s => s.name == evt.newValue);
        if (target == null || target.id == _store.activeSetId)
        {
            return;
        }

        SwitchToSet(target.id);
    }

    private void RefreshSetUi()
    {
        if (_setDropdown == null || _store == null)
        {
            return;
        }

        var names = _store.sets.Select(s => s.name).ToList();
        var active = GetActiveSet();
        _suppressSetChange = true;
        _setDropdown.choices = names;
        _setDropdown.SetValueWithoutNotify(active != null ? active.name : string.Empty);
        _suppressSetChange = false;

        if (_setStatusLabel != null)
        {
            var count = _prefabs?.Count(p => p != null) ?? 0;
            _setStatusLabel.text = active != null ? $"{count}개" : string.Empty;
        }

        RefreshSetChips();
    }

    private void RefreshSetChips()
    {
        if (_setChipRow == null || _store == null)
        {
            return;
        }

        _setChipRow.Clear();
        foreach (var set in _store.sets)
        {
            var setId = set.id;
            var isActive = setId == _store.activeSetId;
            // Click is handled by ReorderManipulator (PointerDown is captured for drag).
            var chip = new Button(() => { })
            {
                text = set.name,
                style =
                {
                    marginRight = 4,
                    marginBottom = 2,
                    backgroundColor = isActive
                        ? new Color(0.25f, 0.45f, 0.7f)
                        : new Color(0.22f, 0.22f, 0.22f),
                    color = Color.white,
                }
            };
            chip.tooltip = "클릭: 전환 / 드래그: 세트 순서 변경";
            chip.AddManipulator(new ReorderManipulator(
                chip,
                _setChipRow,
                ReorderAxis.Horizontal,
                MoveSet,
                OnSetReorderFinished,
                onClick: () =>
                {
                    if (setId != _store.activeSetId)
                    {
                        SwitchToSet(setId);
                    }
                }));
            _setChipRow.Add(chip);
        }
    }

    private void OnSetReorderFinished()
    {
        SaveStore();
        // Defer rebuild so PointerUp can finish on the dragged chip.
        _setChipRow?.schedule.Execute(RefreshSetUi).ExecuteLater(1);
    }

    private void MoveActiveSet(int delta)
    {
        var active = GetActiveSet();
        if (active == null)
        {
            return;
        }

        var index = _store.sets.FindIndex(s => s.id == active.id);
        var target = index + delta;
        if (index < 0 || target < 0 || target >= _store.sets.Count)
        {
            return;
        }

        MoveSet(index, target);
        SaveStore();
        RefreshSetUi();
    }

    private void MoveSet(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex
            || fromIndex < 0 || toIndex < 0
            || fromIndex >= _store.sets.Count || toIndex >= _store.sets.Count)
        {
            return;
        }

        var item = _store.sets[fromIndex];
        _store.sets.RemoveAt(fromIndex);
        _store.sets.Insert(toIndex, item);

        if (_setChipRow != null
            && fromIndex < _setChipRow.childCount
            && toIndex < _setChipRow.childCount)
        {
            var element = _setChipRow[fromIndex];
            _setChipRow.RemoveAt(fromIndex);
            _setChipRow.Insert(toIndex, element);
        }
    }

    private void SwitchToSet(string setId)
    {
        if (_isRenaming)
        {
            CancelRename();
        }

        PersistActiveSetToStore();
        _store.activeSetId = setId;
        LoadActiveSetPrefabs();
        SaveStore();
        RefreshSetUi();
        RefreshPrefabListUI();
    }

    private void CreateNewSet()
    {
        PersistActiveSetToStore();
        var name = MakeUniqueSetName("새 세트");
        var set = new FavoritePrefabSet
        {
            id = Guid.NewGuid().ToString("N"),
            name = name,
            guids = Array.Empty<string>(),
        };
        _store.sets.Add(set);
        _store.activeSetId = set.id;
        LoadActiveSetPrefabs();
        SaveStore();
        RefreshSetUi();
        RefreshPrefabListUI();
        BeginRename();
    }

    private void BeginRename()
    {
        var active = GetActiveSet();
        if (active == null || _renameField == null || _setDropdown == null)
        {
            return;
        }

        _isRenaming = true;
        _renameField.value = active.name;
        _renameField.style.display = DisplayStyle.Flex;
        _setDropdown.style.display = DisplayStyle.None;
        _renameField.schedule.Execute(() => _renameField.Focus()).ExecuteLater(1);
        _renameField.SelectAll();
    }

    private void CommitRename()
    {
        if (!_isRenaming)
        {
            return;
        }

        var active = GetActiveSet();
        if (active == null)
        {
            CancelRename();
            return;
        }

        var nextName = (_renameField?.value ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(nextName))
        {
            CancelRename();
            return;
        }

        if (!string.Equals(active.name, nextName, StringComparison.Ordinal)
            && _store.sets.Any(s => s.id != active.id && string.Equals(s.name, nextName, StringComparison.Ordinal)))
        {
            nextName = MakeUniqueSetName(nextName);
        }

        active.name = nextName;
        _isRenaming = false;
        _renameField.style.display = DisplayStyle.None;
        _setDropdown.style.display = DisplayStyle.Flex;
        SaveStore();
        RefreshSetUi();
    }

    private void CancelRename()
    {
        _isRenaming = false;
        if (_renameField != null)
        {
            _renameField.style.display = DisplayStyle.None;
        }

        if (_setDropdown != null)
        {
            _setDropdown.style.display = DisplayStyle.Flex;
        }
    }

    private void DuplicateActiveSet()
    {
        var active = GetActiveSet();
        if (active == null)
        {
            return;
        }

        PersistActiveSetToStore();
        var copy = new FavoritePrefabSet
        {
            id = Guid.NewGuid().ToString("N"),
            name = MakeUniqueSetName(active.name + " 복사"),
            guids = active.guids != null ? (string[])active.guids.Clone() : Array.Empty<string>(),
        };
        _store.sets.Add(copy);
        _store.activeSetId = copy.id;
        LoadActiveSetPrefabs();
        SaveStore();
        RefreshSetUi();
        RefreshPrefabListUI();
    }

    private void DeleteActiveSet()
    {
        if (_store.sets.Count <= 1)
        {
            EditorUtility.DisplayDialog("Favorite Prefab", "마지막 세트는 삭제할 수 없습니다.", "확인");
            return;
        }

        var active = GetActiveSet();
        if (active == null)
        {
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Favorite Prefab",
                $"세트 \"{active.name}\"을(를) 삭제할까요?\n등록된 프리팹 목록도 함께 사라집니다.",
                "삭제",
                "취소"))
        {
            return;
        }

        if (_isRenaming)
        {
            CancelRename();
        }

        var index = _store.sets.FindIndex(s => s.id == active.id);
        _store.sets.RemoveAt(index);
        var nextIndex = Mathf.Clamp(index, 0, _store.sets.Count - 1);
        _store.activeSetId = _store.sets[nextIndex].id;
        LoadActiveSetPrefabs();
        SaveStore();
        RefreshSetUi();
        RefreshPrefabListUI();
    }

    private string MakeUniqueSetName(string baseName)
    {
        if (_store.sets.All(s => s.name != baseName))
        {
            return baseName;
        }

        for (var i = 2; i < 1000; i++)
        {
            var candidate = $"{baseName} {i}";
            if (_store.sets.All(s => s.name != candidate))
            {
                return candidate;
            }
        }

        return $"{baseName} {Guid.NewGuid().ToString("N").Substring(0, 4)}";
    }

    private FavoritePrefabSet GetActiveSet()
    {
        if (_store?.sets == null || _store.sets.Count == 0)
        {
            return null;
        }

        var active = _store.sets.FirstOrDefault(s => s.id == _store.activeSetId);
        if (active != null)
        {
            return active;
        }

        _store.activeSetId = _store.sets[0].id;
        return _store.sets[0];
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
        element.userData = prefab;

        var dragHandle = new Label("≡")
        {
            style =
            {
                width = 18,
                marginRight = 4,
                color = new Color(0.7f, 0.7f, 0.7f),
                unityTextAlign = TextAnchor.MiddleCenter,
                unityFontStyleAndWeight = FontStyle.Bold,
            },
            tooltip = "행을 드래그하여 순서 변경"
        };
        element.Add(dragHandle);

        var icon = new Image
        {
            image = AssetPreview.GetAssetPreview(prefab),
            style = { width = 32, height = 32, marginRight = 5 }
        };
        element.Add(icon);

        var nameLabel = new Label(prefab.name)
        {
            style = { flexGrow = 1, color = Color.white }
        };
        element.Add(nameLabel);

        var editButton = new Button(() =>
        {
            PrefabStageUtility.OpenPrefab(AssetDatabase.GetAssetPath(prefab));
        })
        {
            text = "편집",
            style = { width = 60, marginRight = 5 }
        };
        element.Add(editButton);

        var deleteButton = new Button(() =>
        {
            _prefabs.Remove(prefab);
            _prefabListContainer.Remove(element);
            PersistActiveSetAndSave();
            RefreshSetUi();
        })
        {
            text = "X",
            style = { width = 20 },
        };
        element.Add(deleteButton);

        // Attach to the row so live reordering follows the pointer across siblings.
        element.AddManipulator(new ReorderManipulator(
            element,
            _prefabListContainer,
            ReorderAxis.Vertical,
            MovePrefab,
            OnPrefabReorderFinished,
            onClick: () =>
            {
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            },
            ignoreButtons: true));

        _prefabListContainer.Add(element);
    }

    private void OnPrefabReorderFinished()
    {
        PersistActiveSetAndSave();
    }

    private void MovePrefab(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex
            || fromIndex < 0 || toIndex < 0
            || fromIndex >= _prefabs.Count || toIndex >= _prefabs.Count
            || _prefabListContainer == null
            || fromIndex >= _prefabListContainer.childCount
            || toIndex >= _prefabListContainer.childCount)
        {
            return;
        }

        var prefab = _prefabs[fromIndex];
        _prefabs.RemoveAt(fromIndex);
        _prefabs.Insert(toIndex, prefab);

        var element = _prefabListContainer[fromIndex];
        _prefabListContainer.RemoveAt(fromIndex);
        _prefabListContainer.Insert(toIndex, element);
    }

    private void RefreshPrefabListUI()
    {
        if (_prefabListContainer == null)
        {
            return;
        }

        _prefabListContainer.Clear();
        if (_prefabs == null || _prefabs.Count == 0)
        {
            return;
        }

        foreach (var prefab in _prefabs)
        {
            if (prefab != null)
            {
                AddPrefabElement(prefab);
            }
        }
    }

    private void PersistActiveSetAndSave()
    {
        PersistActiveSetToStore();
        SaveStore();
        RefreshSetUi();
    }

    private void PersistActiveSetToStore()
    {
        var active = GetActiveSet();
        if (active == null)
        {
            return;
        }

        active.guids = (_prefabs ?? new List<GameObject>())
            .Where(p => p != null)
            .Select(p => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(p)))
            .Where(g => !string.IsNullOrEmpty(g))
            .ToArray();
    }

    private void LoadActiveSetPrefabs()
    {
        _prefabs = new List<GameObject>();
        var active = GetActiveSet();
        if (active?.guids == null)
        {
            return;
        }

        foreach (var guid in active.guids)
        {
            if (string.IsNullOrEmpty(guid))
            {
                continue;
            }

            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                _prefabs.Add(prefab);
            }
        }
    }

    private void SaveStore()
    {
        if (_store == null)
        {
            return;
        }

        var data = new FavoritePrefabStoreData
        {
            activeSetId = _store.activeSetId,
            sets = _store.sets.Select(s => new FavoritePrefabSetData
            {
                id = s.id,
                name = s.name,
                guids = s.guids ?? Array.Empty<string>(),
            }).ToArray(),
        };
        EditorPrefs.SetString(EditorPrefsKeyStore, JsonUtility.ToJson(data));
    }

    private void LoadStore()
    {
        if (_store != null)
        {
            return;
        }

        if (EditorPrefs.HasKey(EditorPrefsKeyStore))
        {
            var json = EditorPrefs.GetString(EditorPrefsKeyStore);
            if (!string.IsNullOrEmpty(json))
            {
                var data = JsonUtility.FromJson<FavoritePrefabStoreData>(json);
                if (data?.sets != null && data.sets.Length > 0)
                {
                    _store = new FavoritePrefabStore
                    {
                        activeSetId = data.activeSetId,
                        sets = data.sets.Select(s => new FavoritePrefabSet
                        {
                            id = string.IsNullOrEmpty(s.id) ? Guid.NewGuid().ToString("N") : s.id,
                            name = string.IsNullOrEmpty(s.name) ? DefaultSetName : s.name,
                            guids = s.guids ?? Array.Empty<string>(),
                        }).ToList(),
                    };
                    EnsureActiveSetValid();
                    return;
                }
            }
        }

        _store = CreateStoreFromLegacyOrDefault();
        SaveStore();
        if (EditorPrefs.HasKey(EditorPrefsKeyLegacy))
        {
            EditorPrefs.DeleteKey(EditorPrefsKeyLegacy);
        }
    }

    private FavoritePrefabStore CreateStoreFromLegacyOrDefault()
    {
        var guids = Array.Empty<string>();
        if (EditorPrefs.HasKey(EditorPrefsKeyLegacy))
        {
            var guidString = EditorPrefs.GetString(EditorPrefsKeyLegacy);
            if (!string.IsNullOrEmpty(guidString))
            {
                guids = guidString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        var set = new FavoritePrefabSet
        {
            id = Guid.NewGuid().ToString("N"),
            name = DefaultSetName,
            guids = guids,
        };
        return new FavoritePrefabStore
        {
            activeSetId = set.id,
            sets = new List<FavoritePrefabSet> { set },
        };
    }

    private void EnsureActiveSetValid()
    {
        if (_store.sets.Count == 0)
        {
            var set = new FavoritePrefabSet
            {
                id = Guid.NewGuid().ToString("N"),
                name = DefaultSetName,
                guids = Array.Empty<string>(),
            };
            _store.sets.Add(set);
            _store.activeSetId = set.id;
            return;
        }

        if (_store.sets.All(s => s.id != _store.activeSetId))
        {
            _store.activeSetId = _store.sets[0].id;
        }
    }

    private class FavoritePrefabStore
    {
        public string activeSetId;
        public List<FavoritePrefabSet> sets = new List<FavoritePrefabSet>();
    }

    private class FavoritePrefabSet
    {
        public string id;
        public string name;
        public string[] guids = Array.Empty<string>();
    }

    [Serializable]
    private class FavoritePrefabStoreData
    {
        public string activeSetId;
        public FavoritePrefabSetData[] sets;
    }

    [Serializable]
    private class FavoritePrefabSetData
    {
        public string id;
        public string name;
        public string[] guids;
    }

    private enum ReorderAxis
    {
        Vertical,
        Horizontal,
    }

    /// <summary>
    /// Drag-to-reorder with live sibling swaps when the pointer crosses midpoints.
    /// </summary>
    private class ReorderManipulator : Manipulator
    {
        private const float DragThresholdPx = 5f;
        private static readonly Color DragHighlight = new Color(0.28f, 0.42f, 0.58f, 0.95f);

        private readonly VisualElement _row;
        private readonly VisualElement _container;
        private readonly ReorderAxis _axis;
        private readonly Action<int, int> _move;
        private readonly Action _onDragFinished;
        private readonly Action _onClick;
        private readonly bool _ignoreButtons;

        private bool _pointerDown;
        private bool _dragging;
        private int _pointerId;
        private Vector2 _startPanelPos;
        private bool _orderChanged;
        private StyleColor _originalBackground;

        public ReorderManipulator(
            VisualElement row,
            VisualElement container,
            ReorderAxis axis,
            Action<int, int> move,
            Action onDragFinished,
            Action onClick = null,
            bool ignoreButtons = false)
        {
            _row = row;
            _container = container;
            _axis = axis;
            _move = move;
            _onDragFinished = onDragFinished;
            _onClick = onClick;
            _ignoreButtons = ignoreButtons;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        private static bool IsFromButton(IEventHandler handler)
        {
            var ve = handler as VisualElement;
            while (ve != null)
            {
                if (ve is Button)
                {
                    return true;
                }

                ve = ve.parent;
            }

            return false;
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0)
            {
                return;
            }

            if (_ignoreButtons && IsFromButton(evt.target))
            {
                return;
            }

            _pointerDown = true;
            _dragging = false;
            _orderChanged = false;
            _pointerId = evt.pointerId;
            _startPanelPos = (Vector2)evt.position;
            target.CapturePointer(_pointerId);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_pointerDown || evt.pointerId != _pointerId)
            {
                return;
            }

            var panelPos = (Vector2)evt.position;
            if (!_dragging)
            {
                if ((panelPos - _startPanelPos).sqrMagnitude < DragThresholdPx * DragThresholdPx)
                {
                    return;
                }

                _dragging = true;
                _originalBackground = _row.style.backgroundColor;
                _row.style.opacity = 0.85f;
                _row.style.backgroundColor = DragHighlight;
            }

            // Keep swapping live while dragging so the drop slot is visible.
            var fromIndex = _container.IndexOf(_row);
            var hoverIndex = FindCrossedIndex(panelPos, fromIndex);
            if (fromIndex >= 0 && hoverIndex >= 0 && hoverIndex != fromIndex)
            {
                _move(fromIndex, hoverIndex);
                _orderChanged = true;
            }

            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!_pointerDown || evt.pointerId != _pointerId)
            {
                return;
            }

            var shouldClick = !_dragging && !_orderChanged;
            FinishDrag();
            if (shouldClick)
            {
                _onClick?.Invoke();
            }

            evt.StopPropagation();
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (_pointerDown)
            {
                FinishDrag();
            }
        }

        private void FinishDrag()
        {
            _pointerDown = false;
            if (_dragging)
            {
                _row.style.opacity = 1f;
                _row.style.backgroundColor = _originalBackground;
            }

            if (target.HasPointerCapture(_pointerId))
            {
                target.ReleasePointer(_pointerId);
            }

            if (_orderChanged)
            {
                _onDragFinished?.Invoke();
            }

            _dragging = false;
            _orderChanged = false;
        }

        /// <summary>
        /// Swap at most one adjacent slot per move when the pointer crosses that sibling's midpoint.
        /// </summary>
        private int FindCrossedIndex(Vector2 panelPos, int fromIndex)
        {
            if (fromIndex < 0 || _container.childCount == 0)
            {
                return -1;
            }

            if (fromIndex > 0)
            {
                var prevBound = _container[fromIndex - 1].worldBound;
                if (_axis == ReorderAxis.Vertical)
                {
                    var mid = prevBound.yMin + prevBound.height * 0.5f;
                    if (panelPos.y < mid)
                    {
                        return fromIndex - 1;
                    }
                }
                else
                {
                    var mid = prevBound.xMin + prevBound.width * 0.5f;
                    if (panelPos.x < mid)
                    {
                        return fromIndex - 1;
                    }
                }
            }

            if (fromIndex < _container.childCount - 1)
            {
                var nextBound = _container[fromIndex + 1].worldBound;
                if (_axis == ReorderAxis.Vertical)
                {
                    var mid = nextBound.yMin + nextBound.height * 0.5f;
                    if (panelPos.y > mid)
                    {
                        return fromIndex + 1;
                    }
                }
                else
                {
                    var mid = nextBound.xMin + nextBound.width * 0.5f;
                    if (panelPos.x > mid)
                    {
                        return fromIndex + 1;
                    }
                }
            }

            return fromIndex;
        }
    }
}
