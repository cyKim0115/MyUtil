using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DoubleColor))]
public class DoubleColorDrawer : PropertyDrawer
{
    private const float SPACING = 5f;
    private const float COLOR_FIELD_HEIGHT = 20f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // 라벨 그리기
        var labelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(labelRect, label);

        // Top Color
        var topLabelRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + SPACING, 60f, EditorGUIUtility.singleLineHeight);
        var topColorRect = new Rect(topLabelRect.xMax + SPACING, topLabelRect.y, position.width - 60f - SPACING, COLOR_FIELD_HEIGHT);
        
        EditorGUI.LabelField(topLabelRect, "Top");
        var topColorProperty = property.FindPropertyRelative("_top");
        EditorGUI.PropertyField(topColorRect, topColorProperty, GUIContent.none);

        // Bottom Color
        var bottomLabelRect = new Rect(position.x, topLabelRect.y + COLOR_FIELD_HEIGHT + SPACING, 60f, EditorGUIUtility.singleLineHeight);
        var bottomColorRect = new Rect(bottomLabelRect.xMax + SPACING, bottomLabelRect.y, position.width - 60f - SPACING, COLOR_FIELD_HEIGHT);
        
        EditorGUI.LabelField(bottomLabelRect, "Bottom");
        var bottomColorProperty = property.FindPropertyRelative("_bottom");
        EditorGUI.PropertyField(bottomColorRect, bottomColorProperty, GUIContent.none);

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight + COLOR_FIELD_HEIGHT + SPACING + EditorGUIUtility.singleLineHeight + COLOR_FIELD_HEIGHT + SPACING;
    }
} 