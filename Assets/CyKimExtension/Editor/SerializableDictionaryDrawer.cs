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

            // 렉트들 사이즈
            var sizeRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + SPACING, position.width, EditorGUIUtility.singleLineHeight);
            var sizeLabelRect = new Rect(sizeRect.x, sizeRect.y, 40f, sizeRect.height);
            var sizeFieldRect = new Rect(sizeLabelRect.xMax + SPACING, sizeRect.y, sizeRect.width * 0.3f - 40f - SPACING, sizeRect.height);
            var spaceRect = new Rect(sizeFieldRect.xMax + SPACING, sizeRect.y, sizeRect.width * 0.1f, sizeRect.height);
            var enumRect = new Rect(spaceRect.xMax + SPACING, sizeRect.y, sizeRect.width * 0.6f - SPACING, sizeRect.height);

            var sizeProperty = pairsProperty.FindPropertyRelative("Array.size");
            EditorGUI.LabelField(sizeLabelRect, "Size");

            // 어두운 박스 배경 안깔림 ㅡㅡ;
            // var box = new GUIStyle(GUI.skin.box);
            // EditorGUILayout.BeginHorizontal(box);
            EditorGUI.BeginChangeCheck();
            EditorGUI.LabelField(sizeFieldRect, sizeProperty.intValue.ToString(), EditorStyles.boldLabel);
            if (EditorGUI.EndChangeCheck())
            {
                pairsProperty.serializedObject.ApplyModifiedProperties();
                pairsProperty.serializedObject.Update();
            }
            // EditorGUILayout.EndHorizontal();

            string propertyPath = property.propertyPath;
            newEnumValues.TryAdd(propertyPath, -99);

            if (pairsProperty.arraySize == 0)
            {
                // 리스트가 비어 있을 경우 버튼 표시
                if (GUI.Button(enumRect, "Add First Pair"))
                {
                    pairsProperty.arraySize++;
                    var elementProperty = pairsProperty.GetArrayElementAtIndex(pairsProperty.arraySize - 1);
                    var keyProperty = elementProperty.FindPropertyRelative("key");
                    keyProperty.enumValueIndex = 0;
                    pairsProperty.serializedObject.ApplyModifiedProperties();
                    pairsProperty.serializedObject.Update();
                }
            }
            else
            {
                var firstKeyProperty = pairsProperty.GetArrayElementAtIndex(0).FindPropertyRelative("key");
                var displayNamesWithDefault = new string[firstKeyProperty.enumDisplayNames.Length + 1];
                displayNamesWithDefault[0] = "-";
                Array.Copy(firstKeyProperty.enumDisplayNames, 0, displayNamesWithDefault, 1, firstKeyProperty.enumDisplayNames.Length);
                EditorGUI.BeginChangeCheck();
                
                int selectedIndex = EditorGUI.Popup(enumRect, newEnumValues[propertyPath] == -99 ? 0 : newEnumValues[propertyPath] + 1, displayNamesWithDefault);
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

                        // 중복 키 체크
                        usedKeyIndices.Clear();
                        for (int i = 0; i < pairsProperty.arraySize; i++)
                        {
                            if (i == index) continue;
                            var otherElement = pairsProperty.GetArrayElementAtIndex(i);
                            var otherKeyProperty = otherElement.FindPropertyRelative("key");
                            usedKeyIndices.Add(otherKeyProperty.enumValueIndex);
                        }

                        var keyRect = new Rect(rect.x, rect.y, rect.width * 0.5f - SPACING, rect.height);
                        var valueRect = new Rect(keyRect.xMax + SPACING, rect.y, rect.width * 0.5f - BUTTON_WIDTH - SPACING, rect.height);
                        var buttonRect = new Rect(valueRect.xMax + SPACING, rect.y, BUTTON_WIDTH, rect.height);

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