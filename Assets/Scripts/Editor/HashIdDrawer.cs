using UnityEditor;
using UnityEngine;
using Utility;

namespace Editor
{
    [CustomPropertyDrawer(typeof(HashId))]
    public class HashIdDrawer : PropertyDrawer
    {
        private const float SubLabelWidth = 40f;
        private const float Spacing = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var foldoutRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            var idProp = property.FindPropertyRelative("_id");
            var hashProp = property.FindPropertyRelative("_hash");

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                var lineHeight = EditorGUIUtility.singleLineHeight;
                var fieldWidth = position.width - EditorGUIUtility.labelWidth;

                // Id field
                var idRect = new Rect(position.x, position.y + lineHeight + Spacing, position.width, lineHeight);
                EditorGUI.BeginChangeCheck();
                var newId = EditorGUI.TextField(idRect, "Id", idProp.stringValue);
                if (EditorGUI.EndChangeCheck())
                {
                    idProp.stringValue = newId;
                    hashProp.intValue = HashUtil.StringToHash(newId);
                }

                // Hash field
                var hashRect = new Rect(position.x, position.y + (lineHeight + Spacing) * 2, position.width, lineHeight);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.IntField(hashRect, "Hash", hashProp.intValue);
                EditorGUI.EndDisabledGroup();

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.isExpanded)
                return EditorGUIUtility.singleLineHeight * 3 + Spacing * 2;
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
