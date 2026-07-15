using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// F12: 선택 오브젝트 공통 컴포넌트에 대해 TMP_Text 텍스트 입력 포커스 / Image Source Image 선택 창을 연다.
/// </summary>
public static class InspectorComponentShortcut
{
    private const string TmpTextControlName = "InspectorComponentShortcut_TMPText";

    private static bool _focusTmpTextRequested;
    private static SerializedObject _spriteSelectorSerializedObject;
    private static SerializedProperty _spriteSelectorProperty;

    [Shortcut("CyKimExtension/Inspector Component Shortcut", KeyCode.F12)]
    private static void OnShortcut()
    {
        Execute();
    }

    private static void Execute()
    {
        GameObject[] selected = Selection.gameObjects;
        if (selected == null || selected.Length == 0)
            return;

        if (AllHaveComponent<TMP_Text>(selected))
        {
            FocusTmpTextInput();
            return;
        }

        if (AllHaveComponent<Image>(selected))
        {
            OpenImageSpriteSelector(selected);
        }
    }

    private static bool AllHaveComponent<T>(GameObject[] selected) where T : Component
    {
        for (int i = 0; i < selected.Length; i++)
        {
            if (!selected[i].TryGetComponent<T>(out _))
                return false;
        }

        return true;
    }

    private static void FocusTmpTextInput()
    {
        _focusTmpTextRequested = true;
        FocusInspectorWindow();
        ActiveEditorTracker.sharedTracker.ForceRebuild();
        InternalEditorUtility.RepaintAllViews();
    }

    internal static bool ConsumeTmpTextFocusRequest()
    {
        if (!_focusTmpTextRequested)
            return false;

        // Layout/Repaint 모두에서 포커스를 시도하고, Repaint에서만 요청을 소모한다.
        if (Event.current != null && Event.current.type == EventType.Repaint)
            _focusTmpTextRequested = false;

        return true;
    }

    internal static string GetTmpTextControlName()
    {
        return TmpTextControlName;
    }

    private static void FocusInspectorWindow()
    {
        Type inspectorType = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
        if (inspectorType == null)
            return;

        EditorWindow.FocusWindowIfItsOpen(inspectorType);
    }

    private static void OpenImageSpriteSelector(GameObject[] selected)
    {
        var images = new Image[selected.Length];
        for (int i = 0; i < selected.Length; i++)
            images[i] = selected[i].GetComponent<Image>();

        _spriteSelectorSerializedObject = new SerializedObject(images);
        _spriteSelectorProperty = _spriteSelectorSerializedObject.FindProperty("m_Sprite");
        if (_spriteSelectorProperty == null)
            return;

        Type objectSelectorType = Type.GetType("UnityEditor.ObjectSelector, UnityEditor.CoreModule");
        if (objectSelectorType == null)
            return;

        PropertyInfo getProperty = objectSelectorType.GetProperty("get", BindingFlags.Public | BindingFlags.Static);
        object selector = getProperty?.GetValue(null);
        if (selector == null)
            return;

        MethodInfo showMethod = FindShowMethod(objectSelectorType);
        if (showMethod == null)
            return;

        ParameterInfo[] parameters = showMethod.GetParameters();
        object[] args = new object[parameters.Length];
        args[0] = typeof(Sprite);
        args[1] = _spriteSelectorProperty;
        args[2] = false;
        for (int i = 3; i < parameters.Length; i++)
        {
            if (parameters[i].ParameterType == typeof(Action<UnityEngine.Object>))
            {
                if (parameters[i].Name.IndexOf("Closed", StringComparison.OrdinalIgnoreCase) >= 0)
                    args[i] = (Action<UnityEngine.Object>)OnSpriteSelectorClosed;
                else
                    args[i] = (Action<UnityEngine.Object>)OnSpriteSelectedUpdated;
            }
            else
            {
                args[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : null;
            }
        }

        showMethod.Invoke(selector, args);
    }

    private static void OnSpriteSelectedUpdated(UnityEngine.Object selectedObject)
    {
        ApplySelectedSprite(selectedObject);
    }

    private static void OnSpriteSelectorClosed(UnityEngine.Object selectedObject)
    {
        // Cancel 시 ObjectSelector가 Undo로 원복한 뒤에도 Closed 콜백이 오므로 재적용하지 않는다.
        if (!IsObjectSelectorCancelled())
            ApplySelectedSprite(selectedObject);

        _spriteSelectorProperty = null;
        _spriteSelectorSerializedObject = null;
    }

    private static bool IsObjectSelectorCancelled()
    {
        Type objectSelectorType = Type.GetType("UnityEditor.ObjectSelector, UnityEditor.CoreModule");
        MethodInfo selectionCanceled = objectSelectorType?.GetMethod(
            "SelectionCanceled",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (selectionCanceled == null)
            return false;

        return (bool)selectionCanceled.Invoke(null, null);
    }

    private static void ApplySelectedSprite(UnityEngine.Object selectedObject)
    {
        if (_spriteSelectorSerializedObject == null || _spriteSelectorProperty == null)
            return;

        _spriteSelectorSerializedObject.Update();
        _spriteSelectorProperty.objectReferenceValue = selectedObject;
        _spriteSelectorSerializedObject.ApplyModifiedProperties();
        InternalEditorUtility.RepaintAllViews();
    }

    private static MethodInfo FindShowMethod(Type objectSelectorType)
    {
        MethodInfo[] methods = objectSelectorType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo method = methods[i];
            if (method.Name != "Show")
                continue;

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length < 3)
                continue;

            if (parameters[0].ParameterType == typeof(Type)
                && parameters[1].ParameterType == typeof(SerializedProperty)
                && parameters[2].ParameterType == typeof(bool))
            {
                return method;
            }
        }

        return null;
    }
}

internal static class TmpTextInputFocusDrawer
{
    public static void Draw(
        SerializedProperty textProp,
        SerializedProperty isRightToLeftProp,
        SerializedProperty parentLinkedTextComponentProp,
        SerializedProperty textStyleHashCodeProp,
        TMP_Text textComponent,
        ref string rtlText,
        GUIContent[] styleNames,
        List<TMP_Style> styles,
        Dictionary<int, int> textStyleIndexLookup,
        ref int styleSelectionIndex,
        ref bool havePropertiesChanged,
        bool focus)
    {
        EditorGUILayout.Space();

        Rect rect = EditorGUILayout.GetControlRect(false, 22);
        GUI.Label(rect, new GUIContent("<b>Text Input</b>"), TMP_UIStyleManager.sectionHeader);

        EditorGUI.indentLevel = 0;

        if (parentLinkedTextComponentProp.objectReferenceValue != null)
        {
            EditorGUILayout.HelpBox("The Text Input Box is disabled due to this text component being linked to another.", MessageType.Info);
            return;
        }

        float labelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 110f;
        isRightToLeftProp.boolValue = EditorGUI.Toggle(
            new Rect(rect.width - 120, rect.y + 3, 130, 20),
            new GUIContent("Enable RTL Editor", "Reverses text direction and allows right to left editing."),
            isRightToLeftProp.boolValue);
        EditorGUIUtility.labelWidth = labelWidth;

        EditorGUI.BeginChangeCheck();
        float textHeight = EditorGUI.GetPropertyHeight(textProp);
        Rect textRect = EditorGUILayout.GetControlRect(false, textHeight);
        if (focus)
            GUI.SetNextControlName(InspectorComponentShortcut.GetTmpTextControlName());

        textProp.stringValue = EditorGUI.TextArea(textRect, textProp.stringValue ?? string.Empty);

        if (focus)
        {
            EditorGUI.FocusTextInControl(InspectorComponentShortcut.GetTmpTextControlName());
            EditorGUIUtility.editingTextField = true;
        }

        if (EditorGUI.EndChangeCheck() && textProp.stringValue != textComponent.text)
            havePropertiesChanged = true;

        if (isRightToLeftProp.boolValue)
        {
            rtlText = string.Empty;
            string sourceText = textProp.stringValue;
            for (int i = 0; i < sourceText.Length; i++)
                rtlText += sourceText[sourceText.Length - i - 1];

            GUILayout.Label("RTL Text Input");

            EditorGUI.BeginChangeCheck();
            rtlText = EditorGUILayout.TextArea(
                rtlText,
                TMP_UIStyleManager.wrappingTextArea,
                GUILayout.Height(textHeight - EditorGUIUtility.singleLineHeight),
                GUILayout.ExpandWidth(true));

            if (EditorGUI.EndChangeCheck())
            {
                sourceText = string.Empty;
                for (int i = 0; i < rtlText.Length; i++)
                    sourceText += rtlText[rtlText.Length - i - 1];

                textProp.stringValue = sourceText;
            }
        }

        if (styleNames == null)
            return;

        rect = EditorGUILayout.GetControlRect(false, 17);
        var styleLabel = new GUIContent("Text Style", "The style from a style sheet to be applied to the text.");
        EditorGUI.BeginProperty(rect, styleLabel, textStyleHashCodeProp);

        textStyleIndexLookup.TryGetValue(textStyleHashCodeProp.intValue, out styleSelectionIndex);

        EditorGUI.BeginChangeCheck();
        styleSelectionIndex = EditorGUI.Popup(rect, styleLabel, styleSelectionIndex, styleNames);
        if (EditorGUI.EndChangeCheck())
        {
            textStyleHashCodeProp.intValue = styles[styleSelectionIndex].hashCode;
            textComponent.textStyle = styles[styleSelectionIndex];
            havePropertiesChanged = true;
        }

        EditorGUI.EndProperty();
    }
}

[CustomEditor(typeof(TextMeshProUGUI), true)]
[CanEditMultipleObjects]
public class TextMeshProUGUIInspectorShortcutEditor : TMP_EditorPanelUI
{
    public override void OnInspectorGUI()
    {
        if (IsMixSelectionTypes())
            return;

        serializedObject.Update();

        bool focus = InspectorComponentShortcut.ConsumeTmpTextFocusRequest();
        TmpTextInputFocusDrawer.Draw(
            m_TextProp,
            m_IsRightToLeftProp,
            m_ParentLinkedTextComponentProp,
            m_TextStyleHashCodeProp,
            m_TextComponent,
            ref m_RtlText,
            m_StyleNames,
            m_Styles,
            m_TextStyleIndexLookup,
            ref m_StyleSelectionIndex,
            ref m_HavePropertiesChanged,
            focus);

        DrawMainSettings();
        DrawExtraSettings();
        EditorGUILayout.Space();

        if (serializedObject.ApplyModifiedProperties() || m_HavePropertiesChanged)
        {
            m_TextComponent.havePropertiesChanged = true;
            m_HavePropertiesChanged = false;
        }
    }
}

[CustomEditor(typeof(TextMeshPro), true)]
[CanEditMultipleObjects]
public class TextMeshProInspectorShortcutEditor : TMP_EditorPanel
{
    public override void OnInspectorGUI()
    {
        if (IsMixSelectionTypes())
            return;

        serializedObject.Update();

        bool focus = InspectorComponentShortcut.ConsumeTmpTextFocusRequest();
        TmpTextInputFocusDrawer.Draw(
            m_TextProp,
            m_IsRightToLeftProp,
            m_ParentLinkedTextComponentProp,
            m_TextStyleHashCodeProp,
            m_TextComponent,
            ref m_RtlText,
            m_StyleNames,
            m_Styles,
            m_TextStyleIndexLookup,
            ref m_StyleSelectionIndex,
            ref m_HavePropertiesChanged,
            focus);

        DrawMainSettings();
        DrawExtraSettings();
        EditorGUILayout.Space();

        if (serializedObject.ApplyModifiedProperties() || m_HavePropertiesChanged)
        {
            m_TextComponent.havePropertiesChanged = true;
            m_HavePropertiesChanged = false;
        }
    }
}
