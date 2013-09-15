using UnityEditor;
using UnityEngine;

public class UnityDictionaryDrawer<TKey, TValue> : PropertyDrawer
{
    override public void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var propTitle = EditorGUI.BeginProperty(position, label, property);

        // PropertyDrawers don't support GUILayout so we have to construct our own rectangles
        var foldoutRect = new Rect(position.left, position.top, position.width, base.GetPropertyHeight(property, label));
        bool wasExpanded = property.isExpanded;
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, propTitle);

        // Don't use the latest isExpanded value because if it's changed then we're the wrong height
        if(wasExpanded)
        {
            
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var total = base.GetPropertyHeight(property, label);
        if(property.isExpanded)
        {
            total += base.GetPropertyHeight(property, label) * property.FindPropertyRelative("_keys").arraySize;
        }

        return total;
    }
}

//TODO: figure out how to make it so you dont need to have one per dummy subclass.

[CustomPropertyDrawer(typeof(UnityDictionaryIntString))]
public class UnityDictionaryDrawerIntString : UnityDictionaryDrawer<int, string>
{
    
}