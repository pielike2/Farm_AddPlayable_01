using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;

namespace Editor
{
    /// <summary>
    /// Compares Julia and Julia_Formal instances on scene to find broken references.
    /// </summary>
    public class SceneCharacterComparer : EditorWindow
    {
        private GameObject _julia;
        private GameObject _juliaFormal;
        private Vector2 _scrollPosition;
        private List<CompareResult> _results = new List<CompareResult>();
        private bool _showOnlyProblems = true;
        private bool _autoFix = false;

        private class CompareResult
        {
            public string Path;
            public string ComponentType;
            public string FieldName;
            public string JuliaValue;
            public string FormalValue;
            public bool IsProblem;
            public bool CanFix;
            public Component FormalComponent;
            public FieldInfo Field;
            public object FixValue;
        }

        [MenuItem("Tools/Compare Scene Characters")]
        public static void ShowWindow()
        {
            var window = GetWindow<SceneCharacterComparer>("Scene Comparer");
            window.minSize = new Vector2(800, 600);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Compare Julia vs Julia_Formal on Scene", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "1. Add Julia and Julia_Formal prefabs to the scene\n" +
                "2. Drag them to the fields below\n" +
                "3. Click 'Compare' to find differences\n" +
                "4. Click 'Auto-Fix' to copy values from Julia to Julia_Formal",
                MessageType.Info);

            EditorGUILayout.Space(10);

            _julia = EditorGUILayout.ObjectField("Julia (Working)", _julia, typeof(GameObject), true) as GameObject;
            _juliaFormal = EditorGUILayout.ObjectField("Julia_Formal (Broken)", _juliaFormal, typeof(GameObject), true) as GameObject;

            EditorGUILayout.Space(10);

            _showOnlyProblems = EditorGUILayout.Toggle("Show Only Problems", _showOnlyProblems);

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Compare", GUILayout.Height(35)))
            {
                Compare();
            }

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("AUTO-FIX ALL", GUILayout.Height(35)))
            {
                _autoFix = true;
                Compare();
                _autoFix = false;
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Export Full Report to Console"))
            {
                ExportReport();
            }

            EditorGUILayout.Space(10);

            // Statistics
            if (_results.Count > 0)
            {
                var problems = _results.Count(r => r.IsProblem);
                var fixable = _results.Count(r => r.CanFix);
                EditorGUILayout.HelpBox(
                    $"Found {_results.Count} differences\n" +
                    $"Problems (null in Formal): {problems}\n" +
                    $"Can auto-fix: {fixable}",
                    problems > 0 ? MessageType.Warning : MessageType.Info);
            }

            EditorGUILayout.Space(5);

            // Results
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            string currentPath = "";
            foreach (var result in _results)
            {
                if (_showOnlyProblems && !result.IsProblem)
                    continue;

                if (result.Path != currentPath)
                {
                    currentPath = result.Path;
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField(currentPath, EditorStyles.boldLabel);
                }

                Color bgColor = result.IsProblem ? new Color(1f, 0.7f, 0.7f) : new Color(0.9f, 0.9f, 0.9f);
                if (result.CanFix) bgColor = new Color(1f, 1f, 0.7f);

                GUI.backgroundColor = bgColor;
                EditorGUILayout.BeginVertical("box");
                GUI.backgroundColor = Color.white;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"[{result.ComponentType}]", EditorStyles.miniLabel, GUILayout.Width(180));
                EditorGUILayout.LabelField(result.FieldName, GUILayout.Width(150));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Julia: {result.JuliaValue}", GUILayout.Width(350));
                EditorGUILayout.LabelField($"Formal: {result.FormalValue}");
                EditorGUILayout.EndHorizontal();

                if (result.CanFix && result.FormalComponent != null)
                {
                    if (GUILayout.Button("Fix This", GUILayout.Width(80)))
                    {
                        FixSingle(result);
                    }
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }

        private void Compare()
        {
            _results.Clear();

            if (_julia == null || _juliaFormal == null)
            {
                Debug.LogError("Please assign both Julia and Julia_Formal!");
                return;
            }

            // Build transform dictionaries
            var juliaTransforms = new Dictionary<string, Transform>();
            var formalTransforms = new Dictionary<string, Transform>();

            foreach (var t in _julia.GetComponentsInChildren<Transform>(true))
                if (!juliaTransforms.ContainsKey(t.name))
                    juliaTransforms[t.name] = t;

            foreach (var t in _juliaFormal.GetComponentsInChildren<Transform>(true))
                if (!formalTransforms.ContainsKey(t.name))
                    formalTransforms[t.name] = t;

            // Get all components by type
            var juliaComps = _julia.GetComponentsInChildren<MonoBehaviour>(true)
                .Where(c => c != null)
                .GroupBy(c => c.GetType())
                .ToDictionary(g => g.Key, g => g.ToList());

            var formalComps = _juliaFormal.GetComponentsInChildren<MonoBehaviour>(true)
                .Where(c => c != null)
                .GroupBy(c => c.GetType())
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var kvp in juliaComps)
            {
                var type = kvp.Key;
                var juliaList = kvp.Value;

                if (!formalComps.TryGetValue(type, out var formalList))
                    continue;

                for (int i = 0; i < juliaList.Count && i < formalList.Count; i++)
                {
                    CompareComponents(juliaList[i], formalList[i], juliaTransforms, formalTransforms);
                }
            }

            // Sort - problems first
            _results = _results.OrderByDescending(r => r.IsProblem).ThenBy(r => r.Path).ToList();

            Debug.Log($"Comparison complete. Found {_results.Count} differences, {_results.Count(r => r.IsProblem)} problems.");
        }

        private void CompareComponents(MonoBehaviour juliaComp, MonoBehaviour formalComp,
            Dictionary<string, Transform> juliaTransforms, Dictionary<string, Transform> formalTransforms)
        {
            var type = juliaComp.GetType();
            var path = GetPath(formalComp.transform);

            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                if (field.IsPrivate && field.GetCustomAttribute<SerializeField>() == null)
                    continue;

                // Skip certain types
                if (typeof(System.Delegate).IsAssignableFrom(field.FieldType))
                    continue;

                try
                {
                    var juliaVal = field.GetValue(juliaComp);
                    var formalVal = field.GetValue(formalComp);

                    string juliaStr = ValueToString(juliaVal);
                    string formalStr = ValueToString(formalVal);

                    bool juliaNull = IsNullOrMissing(juliaVal);
                    bool formalNull = IsNullOrMissing(formalVal);

                    // Problem: Julia has value, Formal doesn't
                    bool isProblem = !juliaNull && formalNull;

                    // Can we fix it?
                    bool canFix = false;
                    object fixValue = null;

                    if (isProblem && juliaVal is Transform juliaTransform)
                    {
                        // Try to find equivalent in Formal
                        if (formalTransforms.TryGetValue(juliaTransform.name, out var formalTransform))
                        {
                            canFix = true;
                            fixValue = formalTransform;
                        }
                    }
                    else if (isProblem && juliaVal is GameObject juliaGo)
                    {
                        if (formalTransforms.TryGetValue(juliaGo.name, out var formalTransform))
                        {
                            canFix = true;
                            fixValue = formalTransform.gameObject;
                        }
                    }
                    else if (isProblem && juliaVal is Component juliaComponent)
                    {
                        // Try to find same component on equivalent object
                        if (formalTransforms.TryGetValue(juliaComponent.gameObject.name, out var formalTransform))
                        {
                            var formalEquiv = formalTransform.GetComponent(juliaComponent.GetType());
                            if (formalEquiv != null)
                            {
                                canFix = true;
                                fixValue = formalEquiv;
                            }
                        }
                    }

                    // Also check for list fields with null elements
                    if (juliaVal is System.Collections.IList juliaList && formalVal is System.Collections.IList formalList)
                    {
                        for (int i = 0; i < juliaList.Count && i < formalList.Count; i++)
                        {
                            var jItem = juliaList[i];
                            var fItem = formalList[i];

                            bool jNull = IsNullOrMissing(jItem);
                            bool fNull = IsNullOrMissing(fItem);

                            if (!jNull && fNull)
                            {
                                _results.Add(new CompareResult
                                {
                                    Path = path,
                                    ComponentType = type.Name,
                                    FieldName = $"{field.Name}[{i}]",
                                    JuliaValue = ValueToString(jItem),
                                    FormalValue = "NULL",
                                    IsProblem = true,
                                    CanFix = false,
                                    FormalComponent = formalComp,
                                    Field = field
                                });
                            }
                        }
                        continue; // Skip adding the whole list
                    }

                    if (isProblem || (!_showOnlyProblems && juliaStr != formalStr))
                    {
                        var result = new CompareResult
                        {
                            Path = path,
                            ComponentType = type.Name,
                            FieldName = field.Name,
                            JuliaValue = juliaStr,
                            FormalValue = formalStr,
                            IsProblem = isProblem,
                            CanFix = canFix,
                            FormalComponent = formalComp,
                            Field = field,
                            FixValue = fixValue
                        };

                        _results.Add(result);

                        if (_autoFix && canFix)
                        {
                            FixSingle(result);
                        }
                    }
                }
                catch { }
            }
        }

        private void FixSingle(CompareResult result)
        {
            if (result.FormalComponent == null || result.Field == null || result.FixValue == null)
                return;

            result.Field.SetValue(result.FormalComponent, result.FixValue);
            EditorUtility.SetDirty(result.FormalComponent);
            Debug.Log($"Fixed {result.ComponentType}.{result.FieldName} = {result.FixValue}");
            result.FormalValue = ValueToString(result.FixValue);
            result.IsProblem = false;
            result.CanFix = false;
        }

        private string GetPath(Transform t)
        {
            var parts = new List<string>();
            while (t != null)
            {
                parts.Insert(0, t.name);
                t = t.parent;
            }
            return string.Join("/", parts);
        }

        private bool IsNullOrMissing(object val)
        {
            if (val == null) return true;
            if (val is UnityEngine.Object obj && obj == null) return true;
            return false;
        }

        private string ValueToString(object val)
        {
            if (val == null) return "null";
            if (val is UnityEngine.Object obj)
            {
                if (obj == null) return "null (missing)";
                return obj.name;
            }
            if (val is System.Collections.IList list)
            {
                var nonNull = 0;
                var nullCount = 0;
                foreach (var item in list)
                {
                    if (IsNullOrMissing(item)) nullCount++;
                    else nonNull++;
                }
                return $"[{nonNull} items, {nullCount} null]";
            }
            return val.ToString();
        }

        private void ExportReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== JULIA vs JULIA_FORMAL SCENE COMPARISON ===\n");

            var problems = _results.Where(r => r.IsProblem).ToList();
            sb.AppendLine($"PROBLEMS ({problems.Count}):\n");

            foreach (var r in problems)
            {
                sb.AppendLine($"Path: {r.Path}");
                sb.AppendLine($"  Component: {r.ComponentType}");
                sb.AppendLine($"  Field: {r.FieldName}");
                sb.AppendLine($"  Julia: {r.JuliaValue}");
                sb.AppendLine($"  Formal: {r.FormalValue}");
                sb.AppendLine($"  Can Fix: {r.CanFix}");
                sb.AppendLine();
            }

            Debug.Log(sb.ToString());
        }
    }
}
