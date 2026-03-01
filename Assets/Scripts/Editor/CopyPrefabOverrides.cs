using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;

namespace Editor
{
    /// <summary>
    /// Copies prefab overrides from Julia to Julia_Formal on scene.
    /// Handles the complex case where Julia has scene-specific modifications.
    /// </summary>
    public class CopyPrefabOverrides : EditorWindow
    {
        private GameObject _source;
        private GameObject _target;

        [MenuItem("Tools/Copy Julia Overrides to Julia_Formal")]
        public static void ShowWindow()
        {
            var window = GetWindow<CopyPrefabOverrides>("Copy Overrides");
            window.minSize = new Vector2(500, 400);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Copy Prefab Overrides", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool copies component values from Julia to Julia_Formal on the scene.\n\n" +
                "It will:\n" +
                "1. Find matching components by type\n" +
                "2. Copy serialized field values\n" +
                "3. Remap object references to Julia_Formal hierarchy\n\n" +
                "After copying, use Overrides → Apply All on Julia_Formal to save to prefab.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            _source = EditorGUILayout.ObjectField("Source (Julia)", _source, typeof(GameObject), true) as GameObject;
            _target = EditorGUILayout.ObjectField("Target (Julia_Formal)", _target, typeof(GameObject), true) as GameObject;

            EditorGUILayout.Space(10);

            // Auto-find buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Find Julia"))
            {
                _source = GameObject.Find("Julia");
                if (_source == null)
                    Debug.LogWarning("Julia not found on scene");
            }
            if (GUILayout.Button("Find Julia_Formal"))
            {
                _target = GameObject.Find("Julia_Formal");
                if (_target == null)
                    Debug.LogWarning("Julia_Formal not found on scene");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("COPY ALL COMPONENT VALUES", GUILayout.Height(40)))
            {
                CopyAllValues();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Fix _cargos list (remove nulls)"))
            {
                FixCargosList();
            }
        }

        private void CopyAllValues()
        {
            if (_source == null || _target == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign both Source and Target!", "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(_target, "Copy Julia values to Julia_Formal");

            // Build name-to-transform mappings
            var sourceTransforms = new Dictionary<string, Transform>();
            var targetTransforms = new Dictionary<string, Transform>();

            foreach (var t in _source.GetComponentsInChildren<Transform>(true))
            {
                string key = GetHierarchyKey(t, _source.transform);
                if (!sourceTransforms.ContainsKey(key))
                    sourceTransforms[key] = t;
                // Also by name only
                if (!sourceTransforms.ContainsKey(t.name))
                    sourceTransforms[t.name] = t;
            }

            foreach (var t in _target.GetComponentsInChildren<Transform>(true))
            {
                string key = GetHierarchyKey(t, _target.transform);
                if (!targetTransforms.ContainsKey(key))
                    targetTransforms[key] = t;
                if (!targetTransforms.ContainsKey(t.name))
                    targetTransforms[t.name] = t;
            }

            int copiedComponents = 0;
            int copiedFields = 0;

            // Get all MonoBehaviours
            var sourceComps = _source.GetComponentsInChildren<MonoBehaviour>(true).Where(c => c != null).ToList();
            var targetComps = _target.GetComponentsInChildren<MonoBehaviour>(true).Where(c => c != null).ToList();

            // Group by type and relative path
            var sourceByTypeAndPath = sourceComps.GroupBy(c => (c.GetType(), GetHierarchyKey(c.transform, _source.transform)));

            foreach (var group in sourceByTypeAndPath)
            {
                var (type, path) = group.Key;
                var sourceComp = group.First();

                // Find matching target component
                var targetComp = targetComps.FirstOrDefault(c =>
                    c.GetType() == type &&
                    GetHierarchyKey(c.transform, _target.transform) == path);

                if (targetComp == null)
                {
                    // Try by type only (for root level components)
                    targetComp = targetComps.FirstOrDefault(c => c.GetType() == type);
                }

                if (targetComp == null)
                    continue;

                // Copy using SerializedObject
                var sourceSO = new SerializedObject(sourceComp);
                var targetSO = new SerializedObject(targetComp);

                var prop = sourceSO.GetIterator();
                bool hasChanges = false;

                while (prop.NextVisible(true))
                {
                    // Skip script reference
                    if (prop.name == "m_Script")
                        continue;

                    var targetProp = targetSO.FindProperty(prop.propertyPath);
                    if (targetProp == null)
                        continue;

                    // Handle object references specially - remap to target hierarchy
                    if (prop.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        var sourceRef = prop.objectReferenceValue;
                        if (sourceRef == null)
                        {
                            if (targetProp.objectReferenceValue != null)
                            {
                                // Source is null, keep target's value if it has one
                                // (don't overwrite with null)
                            }
                            continue;
                        }

                        // Try to find equivalent in target
                        Object targetRef = null;

                        if (sourceRef is Transform sourceTransform)
                        {
                            targetRef = FindEquivalentTransform(sourceTransform, _source.transform, targetTransforms);
                        }
                        else if (sourceRef is GameObject sourceGo)
                        {
                            var equiv = FindEquivalentTransform(sourceGo.transform, _source.transform, targetTransforms);
                            if (equiv != null)
                                targetRef = equiv.gameObject;
                        }
                        else if (sourceRef is Component sourceComponent)
                        {
                            var equiv = FindEquivalentTransform(sourceComponent.transform, _source.transform, targetTransforms);
                            if (equiv != null)
                                targetRef = equiv.GetComponent(sourceComponent.GetType());
                        }

                        if (targetRef != null)
                        {
                            targetProp.objectReferenceValue = targetRef;
                            hasChanges = true;
                            copiedFields++;
                        }
                    }
                    else
                    {
                        // Copy primitive values directly
                        targetSO.CopyFromSerializedProperty(prop);
                        hasChanges = true;
                    }
                }

                if (hasChanges)
                {
                    targetSO.ApplyModifiedProperties();
                    EditorUtility.SetDirty(targetComp);
                    copiedComponents++;
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log($"Copied values from {copiedComponents} components, {copiedFields} object references remapped.");
            EditorUtility.DisplayDialog("Done",
                $"Copied {copiedComponents} components\n{copiedFields} references remapped\n\nNow use Overrides → Apply All on Julia_Formal to save.",
                "OK");
        }

        private Transform FindEquivalentTransform(Transform source, Transform sourceRoot, Dictionary<string, Transform> targetTransforms)
        {
            // Try by hierarchy path first
            string path = GetHierarchyKey(source, sourceRoot);
            if (targetTransforms.TryGetValue(path, out var result))
                return result;

            // Try by name mapping (for bone names)
            string mappedName = MapBoneName(source.name);
            if (targetTransforms.TryGetValue(mappedName, out result))
                return result;

            // Try by name only
            if (targetTransforms.TryGetValue(source.name, out result))
                return result;

            return null;
        }

        private string GetHierarchyKey(Transform t, Transform root)
        {
            var parts = new List<string>();
            var current = t;
            while (current != null && current != root)
            {
                parts.Insert(0, current.name);
                current = current.parent;
            }
            return string.Join("/", parts);
        }

        private string MapBoneName(string oldName)
        {
            var mapping = new Dictionary<string, string>
            {
                {"Root_M", "Root"},
                {"Hip_M", "Hips"},
                {"Spine1_M", "Abdomen"},
                {"Spine2_M", "Torso"},
                {"Chest_M", "Chest"},
                {"Neck_M", "Neck"},
                {"Head_M", "Head"},
                {"Scapula_L", "Shoulder.L"},
                {"Shoulder_L", "UpperArm.L"},
                {"Elbow_L", "LowerArm.L"},
                {"Wrist_L", "Wrist.L"},
                {"Scapula_R", "Shoulder.R"},
                {"Shoulder_R", "UpperArm.R"},
                {"Elbow_R", "LowerArm.R"},
                {"Wrist_R", "Wrist.R"},
            };

            return mapping.TryGetValue(oldName, out var mapped) ? mapped : oldName;
        }

        private void FixCargosList()
        {
            if (_target == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign Target!", "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(_target, "Fix cargos list");

            foreach (var comp in _target.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (comp == null) continue;
                if (comp.GetType().Name != "CharacterInventory") continue;

                var so = new SerializedObject(comp);
                var cargosProp = so.FindProperty("_cargos");

                if (cargosProp == null || !cargosProp.isArray)
                    continue;

                // Remove null elements from end
                int validCount = 0;
                for (int i = 0; i < cargosProp.arraySize; i++)
                {
                    var elem = cargosProp.GetArrayElementAtIndex(i);
                    if (elem.objectReferenceValue != null)
                        validCount = i + 1;
                }

                if (validCount < cargosProp.arraySize)
                {
                    cargosProp.arraySize = validCount;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(comp);
                    Debug.Log($"Fixed _cargos: reduced from {cargosProp.arraySize} to {validCount} elements");
                }
            }

            EditorUtility.DisplayDialog("Done", "Fixed _cargos list", "OK");
        }
    }
}
