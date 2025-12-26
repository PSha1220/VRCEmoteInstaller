#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ModularEmoteTransitionSettings.Condition))]
public class ModularEmoteTransitionSettingsConditionDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        position = EditorGUI.IndentedRect(position);
        float vSpace = EditorGUIUtility.standardVerticalSpacing;

        var propParameter = property.FindPropertyRelative("parameter");
        var propType = property.FindPropertyRelative("type");
        var propBoolValue = property.FindPropertyRelative("boolValue");
        var propIntComparison = property.FindPropertyRelative("intComparison");
        var propIntValue = property.FindPropertyRelative("intValue");
        var propFloatComparison = property.FindPropertyRelative("floatComparison");
        var propFloatValue = property.FindPropertyRelative("floatValue");

        float y = position.y;
        float width = position.width;

        float h = EditorGUI.GetPropertyHeight(propParameter, true);
        var rect = new Rect(position.x, y, width, h);
        EditorGUI.PropertyField(rect, propParameter, new GUIContent("Parameter"), true);
        y += h + vSpace;

        h = EditorGUI.GetPropertyHeight(propType, true);
        rect = new Rect(position.x, y, width, h);
        EditorGUI.PropertyField(rect, propType, new GUIContent("Type"), true);
        y += h + vSpace;

        var type = (ModularEmoteTransitionSettings.Condition.ParameterType)propType.enumValueIndex;

        switch (type)
        {
            case ModularEmoteTransitionSettings.Condition.ParameterType.Bool:
                h = EditorGUI.GetPropertyHeight(propBoolValue, true);
                rect = new Rect(position.x, y, width, h);
                EditorGUI.PropertyField(rect, propBoolValue, new GUIContent("Bool Value"), true);
                y += h + vSpace;
                break;

            case ModularEmoteTransitionSettings.Condition.ParameterType.Int:
                h = EditorGUI.GetPropertyHeight(propIntComparison, true);
                rect = new Rect(position.x, y, width, h);
                EditorGUI.PropertyField(rect, propIntComparison, new GUIContent("Int Comparison"), true);
                y += h + vSpace;

                h = EditorGUI.GetPropertyHeight(propIntValue, true);
                rect = new Rect(position.x, y, width, h);
                EditorGUI.PropertyField(rect, propIntValue, new GUIContent("Int Value"), true);
                y += h + vSpace;
                break;

            case ModularEmoteTransitionSettings.Condition.ParameterType.Float:
                h = EditorGUI.GetPropertyHeight(propFloatComparison, true);
                rect = new Rect(position.x, y, width, h);
                EditorGUI.PropertyField(rect, propFloatComparison, new GUIContent("Float Comparison"), true);
                y += h + vSpace;

                h = EditorGUI.GetPropertyHeight(propFloatValue, true);
                rect = new Rect(position.x, y, width, h);
                EditorGUI.PropertyField(rect, propFloatValue, new GUIContent("Float Value"), true);
                y += h + vSpace;
                break;

            case ModularEmoteTransitionSettings.Condition.ParameterType.Trigger:
                break;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float vSpace = EditorGUIUtility.standardVerticalSpacing;
        float totalHeight = 0f;
        int lineCount = 0;

        var propParameter = property.FindPropertyRelative("parameter");
        var propType = property.FindPropertyRelative("type");
        var propBoolValue = property.FindPropertyRelative("boolValue");
        var propIntComparison = property.FindPropertyRelative("intComparison");
        var propIntValue = property.FindPropertyRelative("intValue");
        var propFloatComparison = property.FindPropertyRelative("floatComparison");
        var propFloatValue = property.FindPropertyRelative("floatValue");

        totalHeight += EditorGUI.GetPropertyHeight(propParameter, true);
        lineCount++;

        totalHeight += EditorGUI.GetPropertyHeight(propType, true);
        lineCount++;

        var type = (ModularEmoteTransitionSettings.Condition.ParameterType)propType.enumValueIndex;

        switch (type)
        {
            case ModularEmoteTransitionSettings.Condition.ParameterType.Bool:
                totalHeight += EditorGUI.GetPropertyHeight(propBoolValue, true);
                lineCount++;
                break;

            case ModularEmoteTransitionSettings.Condition.ParameterType.Int:
                totalHeight += EditorGUI.GetPropertyHeight(propIntComparison, true);
                totalHeight += EditorGUI.GetPropertyHeight(propIntValue, true);
                lineCount += 2;
                break;

            case ModularEmoteTransitionSettings.Condition.ParameterType.Float:
                totalHeight += EditorGUI.GetPropertyHeight(propFloatComparison, true);
                totalHeight += EditorGUI.GetPropertyHeight(propFloatValue, true);
                lineCount += 2;
                break;

            case ModularEmoteTransitionSettings.Condition.ParameterType.Trigger:
                break;
        }

        if (lineCount > 1)
        {
            totalHeight += (lineCount - 1) * vSpace;
        }

        return totalHeight;
    }
}
#endif
