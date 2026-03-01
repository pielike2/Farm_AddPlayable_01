using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Editor
{
    /// <summary>
    /// Compares two prefabs component by component and shows missing/different fields.
    /// Use to find what's broken after model replacement.
    /// </summary>
    public class PrefabComponentComparer : EditorWindow
    {
        private const string JULIA_PATH = "Assets/Prefabs/Characters/Julia.prefab";
        private const string JULIA_FORMAL_PATH = "Assets/Prefabs/Characters/Julia_Formal.prefab";

        private Vector2 scrollPosition;
        private List<ComparisonResult> results = new List<ComparisonResult>();
        private bool showOnlyProblems = true;
        private bool showNullFields = true;
        private bool showMissingComponents = true;
        private bool showDifferentValues = false;

        private class ComparisonResult
        {
            public string objectPath;
            public string componentType;
            public string fieldName;
            public string juliaValue;
            public string formalValue;
            public ProblemType problemType;
            public Component formalComponent;
        }

        private enum ProblemType
        {
            OK,
            NullInFormal,
            MissingComponent,
            MissingObject,
            DifferentValue
        }

        [MenuItem("Tools/Compare Julia Prefabs")]
        public static void ShowWindow()
        {
            var window = GetWindow<PrefabComponentComparer>("Prefab Comparer");
            window.minSize = new Vector2(800, 600);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Julia vs Julia_Formal Component Comparison", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            showOnlyProblems = EditorGUILayout.Toggle("Show Only Problems", showOnlyProblems);
            showNullFields = EditorGUILayout.Toggle("Show Null Fields", showNullFields);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            showMissingComponents = EditorGUILayout.Toggle("Show Missing Components", showMissingComponents);
            showDifferentValues = EditorGUILayout.Toggle("Show Different Values", showDifferentValues);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Compare Prefabs", GUILayout.Height(30)))
            {
                ComparePrefabs();
            }

            if (GUILayout.Button("Export to Console (Full Report)", GUILayout.Height(25)))
            {
                ExportToConsole();
            }

            EditorGUILayout.Space(10);

            // Statistics
            if (results.Count > 0)
            {
                var nullCount = results.Count(r => r.problemType == ProblemType.NullInFormal);
                var missingCompCount = results.Count(r => r.problemType == ProblemType.MissingComponent);
                var missingObjCount = results.Count(r => r.problemType == ProblemType.MissingObject);
                var diffCount = results.Count(r => r.problemType == ProblemType.DifferentValue);

                EditorGUILayout.HelpBox(
                    $"Found:\n" +
                    $"  - Null fields in Formal: {nullCount}\n" +
                    $"  - Missing components: {missingCompCount}\n" +
                    $"  - Missing objects: {missingObjCount}\n" +
                    $"  - Different values: {diffCount}",
                    nullCount > 0 || missingCompCount > 0 ? MessageType.Warning : MessageType.Info);
            }

            EditorGUILayout.Space(5);

            // Results list
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            var filteredResults = results.Where(r =>
            {
                if (showOnlyProblems && r.problemType == ProblemType.OK) return false;
                if (!showNullFields && r.problemType == ProblemType.NullInFormal) return false;
                if (!showMissingComponents && (r.problemType == ProblemType.MissingComponent || r.problemType == ProblemType.MissingObject)) return false;
                if (!showDifferentValues && r.problemType == ProblemType.DifferentValue) return false;
                return true;
            }).ToList();

            string currentPath = "";
            foreach (var result in filteredResults)
            {
                if (result.objectPath != currentPath)
                {
                    currentPath = result.objectPath;
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField(currentPath, EditorStyles.boldLabel);
                }

                Color bgColor = result.problemType switch
                {
                    ProblemType.NullInFormal => new Color(1f, 0.8f, 0.8f),
                    ProblemType.MissingComponent => new Color(1f, 0.6f, 0.6f),
                    ProblemType.MissingObject => new Color(1f, 0.5f, 0.5f),
                    ProblemType.DifferentValue => new Color(1f, 1f, 0.8f),
                    _ => Color.white
                };

                GUI.backgroundColor = bgColor;
                EditorGUILayout.BeginVertical("box");
                GUI.backgroundColor = Color.white;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"[{result.componentType}]", GUILayout.Width(200));
                EditorGUILayout.LabelField(result.fieldName, GUILayout.Width(200));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Julia: {result.juliaValue}", GUILayout.Width(400));
                EditorGUILayout.LabelField($"Formal: {result.formalValue}");
                EditorGUILayout.EndHorizontal();

                // Add button to select the component in Formal prefab
                if (result.formalComponent != null && result.problemType == ProblemType.NullInFormal)
                {
                    if (GUILayout.Button("Select in Formal", GUILayout.Width(120)))
                    {
                        var formalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(JULIA_FORMAL_PATH);
                        Selection.activeObject = formalPrefab;
                        EditorGUIUtility.PingObject(result.formalComponent);
                    }
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }

        private void ComparePrefabs()
        {
            results.Clear();

            var juliaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(JULIA_PATH);
            var formalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(JULIA_FORMAL_PATH);

            if (juliaPrefab == null)
            {
                Debug.LogError($"Julia prefab not found at {JULIA_PATH}");
                return;
            }

            if (formalPrefab == null)
            {
                Debug.LogError($"Julia_Formal prefab not found at {JULIA_FORMAL_PATH}");
                return;
            }

            // Build path dictionary for Julia
            var juliaObjects = new Dictionary<string, GameObject>();
            BuildObjectDictionary(juliaPrefab.transform, "", juliaObjects);

            // Build path dictionary for Formal
            var formalObjects = new Dictionary<string, GameObject>();
            BuildObjectDictionary(formalPrefab.transform, "", formalObjects);

            Debug.Log($"Julia objects: {juliaObjects.Count}, Formal objects: {formalObjects.Count}");

            // Compare each object in Julia
            foreach (var kvp in juliaObjects)
            {
                string path = kvp.Key;
                GameObject juliaObj = kvp.Value;

                if (!formalObjects.TryGetValue(path, out GameObject formalObj))
                {
                    // Check if it's a skeleton bone (skip those)
                    if (IsSkeletonBone(juliaObj.name))
                        continue;

                    results.Add(new ComparisonResult
                    {
                        objectPath = path,
                        componentType = "GameObject",
                        fieldName = "(entire object)",
                        juliaValue = "EXISTS",
                        formalValue = "MISSING",
                        problemType = ProblemType.MissingObject
                    });
                    continue;
                }

                // Compare components
                CompareComponents(path, juliaObj, formalObj);
            }

            // Sort results by problem type (most severe first)
            results = results.OrderBy(r => r.problemType).ThenBy(r => r.objectPath).ToList();

            Debug.Log($"Comparison complete. Found {results.Count} results.");
        }

        private void BuildObjectDictionary(Transform transform, string parentPath, Dictionary<string, GameObject> dict)
        {
            string path = string.IsNullOrEmpty(parentPath) ? transform.name : $"{parentPath}/{transform.name}";
            dict[path] = transform.gameObject;

            foreach (Transform child in transform)
            {
                BuildObjectDictionary(child, path, dict);
            }
        }

        private bool IsSkeletonBone(string name)
        {
            // Julia skeleton bones
            if (name.EndsWith("_M") || name.EndsWith("_L") || name.EndsWith("_R"))
                return true;
            if (name.StartsWith("Root") || name.StartsWith("Hip") || name.StartsWith("Spine") ||
                name.StartsWith("Chest") || name.StartsWith("Neck") || name.StartsWith("Head") ||
                name.StartsWith("Shoulder") || name.StartsWith("Elbow") || name.StartsWith("Wrist") ||
                name.StartsWith("Knee") || name.StartsWith("Ankle") || name.StartsWith("Thumb") ||
                name.StartsWith("Index") || name.StartsWith("Middle") || name.StartsWith("Ring") ||
                name.StartsWith("Pinky") || name.StartsWith("Scapula") || name.StartsWith("Finger"))
                return true;

            // Formal skeleton bones
            if (name.EndsWith(".L") || name.EndsWith(".R"))
                return true;
            if (name == "Hips" || name == "Abdomen" || name == "Torso" || name == "CharacterArmature")
                return true;

            return false;
        }

        private void CompareComponents(string path, GameObject juliaObj, GameObject formalObj)
        {
            var juliaComponents = juliaObj.GetComponents<Component>().Where(c => c != null).ToList();
            var formalComponents = formalObj.GetComponents<Component>().Where(c => c != null).ToList();

            foreach (var juliaComp in juliaComponents)
            {
                var compType = juliaComp.GetType();

                // Skip Transform
                if (compType == typeof(Transform))
                    continue;

                // Find matching component in Formal
                var formalComp = formalComponents.FirstOrDefault(c => c.GetType() == compType);

                if (formalComp == null)
                {
                    results.Add(new ComparisonResult
                    {
                        objectPath = path,
                        componentType = compType.Name,
                        fieldName = "(entire component)",
                        juliaValue = "EXISTS",
                        formalValue = "MISSING",
                        problemType = ProblemType.MissingComponent
                    });
                    continue;
                }

                // Compare fields
                CompareComponentFields(path, compType.Name, juliaComp, formalComp);
            }
        }

        private void CompareComponentFields(string path, string compTypeName, Component juliaComp, Component formalComp)
        {
            var type = juliaComp.GetType();
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                // Skip non-serialized fields
                if (field.IsPrivate && field.GetCustomAttribute<SerializeField>() == null)
                    continue;

                // Skip certain field types
                if (field.FieldType.IsSubclassOf(typeof(Delegate)))
                    continue;

                try
                {
                    var juliaValue = field.GetValue(juliaComp);
                    var formalValue = field.GetValue(formalComp);

                    string juliaStr = ValueToString(juliaValue);
                    string formalStr = ValueToString(formalValue);

                    // Check for null references that should exist
                    bool juliaIsNull = IsNullOrEmpty(juliaValue);
                    bool formalIsNull = IsNullOrEmpty(formalValue);

                    if (!juliaIsNull && formalIsNull)
                    {
                        // Julia has value, Formal is null - this is a problem
                        results.Add(new ComparisonResult
                        {
                            objectPath = path,
                            componentType = compTypeName,
                            fieldName = field.Name,
                            juliaValue = juliaStr,
                            formalValue = "NULL/EMPTY",
                            problemType = ProblemType.NullInFormal,
                            formalComponent = formalComp
                        });
                    }
                    else if (!juliaIsNull && !formalIsNull && juliaStr != formalStr)
                    {
                        // Both have values but different
                        results.Add(new ComparisonResult
                        {
                            objectPath = path,
                            componentType = compTypeName,
                            fieldName = field.Name,
                            juliaValue = juliaStr,
                            formalValue = formalStr,
                            problemType = ProblemType.DifferentValue,
                            formalComponent = formalComp
                        });
                    }
                }
                catch
                {
                    // Skip fields that can't be compared
                }
            }
        }

        private bool IsNullOrEmpty(object value)
        {
            if (value == null)
                return true;

            if (value is UnityEngine.Object unityObj && unityObj == null)
                return true;

            if (value is string str && string.IsNullOrEmpty(str))
                return true;

            if (value is System.Collections.IList list && list.Count == 0)
                return true;

            if (value is System.Collections.IList listWithNulls)
            {
                bool allNull = true;
                foreach (var item in listWithNulls)
                {
                    if (item != null && !(item is UnityEngine.Object uo && uo == null))
                    {
                        allNull = false;
                        break;
                    }
                }
                if (allNull && listWithNulls.Count > 0)
                    return true;
            }

            return false;
        }

        private string ValueToString(object value)
        {
            if (value == null)
                return "null";

            if (value is UnityEngine.Object unityObj)
            {
                if (unityObj == null)
                    return "null (destroyed)";
                return unityObj.name;
            }

            if (value is System.Collections.IList list)
            {
                if (list.Count == 0)
                    return "[]";

                var items = new List<string>();
                int count = Math.Min(list.Count, 5);
                for (int i = 0; i < count; i++)
                {
                    items.Add(ValueToString(list[i]));
                }
                if (list.Count > 5)
                    items.Add($"... +{list.Count - 5} more");
                return $"[{string.Join(", ", items)}]";
            }

            return value.ToString();
        }

        private void ExportToConsole()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== JULIA vs JULIA_FORMAL COMPARISON REPORT ===\n");

            var grouped = results
                .Where(r => r.problemType != ProblemType.OK)
                .GroupBy(r => r.problemType);

            foreach (var group in grouped)
            {
                sb.AppendLine($"\n=== {group.Key} ({group.Count()}) ===\n");

                foreach (var result in group)
                {
                    sb.AppendLine($"Path: {result.objectPath}");
                    sb.AppendLine($"  Component: {result.componentType}");
                    sb.AppendLine($"  Field: {result.fieldName}");
                    sb.AppendLine($"  Julia: {result.juliaValue}");
                    sb.AppendLine($"  Formal: {result.formalValue}");
                    sb.AppendLine();
                }
            }

            Debug.Log(sb.ToString());
        }
    }
}
