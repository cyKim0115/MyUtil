using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomPropertyDrawer(typeof(SerializableDictionary<,>))]
public class SerializableDictionaryDrawer : PropertyDrawer
{
    private const float BUTTON_WIDTH = 20f;
    private const float SPACING = 5f;
    private const float BOTTOM_PADDING = 10f;

    private readonly HashSet<int> usedKeyIndices = new();
    private readonly Dictionary<string, ReorderableList> reorderableLists = new();
    private readonly Dictionary<string, int> newEnumValues = new();
    private readonly Dictionary<string, string> newStringValues = new();
    private readonly Dictionary<string, int> newIntValues = new();
    private readonly Dictionary<string, float> newFloatValues = new();
    private readonly Dictionary<string, DoubleColor> newDoubleColorValues = new();

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        property.serializedObject.Update();

        var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            var pairsProperty = property.FindPropertyRelative("keyValuePairs");
            if (pairsProperty is not { isArray: true })
            {
                EditorGUI.LabelField(position, "Error: Key Value Pair 찾을 수 없음");
                EditorGUI.EndProperty();
                return;
            }

            // 키 타입이 Enum인지 확인
            bool isEnumKey = false;
            SerializedPropertyType keyType = SerializedPropertyType.String;
            if (pairsProperty.arraySize > 0)
            {
                var firstElement = pairsProperty.GetArrayElementAtIndex(0);
                var keyProperty = firstElement.FindPropertyRelative("key");
                keyType = keyProperty.propertyType;
                isEnumKey = keyType == SerializedPropertyType.Enum;
            }

            // 렉트들 사이즈
            var sizeRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + SPACING, position.width, EditorGUIUtility.singleLineHeight);
            var sizeLabelRect = new Rect(sizeRect.x, sizeRect.y, 40f, sizeRect.height);
            var sizeFieldRect = new Rect(sizeLabelRect.xMax + SPACING, sizeRect.y, sizeRect.width * 0.3f - 40f - SPACING, sizeRect.height);
            var spaceRect = new Rect(sizeFieldRect.xMax + SPACING, sizeRect.y, sizeRect.width * 0.1f, sizeRect.height);
            var inputRect = new Rect(spaceRect.xMax + SPACING, sizeRect.y, sizeRect.width * 0.5f - SPACING, sizeRect.height);
            var addButtonRect = new Rect(inputRect.xMax + SPACING, sizeRect.y, sizeRect.width * 0.1f, sizeRect.height);

            var sizeProperty = pairsProperty.FindPropertyRelative("Array.size");
            EditorGUI.LabelField(sizeLabelRect, "Size");

            EditorGUI.BeginChangeCheck();
            EditorGUI.LabelField(sizeFieldRect, sizeProperty.intValue.ToString(), EditorStyles.boldLabel);
            if (EditorGUI.EndChangeCheck())
            {
                pairsProperty.serializedObject.ApplyModifiedProperties();
                pairsProperty.serializedObject.Update();
            }

            string propertyPath = property.propertyPath;
            newEnumValues.TryAdd(propertyPath, -99);
            newStringValues.TryAdd(propertyPath, "");
            newIntValues.TryAdd(propertyPath, 0);
            newFloatValues.TryAdd(propertyPath, 0f);
            newDoubleColorValues.TryAdd(propertyPath, new DoubleColor(Color.white, Color.white));

            if (pairsProperty.arraySize == 0)
            {
                // 리스트가 비어 있을 경우 버튼 표시
                if (GUI.Button(addButtonRect, "Add First Pair"))
                {
                    pairsProperty.arraySize++;
                    var elementProperty = pairsProperty.GetArrayElementAtIndex(pairsProperty.arraySize - 1);
                    var keyProperty = elementProperty.FindPropertyRelative("key");
                    if (isEnumKey)
                    {
                        keyProperty.enumValueIndex = 0;
                    }
                    pairsProperty.serializedObject.ApplyModifiedProperties();
                    pairsProperty.serializedObject.Update();
                }
            }
            else if (isEnumKey)
            {
                // Enum 타입일 때만 기존 로직 사용
                var firstKeyProperty = pairsProperty.GetArrayElementAtIndex(0).FindPropertyRelative("key");
                var displayNamesWithDefault = new string[firstKeyProperty.enumDisplayNames.Length + 1];
                displayNamesWithDefault[0] = "-";
                Array.Copy(firstKeyProperty.enumDisplayNames, 0, displayNamesWithDefault, 1, firstKeyProperty.enumDisplayNames.Length);
                EditorGUI.BeginChangeCheck();
                
                int selectedIndex = EditorGUI.Popup(inputRect, newEnumValues[propertyPath] == -99 ? 0 : newEnumValues[propertyPath] + 1, displayNamesWithDefault);
                if (EditorGUI.EndChangeCheck())
                {
                    int enumIndex = selectedIndex - 1;
                    newEnumValues[propertyPath] = enumIndex;

                    if (enumIndex >= 0)
                    {
                        usedKeyIndices.Clear();
                        for (int i = 0; i < pairsProperty.arraySize; i++)
                        {
                            var elementProperty = pairsProperty.GetArrayElementAtIndex(i);
                            var keyProperty = elementProperty.FindPropertyRelative("key");
                            usedKeyIndices.Add(keyProperty.enumValueIndex);
                        }

                        if (!usedKeyIndices.Contains(enumIndex))
                        {
                            pairsProperty.arraySize++;
                            var elementProperty = pairsProperty.GetArrayElementAtIndex(pairsProperty.arraySize - 1);
                            var keyProperty = elementProperty.FindPropertyRelative("key");
                            keyProperty.enumValueIndex = enumIndex; // 선택된 Enum 값으로 설정
                            pairsProperty.serializedObject.ApplyModifiedProperties();
                            pairsProperty.serializedObject.Update();
                        }
                        else
                        {
                            Debug.LogError($"이미 있는 키 선택 '{firstKeyProperty.enumDisplayNames[enumIndex]}'");
                        }

                        newEnumValues[propertyPath] = -99;
                    }
                }
            }
            else
            {
                // 일반 타입을 위한 입력 필드와 추가 버튼
                bool shouldAdd = false;
                
                switch (keyType)
                {
                    case SerializedPropertyType.String:
                        EditorGUI.BeginChangeCheck();
                        newStringValues[propertyPath] = EditorGUI.TextField(inputRect, newStringValues[propertyPath]);
                        if (EditorGUI.EndChangeCheck())
                        {
                            // 입력값 변경 시 처리
                        }
                        break;
                        
                    case SerializedPropertyType.Integer:
                        EditorGUI.BeginChangeCheck();
                        newIntValues[propertyPath] = EditorGUI.IntField(inputRect, newIntValues[propertyPath]);
                        if (EditorGUI.EndChangeCheck())
                        {
                            // 입력값 변경 시 처리
                        }
                        break;
                        
                    case SerializedPropertyType.Float:
                        EditorGUI.BeginChangeCheck();
                        newFloatValues[propertyPath] = EditorGUI.FloatField(inputRect, newFloatValues[propertyPath]);
                        if (EditorGUI.EndChangeCheck())
                        {
                            // 입력값 변경 시 처리
                        }
                        break;
                        
                    default:
                        // DoubleColor나 다른 복합 타입의 경우
                        if (keyType == SerializedPropertyType.Generic)
                        {
                            EditorGUI.LabelField(inputRect, "DoubleColor (기본값으로 추가)");
                        }
                        else
                        {
                            EditorGUI.LabelField(inputRect, $"지원하지 않는 키 타입: {keyType}");
                        }
                        break;
                }
                
                // 추가 버튼
                if (GUI.Button(addButtonRect, "Add"))
                {
                    shouldAdd = true;
                }
                
                if (shouldAdd)
                {
                    // 중복 키 체크
                    bool isDuplicate = false;
                    for (int i = 0; i < pairsProperty.arraySize; i++)
                    {
                        var elementProperty = pairsProperty.GetArrayElementAtIndex(i);
                        var keyProperty = elementProperty.FindPropertyRelative("key");
                        
                        switch (keyType)
                        {
                            case SerializedPropertyType.String:
                                if (keyProperty.stringValue == newStringValues[propertyPath])
                                {
                                    isDuplicate = true;
                                    Debug.LogError($"중복 키 감지: '{newStringValues[propertyPath]}'");
                                }
                                break;
                                
                            case SerializedPropertyType.Integer:
                                if (keyProperty.intValue == newIntValues[propertyPath])
                                {
                                    isDuplicate = true;
                                    Debug.LogError($"중복 키 감지: {newIntValues[propertyPath]}");
                                }
                                break;
                                
                            case SerializedPropertyType.Float:
                                if (Mathf.Approximately(keyProperty.floatValue, newFloatValues[propertyPath]))
                                {
                                    isDuplicate = true;
                                    Debug.LogError($"중복 키 감지: {newFloatValues[propertyPath]}");
                                }
                                break;
                                
                            case SerializedPropertyType.Generic:
                                // DoubleColor 추가
                                var doubleColorToAdd = newDoubleColorValues[propertyPath];
                                keyProperty.FindPropertyRelative("_top").colorValue = doubleColorToAdd.top;
                                keyProperty.FindPropertyRelative("_bottom").colorValue = doubleColorToAdd.bottom;
                                newDoubleColorValues[propertyPath] = new DoubleColor(Color.white, Color.white); // 입력 필드 초기화
                                break;
                        }
                        
                        if (isDuplicate) break;
                    }
                    
                    if (!isDuplicate)
                    {
                        pairsProperty.arraySize++;
                        var elementProperty = pairsProperty.GetArrayElementAtIndex(pairsProperty.arraySize - 1);
                        var keyProperty = elementProperty.FindPropertyRelative("key");
                        
                        switch (keyType)
                        {
                            case SerializedPropertyType.String:
                                keyProperty.stringValue = newStringValues[propertyPath];
                                newStringValues[propertyPath] = ""; // 입력 필드 초기화
                                break;
                                
                            case SerializedPropertyType.Integer:
                                keyProperty.intValue = newIntValues[propertyPath];
                                newIntValues[propertyPath] = 0; // 입력 필드 초기화
                                break;
                                
                            case SerializedPropertyType.Float:
                                keyProperty.floatValue = newFloatValues[propertyPath];
                                newFloatValues[propertyPath] = 0f; // 입력 필드 초기화
                                break;
                        }
                        
                        pairsProperty.serializedObject.ApplyModifiedProperties();
                        pairsProperty.serializedObject.Update();
                    }
                }
            }

            float yOffset = sizeRect.y + EditorGUIUtility.singleLineHeight + SPACING;

            if (!reorderableLists.ContainsKey(propertyPath))
            {
                reorderableLists[propertyPath] = new ReorderableList(property.serializedObject, pairsProperty, true, true, false, false)
                {
                    drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Items"),
                    footerHeight = 0f,
                    drawElementCallback = (rect, index, isActive, isFocused) =>
                    {
                        if (pairsProperty.arraySize <= index)
                            return;

                        var element = pairsProperty.GetArrayElementAtIndex(index);
                        var keyProperty = element.FindPropertyRelative("key");
                        var valueProperty = element.FindPropertyRelative("value");

                        var keyRect = new Rect(rect.x, rect.y, rect.width * 0.5f - SPACING, rect.height);
                        var valueRect = new Rect(keyRect.xMax + SPACING, rect.y, rect.width * 0.5f - BUTTON_WIDTH - SPACING, rect.height);
                        var buttonRect = new Rect(valueRect.xMax + SPACING, rect.y, BUTTON_WIDTH, rect.height);

                        if (isEnumKey)
                        {
                            // Enum 타입일 때 중복 키 체크
                            usedKeyIndices.Clear();
                            for (int i = 0; i < pairsProperty.arraySize; i++)
                            {
                                if (i == index) continue;
                                var otherElement = pairsProperty.GetArrayElementAtIndex(i);
                                var otherKeyProperty = otherElement.FindPropertyRelative("key");
                                usedKeyIndices.Add(otherKeyProperty.enumValueIndex);
                            }

                            EditorGUI.BeginChangeCheck();
                            EditorGUI.PropertyField(keyRect, keyProperty, GUIContent.none);
                            if (EditorGUI.EndChangeCheck())
                            {
                                if (usedKeyIndices.Contains(keyProperty.enumValueIndex))
                                {
                                    Debug.LogError($"중복 키 감지 '{keyProperty.enumValueIndex}'.");
                                    keyProperty.enumValueIndex = 0;
                                }
                            }
                        }
                        else
                        {
                            // 일반 타입일 때는 기본 PropertyField 사용
                            // DoubleColor는 커스텀 PropertyDrawer가 있으므로 자동으로 처리됨
                            EditorGUI.PropertyField(keyRect, keyProperty, GUIContent.none);
                        }

                        EditorGUI.PropertyField(valueRect, valueProperty, GUIContent.none);

                        // 삭제
                        if (GUI.Button(buttonRect, "-"))
                        {
                            pairsProperty.DeleteArrayElementAtIndex(index);
                            pairsProperty.serializedObject.ApplyModifiedProperties();
                        }
                    },
                    elementHeightCallback = index =>
                    {
                        float height = EditorGUIUtility.singleLineHeight;
                        if (index >= pairsProperty.arraySize) return height;

                        if (isEnumKey)
                        {
                            var element = pairsProperty.GetArrayElementAtIndex(index);
                            var keyProperty = element.FindPropertyRelative("key");
                            usedKeyIndices.Clear();
                            for (int i = 0; i < pairsProperty.arraySize; i++)
                            {
                                if (i == index) continue;
                                var otherElement = pairsProperty.GetArrayElementAtIndex(i);
                                var otherKeyProperty = otherElement.FindPropertyRelative("key");
                                usedKeyIndices.Add(otherKeyProperty.enumValueIndex);
                            }
                            if (usedKeyIndices.Contains(keyProperty.enumValueIndex))
                            {
                                height += EditorGUIUtility.singleLineHeight; // 중복 키 경고 메시지 높이 추가
                            }
                        }
                        else
                        {
                            // 값 타입이 DoubleColor인지 확인
                            var element = pairsProperty.GetArrayElementAtIndex(index);
                            var valueProperty = element.FindPropertyRelative("value");
                            if (valueProperty != null && valueProperty.propertyType == SerializedPropertyType.Generic && valueProperty.type == "DoubleColor")
                            {
                                // DoubleColorDrawer에서 사용하는 높이와 동일하게 맞춰줌
                                height = EditorGUIUtility.singleLineHeight + 20f + 5f + EditorGUIUtility.singleLineHeight + 20f + 5f;
                            }
                        }
                        return height;
                    }
                };
            }

            usedKeyIndices.Clear();

            var listRect = new Rect(position.x, yOffset, position.width, reorderableLists[propertyPath].GetHeight());
            reorderableLists[propertyPath].DoList(listRect);

            EditorGUI.indentLevel--;
        }

        property.serializedObject.ApplyModifiedProperties();
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;

        if (property.isExpanded)
        {
            var pairsProperty = property.FindPropertyRelative("keyValuePairs");
            height += EditorGUIUtility.singleLineHeight + SPACING;
            if (pairsProperty != null && pairsProperty.isArray)
            {
                string propertyPath = property.propertyPath;
                if (reorderableLists.TryGetValue(propertyPath, out var list))
                {
                    height += list.GetHeight();
                    height += BOTTOM_PADDING;
                }
            }
        }

        return height;
    }
}