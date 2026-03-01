using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Editor
{
    /// <summary>
    /// Deep comparison of Julia and Julia_Formal with full bone mapping support.
    /// Analyzes every component, every field, and provides detailed fix options.
    /// </summary>
    public class DeepCharacterComparer : EditorWindow
    {
        private GameObject _julia;
        private GameObject _juliaFormal;
        private Vector2 _scrollPosition;
        private List<DiffItem> _diffs = new List<DiffItem>();
        private bool _showAll = false;
        private string _filterComponent = "";

        // Bone mapping: Julia (TS_Character_Rig) -> Formal
        private static readonly Dictionary<string, string> BoneMapping = new Dictionary<string, string>
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
            {"Hip_L", "UpperLeg.L"},
            {"Knee_L", "LowerLeg.L"},
            {"Ankle_L", "Foot.L"},
            {"Toes_L", "Toes.L"},
            {"Hip_R", "UpperLeg.R"},
            {"Knee_R", "LowerLeg.R"},
            {"Ankle_R", "Foot.R"},
            {"Toes_R", "Toes.R"},
            // Common non-bone objects
            {"DeformationSystem", "CharacterArmature"},
            {"INVENTORY", "INVENTORY"},
            {"Cargo_DollarPacks", "Cargo_DollarPacks"},
            {"Cargo_Tomato", "Cargo_Tomato"},
        };

        private class DiffItem
        {
            public string ComponentPath;
            public string ComponentType;
            public string FieldPath;
            public string JuliaValue;
            public string FormalValue;
            public DiffType Type;
            public bool CanAutoFix;
            public Action FixAction;
            public SerializedProperty FormalProperty;
            public object ResolvedValue;
        }

        private enum DiffType
        {
            Same,
            NullInFormal,      // Julia has value, Formal is null - CRITICAL
            NullInJulia,       // Formal has value, Julia is null
            DifferentValue,    // Both have different values
            DifferentCount,    // Array size different
            MissingInFormal,   // Component exists in Julia but not Formal
            ExtraInFormal,     // Component exists in Formal but not Julia
        }

        [MenuItem("Tools/Deep Character Compare")]
        public static void ShowWindow()
        {
            var window = GetWindow<DeepCharacterComparer>("Deep Compare");
            window.minSize = new Vector2(1000, 700);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Deep Character Comparison", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Object fields
            EditorGUILayout.BeginHorizontal();
            _julia = EditorGUILayout.ObjectField("Julia (Working)", _julia, typeof(GameObject), true) as GameObject;
            if (GUILayout.Button("Find", GUILayout.Width(50)))
                _julia = GameObject.Find("Julia");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _juliaFormal = EditorGUILayout.ObjectField("Julia_Formal (Target)", _juliaFormal, typeof(GameObject), true) as GameObject;
            if (GUILayout.Button("Find", GUILayout.Width(50)))
                _juliaFormal = GameObject.Find("Julia_Formal");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Controls
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("ANALYZE", GUILayout.Height(30)))
            {
                Analyze();
            }

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("FIX ALL PROBLEMS", GUILayout.Height(30)))
            {
                FixAllProblems();
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("Export Report", GUILayout.Height(30)))
            {
                ExportReport();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Filters
            EditorGUILayout.BeginHorizontal();
            _showAll = EditorGUILayout.Toggle("Show All (including same)", _showAll);
            _filterComponent = EditorGUILayout.TextField("Filter Component:", _filterComponent);
            EditorGUILayout.EndHorizontal();

            // Statistics
            if (_diffs.Count > 0)
            {
                var critical = _diffs.Count(d => d.Type == DiffType.NullInFormal);
                var different = _diffs.Count(d => d.Type == DiffType.DifferentValue);
                var countDiff = _diffs.Count(d => d.Type == DiffType.DifferentCount);
                var fixable = _diffs.Count(d => d.CanAutoFix);

                EditorGUILayout.HelpBox(
                    $"Analysis Results:\n" +
                    $"  CRITICAL (null in Formal): {critical}\n" +
                    $"  Different values: {different}\n" +
                    $"  Different array sizes: {countDiff}\n" +
                    $"  Can auto-fix: {fixable}",
                    critical > 0 ? MessageType.Error : MessageType.Info);
            }

            EditorGUILayout.Space(5);

            // Results
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            string currentComp = "";
            foreach (var diff in _diffs)
            {
                if (!_showAll && diff.Type == DiffType.Same)
                    continue;

                if (!string.IsNullOrEmpty(_filterComponent) &&
                    !diff.ComponentType.ToLower().Contains(_filterComponent.ToLower()))
                    continue;

                // Component header
                string compKey = $"{diff.ComponentPath}|{diff.ComponentType}";
                if (compKey != currentComp)
                {
                    currentComp = compKey;
                    EditorGUILayout.Space(10);
                    EditorGUILayout.LabelField($"[{diff.ComponentType}] {diff.ComponentPath}", EditorStyles.boldLabel);
                }

                // Diff item
                Color bgColor = diff.Type switch
                {
                    DiffType.NullInFormal => new Color(1f, 0.5f, 0.5f),
                    DiffType.DifferentValue => new Color(1f, 0.9f, 0.5f),
                    DiffType.DifferentCount => new Color(1f, 0.8f, 0.6f),
                    DiffType.MissingInFormal => new Color(1f, 0.6f, 0.6f),
                    _ => new Color(0.9f, 0.9f, 0.9f)
                };

                GUI.backgroundColor = bgColor;
                EditorGUILayout.BeginHorizontal("box");
                GUI.backgroundColor = Color.white;

                // Field name and type
                EditorGUILayout.LabelField(diff.FieldPath, GUILayout.Width(200));
                EditorGUILayout.LabelField($"[{diff.Type}]", GUILayout.Width(120));

                // Values
                EditorGUILayout.LabelField($"Julia: {Truncate(diff.JuliaValue, 30)}", GUILayout.Width(250));
                EditorGUILayout.LabelField($"Formal: {Truncate(diff.FormalValue, 30)}", GUILayout.Width(250));

                // Fix button
                if (diff.CanAutoFix && diff.FixAction != null)
                {
                    if (GUILayout.Button("Fix", GUILayout.Width(50)))
                    {
                        diff.FixAction();
                        Analyze(); // Re-analyze after fix
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void Analyze()
        {
            _diffs.Clear();

            if (_julia == null || _juliaFormal == null)
            {
                Debug.LogError("Please assign both objects!");
                return;
            }

            // Build transform lookups
            var juliaTransforms = BuildTransformLookup(_julia.transform);
            var formalTransforms = BuildTransformLookup(_juliaFormal.transform);

            Debug.Log($"Julia transforms: {juliaTransforms.Count}, Formal transforms: {formalTransforms.Count}");

            // Get all components
            var juliaComps = _julia.GetComponentsInChildren<Component>(true)
                .Where(c => c != null && !(c is Transform))
                .ToList();

            var formalComps = _juliaFormal.GetComponentsInChildren<Component>(true)
                .Where(c => c != null && !(c is Transform))
                .ToList();

            // Group Julia components by type and normalized path
            var juliaByKey = new Dictionary<string, Component>();
            foreach (var c in juliaComps)
            {
                string key = GetComponentKey(c, _julia.transform);
                if (!juliaByKey.ContainsKey(key))
                    juliaByKey[key] = c;
            }

            // Group Formal components
            var formalByKey = new Dictionary<string, Component>();
            foreach (var c in formalComps)
            {
                string key = GetComponentKey(c, _juliaFormal.transform);
                if (!formalByKey.ContainsKey(key))
                    formalByKey[key] = c;
            }

            // Compare each Julia component with its Formal equivalent
            foreach (var kvp in juliaByKey)
            {
                var juliaComp = kvp.Value;
                var key = kvp.Key;

                if (!formalByKey.TryGetValue(key, out var formalComp))
                {
                    _diffs.Add(new DiffItem
                    {
                        ComponentPath = GetPath(juliaComp.transform, _julia.transform),
                        ComponentType = juliaComp.GetType().Name,
                        FieldPath = "(entire component)",
                        JuliaValue = "EXISTS",
                        FormalValue = "MISSING",
                        Type = DiffType.MissingInFormal
                    });
                    continue;
                }

                CompareComponents(juliaComp, formalComp, juliaTransforms, formalTransforms);
            }

            // Sort: critical first
            _diffs = _diffs
                .OrderBy(d => d.Type == DiffType.Same ? 99 : (int)d.Type)
                .ThenBy(d => d.ComponentType)
                .ThenBy(d => d.FieldPath)
                .ToList();

            Debug.Log($"Analysis complete. Found {_diffs.Count} items, {_diffs.Count(d => d.Type == DiffType.NullInFormal)} critical.");
        }

        private void CompareComponents(Component juliaComp, Component formalComp,
            Dictionary<string, Transform> juliaTransforms, Dictionary<string, Transform> formalTransforms)
        {
            var type = juliaComp.GetType();
            string compPath = GetPath(formalComp.transform, _juliaFormal.transform);

            var juliaSO = new SerializedObject(juliaComp);
            var formalSO = new SerializedObject(formalComp);

            var prop = juliaSO.GetIterator();
            while (prop.NextVisible(true))
            {
                if (prop.name == "m_Script" || prop.name == "m_ObjectHideFlags")
                    continue;

                var formalProp = formalSO.FindProperty(prop.propertyPath);
                if (formalProp == null)
                    continue;

                CompareProp(prop, formalProp, compPath, type.Name, juliaTransforms, formalTransforms, formalSO);
            }
        }

        private void CompareProp(SerializedProperty juliaProp, SerializedProperty formalProp,
            string compPath, string compType,
            Dictionary<string, Transform> juliaTransforms, Dictionary<string, Transform> formalTransforms,
            SerializedObject formalSO)
        {
            string juliaVal = PropToString(juliaProp);
            string formalVal = PropToString(formalProp);

            DiffType diffType = DiffType.Same;
            bool canFix = false;
            Action fixAction = null;
            object resolvedValue = null;

            // Check for differences
            if (juliaProp.propertyType == SerializedPropertyType.ObjectReference)
            {
                var juliaRef = juliaProp.objectReferenceValue;
                var formalRef = formalProp.objectReferenceValue;

                bool juliaNull = juliaRef == null;
                bool formalNull = formalRef == null;

                if (!juliaNull && formalNull)
                {
                    diffType = DiffType.NullInFormal;

                    // Try to resolve equivalent in Formal
                    var resolved = ResolveEquivalent(juliaRef, juliaTransforms, formalTransforms);
                    if (resolved != null)
                    {
                        canFix = true;
                        resolvedValue = resolved;
                        fixAction = () =>
                        {
                            formalProp.objectReferenceValue = resolved;
                            formalSO.ApplyModifiedProperties();
                            EditorUtility.SetDirty(formalProp.serializedObject.targetObject);
                        };
                        formalVal = $"null → can fix to: {resolved.name}";
                    }
                }
                else if (juliaNull && !formalNull)
                {
                    diffType = DiffType.NullInJulia;
                }
                else if (!juliaNull && !formalNull && juliaRef.name != formalRef.name)
                {
                    // Check if it's equivalent (mapped bone)
                    var mappedName = MapName(juliaRef.name);
                    if (mappedName != formalRef.name)
                    {
                        diffType = DiffType.DifferentValue;
                    }
                }
            }
            else if (juliaProp.propertyType == SerializedPropertyType.ArraySize ||
                     juliaProp.propertyPath.EndsWith(".Array.size"))
            {
                if (juliaProp.intValue != formalProp.intValue)
                {
                    diffType = DiffType.DifferentCount;
                    canFix = true;
                    fixAction = () =>
                    {
                        formalProp.intValue = juliaProp.intValue;
                        formalSO.ApplyModifiedProperties();
                        EditorUtility.SetDirty(formalProp.serializedObject.targetObject);
                    };
                }
            }
            else if (juliaVal != formalVal)
            {
                diffType = DiffType.DifferentValue;

                // Can fix primitive values
                if (juliaProp.propertyType != SerializedPropertyType.Generic &&
                    juliaProp.propertyType != SerializedPropertyType.ManagedReference)
                {
                    canFix = true;
                    fixAction = () =>
                    {
                        formalSO.CopyFromSerializedProperty(juliaProp);
                        formalSO.ApplyModifiedProperties();
                        EditorUtility.SetDirty(formalProp.serializedObject.targetObject);
                    };
                }
            }

            if (diffType != DiffType.Same || _showAll)
            {
                _diffs.Add(new DiffItem
                {
                    ComponentPath = compPath,
                    ComponentType = compType,
                    FieldPath = juliaProp.propertyPath,
                    JuliaValue = juliaVal,
                    FormalValue = formalVal,
                    Type = diffType,
                    CanAutoFix = canFix,
                    FixAction = fixAction,
                    FormalProperty = formalProp,
                    ResolvedValue = resolvedValue
                });
            }
        }

        private UnityEngine.Object ResolveEquivalent(UnityEngine.Object juliaRef,
            Dictionary<string, Transform> juliaTransforms, Dictionary<string, Transform> formalTransforms)
        {
            if (juliaRef == null)
                return null;

            Transform targetTransform = null;

            if (juliaRef is Transform t)
            {
                targetTransform = FindEquivalentTransform(t, formalTransforms);
                return targetTransform;
            }
            else if (juliaRef is GameObject go)
            {
                targetTransform = FindEquivalentTransform(go.transform, formalTransforms);
                return targetTransform?.gameObject;
            }
            else if (juliaRef is Component comp)
            {
                targetTransform = FindEquivalentTransform(comp.transform, formalTransforms);
                if (targetTransform != null)
                    return targetTransform.GetComponent(comp.GetType());
            }

            return null;
        }

        private Transform FindEquivalentTransform(Transform source, Dictionary<string, Transform> formalTransforms)
        {
            // Try exact name
            if (formalTransforms.TryGetValue(source.name, out var result))
                return result;

            // Try mapped name
            string mapped = MapName(source.name);
            if (formalTransforms.TryGetValue(mapped, out result))
                return result;

            // Try parent path matching
            string parentName = source.parent?.name ?? "";
            string mappedParent = MapName(parentName);
            string keyWithParent = $"{mappedParent}/{mapped}";
            if (formalTransforms.TryGetValue(keyWithParent, out result))
                return result;

            return null;
        }

        private Dictionary<string, Transform> BuildTransformLookup(Transform root)
        {
            var dict = new Dictionary<string, Transform>();
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                // By name
                if (!dict.ContainsKey(t.name))
                    dict[t.name] = t;

                // By mapped name
                string mapped = MapName(t.name);
                if (!dict.ContainsKey(mapped))
                    dict[mapped] = t;

                // By path
                string path = GetPath(t, root);
                if (!dict.ContainsKey(path))
                    dict[path] = t;
            }
            return dict;
        }

        private string GetComponentKey(Component c, Transform root)
        {
            // Use object name + type for better matching across different hierarchies
            string objName = MapName(c.gameObject.name);
            string parentName = c.transform.parent != null ? MapName(c.transform.parent.name) : "";
            return $"{parentName}/{objName}|{c.GetType().Name}";
        }

        private string GetPath(Transform t, Transform root)
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

        private string GetNormalizedPath(Transform t, Transform root)
        {
            var parts = new List<string>();
            var current = t;
            while (current != null && current != root)
            {
                parts.Insert(0, MapName(current.name));
                current = current.parent;
            }
            return string.Join("/", parts);
        }

        private string MapName(string name)
        {
            if (BoneMapping.TryGetValue(name, out var mapped))
                return mapped;

            // Handle Julia_Formal -> Julia root name
            if (name == "Julia_Formal")
                return "Julia";

            return name;
        }

        private string PropToString(SerializedProperty prop)
        {
            return prop.propertyType switch
            {
                SerializedPropertyType.Integer => prop.intValue.ToString(),
                SerializedPropertyType.Boolean => prop.boolValue.ToString(),
                SerializedPropertyType.Float => prop.floatValue.ToString("F3"),
                SerializedPropertyType.String => prop.stringValue,
                SerializedPropertyType.ObjectReference => prop.objectReferenceValue?.name ?? "null",
                SerializedPropertyType.Enum => prop.enumNames.Length > prop.enumValueIndex && prop.enumValueIndex >= 0
                    ? prop.enumNames[prop.enumValueIndex] : prop.intValue.ToString(),
                SerializedPropertyType.Vector2 => prop.vector2Value.ToString(),
                SerializedPropertyType.Vector3 => prop.vector3Value.ToString(),
                SerializedPropertyType.Vector4 => prop.vector4Value.ToString(),
                SerializedPropertyType.Quaternion => prop.quaternionValue.eulerAngles.ToString(),
                SerializedPropertyType.ArraySize => prop.intValue.ToString(),
                _ => $"({prop.propertyType})"
            };
        }

        private string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= max ? s : s.Substring(0, max) + "...";
        }

        private void FixAllProblems()
        {
            if (_diffs.Count == 0)
            {
                Analyze();
            }

            Undo.RegisterFullObjectHierarchyUndo(_juliaFormal, "Fix all Julia_Formal problems");

            int fixedCount = 0;
            foreach (var diff in _diffs)
            {
                if (diff.CanAutoFix && diff.FixAction != null)
                {
                    try
                    {
                        diff.FixAction();
                        fixedCount++;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Failed to fix {diff.ComponentType}.{diff.FieldPath}: {e.Message}");
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log($"Fixed {fixedCount} problems.");
            EditorUtility.DisplayDialog("Done",
                $"Fixed {fixedCount} problems.\n\nNow select Julia_Formal and use Overrides → Apply All to save to prefab.",
                "OK");

            Analyze(); // Re-analyze
        }

        private void ExportReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== DEEP CHARACTER COMPARISON REPORT ===\n");
            sb.AppendLine($"Julia: {_julia?.name}");
            sb.AppendLine($"Julia_Formal: {_juliaFormal?.name}");
            sb.AppendLine();

            var grouped = _diffs.Where(d => d.Type != DiffType.Same).GroupBy(d => d.Type);

            foreach (var group in grouped.OrderBy(g => (int)g.Key))
            {
                sb.AppendLine($"\n=== {group.Key} ({group.Count()}) ===\n");

                foreach (var diff in group)
                {
                    sb.AppendLine($"[{diff.ComponentType}] {diff.ComponentPath}");
                    sb.AppendLine($"  Field: {diff.FieldPath}");
                    sb.AppendLine($"  Julia: {diff.JuliaValue}");
                    sb.AppendLine($"  Formal: {diff.FormalValue}");
                    sb.AppendLine($"  Can Fix: {diff.CanAutoFix}");
                    sb.AppendLine();
                }
            }

            Debug.Log(sb.ToString());
        }
    }
}
