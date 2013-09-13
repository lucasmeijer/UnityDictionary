using UnityEditor;
using UnityEngine;

public class UnityDictionaryDrawer<TKey, TValue> : PropertyDrawer
{
    override public void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var keysProperty = property.FindPropertyRelative("_keys");
        var valuesProperty = property.FindPropertyRelative("_values");

        for(int i = 0; i < keysProperty.arraySize; ++i)
        {
            var keyProperty = keysProperty.GetArrayElementAtIndex(i);
            var valueProperty = valuesProperty.GetArrayElementAtIndex(i);

            var subRect = position;
            subRect.xMax -= position.width/2;
            EditorGUI.PropertyField(subRect, keyProperty);

            subRect.x += position.width/2;
            EditorGUI.PropertyField(subRect, valueProperty);
        }

        EditorGUI.EndProperty();
    }
}

//TODO: figure out how to make it so you dont need to have one per dummy subclass.

[CustomPropertyDrawer(typeof(UnityDictionaryIntString))]
public class UnityDictionaryDrawerIntString : UnityDictionaryDrawer<int, string>
{
    
}