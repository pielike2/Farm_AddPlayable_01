using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;

namespace Editor
{
    /// <summary>
    /// Unified fixer for Julia_Formal - fixes all broken references in one go.
    /// Works on scene instances and applies to prefab.
    /// </summary>
    public class JuliaFormalUnifiedFixer : EditorWindow
    {
        private GameObject _julia;
        private GameObject _juliaFormal;
        private Vector2 _scrollPosition;
        private List<FixItem> _fixItems = new List<FixItem>();
        private bool _showDetails = true;

        // Bone name mapping from old skeleton (TS_Character_Rig) to new (Formal.fbx)
        private static readonly Dictionary<string, string> BoneMapping = new Dictionary<string, string>
        {
            // Core skeleton
            {"Root_M", "Root"},
            {"Hip_M", "Hips"},
            {"Spine1_M", "Abdomen"},
            {"Spine2_M", "Torso"},
            {"Chest_M", "Chest"},
            {"Neck_M", "Neck"},
            {"Head_M", "Head"},
            // Left arm
            {"Scapula_L", "Shoulder.L"},
            {"Shoulder_L", "UpperArm.L"},
            {"Elbow_L", "LowerArm.L"},
            {"Wrist_L", "Wrist.L"},
            // Right arm
            {"Scapula_R", "Shoulder.R"},
            {"Shoulder_R", "UpperArm.R"},
            {"Elbow_R", "LowerArm.R"},
            {"Wrist_R", "Wrist.R"},
            // Left leg
            {"Hip_L", "UpperLeg.L"},
            {"Knee_L", "LowerLeg.L"},
            {"Ankle_L", "Foot.L"},
            {"Toes_L", "Toes.L"},
            // Right leg
            {"Hip_R", "UpperLeg.R"},
            {"Knee_R", "LowerLeg.R"},
            {"Ankle_R", "Foot.R"},
            {"Toes_R", "Toes.R"},
            // Left hand fingers
            {"ThumbFinger1_L", "Thumb1.L"},
            {"ThumbFinger2_L", "Thumb2.L"},
            {"ThumbFinger3_L", "Thumb3.L"},
            {"IndexFinger1_L", "Index1.L"},
            {"IndexFinger2_L", "Index2.L"},
            {"IndexFinger3_L", "Index3.L"},
            {"MiddleFinger1_L", "Middle1.L"},
            {"MiddleFinger2_L", "Middle2.L"},
            {"MiddleFinger3_L", "Middle3.L"},
            {"RingFinger1_L", "Ring1.L"},
            {"RingFinger2_L", "Ring2.L"},
            {"RingFinger3_L", "Ring3.L"},
            {"PinkyFinger1_L", "Pinky1.L"},
            {"PinkyFinger2_L", "Pinky2.L"},
            {"PinkyFinger3_L", "Pinky3.L"},
            // Right hand fingers
            {"ThumbFinger1_R", "Thumb1.R"},
            {"ThumbFinger2_R", "Thumb2.R"},
            {"ThumbFinger3_R", "Thumb3.R"},
            {"IndexFinger1_R", "Index1.R"},
            {"IndexFinger2_R", "Index2.R"},
            {"IndexFinger3_R", "Index3.R"},
            {"MiddleFinger1_R", "Middle1.R"},
            {"MiddleFinger2_R", "Middle2.R"},
            {"MiddleFinger3_R", "Middle3.R"},
            {"RingFinger1_R", "Ring1.R"},
            {"RingFinger2_R", "Ring2.R"},
            {"RingFinger3_R", "Ring3.R"},
            {"PinkyFinger1_R", "Pinky1.R"},
            {"PinkyFinger2_R", "Pinky2.R"},
            {"PinkyFinger3_R", "Pinky3.R"},
            // Special mappings
            {"DeformationSystem", "CharacterArmature"},
        };

        private class FixItem
        {
            public string Category;
            public string ObjectPath;
            public string ComponentType;
            public string FieldName;
            public string OldValue;
            public string NewValue;
            public bool CanFix;
            public bool WasFixed;
            public System.Action FixAction;
        }

        [MenuItem("Tools/Julia Formal Unified Fixer")]
        public static void ShowWindow()
        {
            var window = GetWindow<JuliaFormalUnifiedFixer>("Unified Fixer");
            window.minSize = new Vector2(900, 700);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Julia → Julia_Formal Unified Fixer", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "This tool fixes ALL problems in Julia_Formal by comparing with Julia:\n\n" +
                "1. Cargo list null references\n" +
                "2. Transform references (bones, attach points)\n" +
                "3. Component references (sensors, event receivers)\n" +
                "4. Array/List element remapping\n\n" +
                "Works on scene instances. After fixing, apply overrides to prefab.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // Object fields
            EditorGUILayout.BeginHorizontal();
            _julia = EditorGUILayout.ObjectField("Julia (Source)", _julia, typeof(GameObject), true) as GameObject;
            if (GUILayout.Button("Find", GUILayout.Width(50)))
            {
                _julia = GameObject.Find("Julia");
                if (_julia == null) Debug.LogWarning("Julia not found on scene");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _juliaFormal = EditorGUILayout.ObjectField("Julia_Formal (Target)", _juliaFormal, typeof(GameObject), true) as GameObject;
            if (GUILayout.Button("Find", GUILayout.Width(50)))
            {
                _juliaFormal = GameObject.Find("Julia_Formal");
                if (_juliaFormal == null) Debug.LogWarning("Julia_Formal not found on scene");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Buttons
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("1. ANALYZE", GUILayout.Height(40)))
            {
                Analyze();
            }

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("2. FIX ALL", GUILayout.Height(40)))
            {
                FixAll();
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("3. Apply to Prefab", GUILayout.Height(40)))
            {
                ApplyToPrefab();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("COPY MISSING OBJECTS from Julia to Julia_Formal", GUILayout.Height(30)))
            {
                CopyMissingObjects();
            }

            if (GUILayout.Button("Copy Animator Controller from Julia to Julia_Formal", GUILayout.Height(25)))
            {
                CopyAnimatorController();
            }

            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("FIX CARGO TRANSFORMS (copy from Julia)", GUILayout.Height(30)))
            {
                FixCargoTransforms();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Debug: List Julia Objects"))
            {
                ListAllObjects(_julia, "Julia");
            }
            if (GUILayout.Button("Debug: List Formal Objects"))
            {
                ListAllObjects(_juliaFormal, "Julia_Formal");
            }
            if (GUILayout.Button("Check Critical Components"))
            {
                CheckCriticalComponents();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Statistics
            if (_fixItems.Count > 0)
            {
                var canFix = _fixItems.Count(f => f.CanFix);
                var wasFixed = _fixItems.Count(f => f.WasFixed);
                var categories = _fixItems.GroupBy(f => f.Category).Select(g => $"{g.Key}: {g.Count()}");

                EditorGUILayout.HelpBox(
                    $"Found {_fixItems.Count} issues:\n" +
                    $"  Can fix: {canFix}\n" +
                    $"  Already fixed: {wasFixed}\n\n" +
                    $"By category:\n  " + string.Join("\n  ", categories),
                    canFix > wasFixed ? MessageType.Warning : MessageType.Info);
            }

            _showDetails = EditorGUILayout.Toggle("Show Details", _showDetails);

            EditorGUILayout.Space(5);

            // Results scroll view
            if (_showDetails)
            {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

                string currentCategory = "";
                foreach (var item in _fixItems)
                {
                    if (item.Category != currentCategory)
                    {
                        currentCategory = item.Category;
                        EditorGUILayout.Space(5);
                        EditorGUILayout.LabelField($"=== {currentCategory} ===", EditorStyles.boldLabel);
                    }

                    Color bgColor = item.WasFixed ? new Color(0.7f, 1f, 0.7f) :
                                    item.CanFix ? new Color(1f, 1f, 0.7f) :
                                    new Color(1f, 0.8f, 0.8f);

                    GUI.backgroundColor = bgColor;
                    EditorGUILayout.BeginVertical("box");
                    GUI.backgroundColor = Color.white;

                    EditorGUILayout.LabelField($"{item.ObjectPath}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"[{item.ComponentType}] {item.FieldName}");
                    EditorGUILayout.LabelField($"  Old: {item.OldValue}");
                    EditorGUILayout.LabelField($"  New: {item.NewValue}");

                    if (item.CanFix && !item.WasFixed && item.FixAction != null)
                    {
                        if (GUILayout.Button("Fix This", GUILayout.Width(80)))
                        {
                            item.FixAction();
                            item.WasFixed = true;
                        }
                    }

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void Analyze()
        {
            _fixItems.Clear();

            if (_julia == null || _juliaFormal == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign both Julia and Julia_Formal!", "OK");
                return;
            }

            // Build transform dictionaries for both hierarchies
            var juliaTransforms = BuildTransformDictionary(_julia.transform);
            var formalTransforms = BuildTransformDictionary(_juliaFormal.transform);

            Debug.Log($"Julia transforms: {juliaTransforms.Count}, Formal transforms: {formalTransforms.Count}");

            // Check for missing objects in Julia_Formal
            foreach (var kvp in juliaTransforms)
            {
                string mappedName = MapName(kvp.Key);
                if (!formalTransforms.ContainsKey(kvp.Key) && !formalTransforms.ContainsKey(mappedName))
                {
                    // Check if it's an important object (has components)
                    var comps = kvp.Value.GetComponents<Component>().Where(c => c != null && !(c is Transform)).ToList();
                    if (comps.Count > 0)
                    {
                        _fixItems.Add(new FixItem
                        {
                            Category = "Missing Object",
                            ObjectPath = GetObjectPath(kvp.Value, _julia.transform),
                            ComponentType = string.Join(", ", comps.Select(c => c.GetType().Name)),
                            FieldName = kvp.Key,
                            OldValue = "(missing in Julia_Formal)",
                            NewValue = "(needs manual creation)",
                            CanFix = false
                        });
                    }
                }
            }

            // Get all MonoBehaviours
            var juliaComps = _julia.GetComponentsInChildren<MonoBehaviour>(true).Where(c => c != null).ToList();
            var formalComps = _juliaFormal.GetComponentsInChildren<MonoBehaviour>(true).Where(c => c != null).ToList();

            // Strategy 1: Match by type (for unique component types)
            var juliaByType = juliaComps.GroupBy(c => c.GetType()).ToDictionary(g => g.Key, g => g.ToList());
            var formalByType = formalComps.GroupBy(c => c.GetType()).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var kvp in juliaByType)
            {
                var type = kvp.Key;
                var juliaList = kvp.Value;

                if (!formalByType.TryGetValue(type, out var formalList))
                    continue;

                // If only one component of this type, match directly
                if (juliaList.Count == 1 && formalList.Count == 1)
                {
                    CompareAndCollectFixes(juliaList[0], formalList[0], juliaTransforms, formalTransforms);
                }
                else
                {
                    // Multiple components of same type - match by object name with mapping
                    foreach (var juliaComp in juliaList)
                    {
                        var juliaObjMapped = MapName(juliaComp.gameObject.name);

                        // Try to find matching formal component
                        var formalComp = formalList.FirstOrDefault(f =>
                            MapName(f.gameObject.name) == juliaObjMapped ||
                            f.gameObject.name == juliaComp.gameObject.name);

                        if (formalComp == null)
                        {
                            // Try by index if names don't match
                            int index = juliaList.IndexOf(juliaComp);
                            if (index < formalList.Count)
                                formalComp = formalList[index];
                        }

                        if (formalComp != null)
                        {
                            CompareAndCollectFixes(juliaComp, formalComp, juliaTransforms, formalTransforms);
                        }
                    }
                }
            }

            // Sort by category
            _fixItems = _fixItems.OrderBy(f => f.Category).ThenBy(f => f.ObjectPath).ToList();

            Debug.Log($"Analysis complete. Found {_fixItems.Count} issues, {_fixItems.Count(f => f.CanFix)} can be fixed.");
        }

        private string GetComponentKey(MonoBehaviour c)
        {
            var objName = MapName(c.gameObject.name);
            return $"{objName}|{c.GetType().Name}";
        }

        private void CompareAndCollectFixes(MonoBehaviour juliaComp, MonoBehaviour formalComp,
            Dictionary<string, Transform> juliaTransforms, Dictionary<string, Transform> formalTransforms)
        {
            var type = juliaComp.GetType();
            var path = GetObjectPath(formalComp.transform, _juliaFormal.transform);

            var so1 = new SerializedObject(juliaComp);
            var so2 = new SerializedObject(formalComp);

            // Track visited arrays to avoid duplicate processing
            var visitedArrays = new HashSet<string>();

            var prop = so1.GetIterator();
            while (prop.NextVisible(true))
            {
                if (prop.name == "m_Script") continue;
                if (prop.name == "m_ObjectHideFlags") continue;

                var prop2 = so2.FindProperty(prop.propertyPath);
                if (prop2 == null) continue;

                // Skip array elements - we handle arrays at the array level
                if (prop.propertyPath.Contains(".Array.data["))
                    continue;

                // Handle arrays (only process each array once)
                if (prop.isArray && prop.propertyType != SerializedPropertyType.String)
                {
                    if (visitedArrays.Contains(prop.propertyPath))
                        continue;
                    visitedArrays.Add(prop.propertyPath);

                    CompareArrays(prop, prop2, type.Name, path, juliaTransforms, formalTransforms, so2);
                    continue;
                }

                // Handle object references
                if (prop.propertyType == SerializedPropertyType.ObjectReference)
                {
                    CompareObjectReference(prop, prop2, type.Name, path, juliaTransforms, formalTransforms, so2);
                }
            }
        }

        private void CompareArrays(SerializedProperty juliaProp, SerializedProperty formalProp,
            string typeName, string path, Dictionary<string, Transform> juliaTransforms,
            Dictionary<string, Transform> formalTransforms, SerializedObject formalSO)
        {
            // Skip non-object arrays for now (just check size)
            bool isObjectArray = juliaProp.arraySize > 0 &&
                juliaProp.GetArrayElementAtIndex(0).propertyType == SerializedPropertyType.ObjectReference;

            if (!isObjectArray)
            {
                if (juliaProp.arraySize != formalProp.arraySize)
                {
                    _fixItems.Add(new FixItem
                    {
                        Category = "Array Size",
                        ObjectPath = path,
                        ComponentType = typeName,
                        FieldName = juliaProp.propertyPath + ".size",
                        OldValue = formalProp.arraySize.ToString(),
                        NewValue = juliaProp.arraySize.ToString(),
                        CanFix = false // Non-object arrays need manual fix
                    });
                }
                return;
            }

            // Count valid (non-null) elements in both arrays
            int juliaValidCount = 0;
            int formalValidCount = 0;
            int formalNullCount = 0;
            var juliaValidRefs = new List<Object>();
            var formalValidRefs = new List<Object>();

            for (int i = 0; i < juliaProp.arraySize; i++)
            {
                var elem = juliaProp.GetArrayElementAtIndex(i);
                if (elem.objectReferenceValue != null)
                {
                    juliaValidCount++;
                    juliaValidRefs.Add(elem.objectReferenceValue);
                }
            }

            for (int i = 0; i < formalProp.arraySize; i++)
            {
                var elem = formalProp.GetArrayElementAtIndex(i);
                if (elem.objectReferenceValue != null)
                {
                    formalValidCount++;
                    formalValidRefs.Add(elem.objectReferenceValue);
                }
                else
                {
                    formalNullCount++;
                }
            }

            // If arrays are different (size or content), we need to remap
            bool needsRemap = juliaProp.arraySize != formalProp.arraySize ||
                              formalNullCount > 0 ||
                              juliaValidCount != formalValidCount;

            if (needsRemap && juliaValidCount > 0)
            {
                // Collect all valid julia refs and their mappings
                var mappings = new List<(Object juliaRef, Object formalRef)>();
                bool allCanMap = true;
                var unmappable = new List<string>();

                for (int i = 0; i < juliaProp.arraySize; i++)
                {
                    var juliaElem = juliaProp.GetArrayElementAtIndex(i);
                    var juliaRef = juliaElem.objectReferenceValue;
                    if (juliaRef == null) continue;

                    var mapped = FindEquivalentObject(juliaRef, juliaTransforms, formalTransforms);
                    if (mapped == null)
                    {
                        allCanMap = false;
                        unmappable.Add(juliaRef.name);
                    }
                    mappings.Add((juliaRef, mapped));
                }

                string juliaNames = string.Join(", ", juliaValidRefs.Select(r => r.name));
                string formalNames = string.Join(", ", formalValidRefs.Select(r => r.name));

                string oldValue = $"[{formalValidCount}]: {formalNames}" + (formalNullCount > 0 ? $" + {formalNullCount} nulls" : "");
                string newValue = allCanMap
                    ? $"[{mappings.Count}]: {string.Join(", ", mappings.Select(m => m.formalRef?.name ?? "?"))}"
                    : $"Cannot map: {string.Join(", ", unmappable)}";

                _fixItems.Add(new FixItem
                {
                    Category = "Array Remap",
                    ObjectPath = path,
                    ComponentType = typeName,
                    FieldName = juliaProp.propertyPath,
                    OldValue = oldValue,
                    NewValue = newValue,
                    CanFix = allCanMap,
                    FixAction = allCanMap ? () => {
                        // Resize and remap
                        formalProp.arraySize = mappings.Count;
                        for (int i = 0; i < mappings.Count; i++)
                        {
                            var formalElem = formalProp.GetArrayElementAtIndex(i);
                            formalElem.objectReferenceValue = mappings[i].formalRef;
                        }
                        formalSO.ApplyModifiedProperties();
                    } : null
                });
            }
        }

        private void CompareObjectReference(SerializedProperty juliaProp, SerializedProperty formalProp,
            string typeName, string path, Dictionary<string, Transform> juliaTransforms,
            Dictionary<string, Transform> formalTransforms, SerializedObject formalSO)
        {
            var juliaRef = juliaProp.objectReferenceValue;
            var formalRef = formalProp.objectReferenceValue;

            bool juliaNull = juliaRef == null;
            bool formalNull = formalRef == null;

            // Julia has reference, Formal is null - needs fix
            if (!juliaNull && formalNull)
            {
                var mapped = FindEquivalentObject(juliaRef, juliaTransforms, formalTransforms);

                _fixItems.Add(new FixItem
                {
                    Category = "Null Reference",
                    ObjectPath = path,
                    ComponentType = typeName,
                    FieldName = juliaProp.propertyPath,
                    OldValue = "null",
                    NewValue = mapped != null ? GetObjectName(mapped) : $"(cannot find: {GetObjectName(juliaRef)})",
                    CanFix = mapped != null,
                    FixAction = mapped != null ? () => {
                        formalProp.objectReferenceValue = mapped;
                        formalSO.ApplyModifiedProperties();
                    } : null
                });
            }
            // Both have references but different (might need remapping)
            else if (!juliaNull && !formalNull)
            {
                string juliaName = GetObjectName(juliaRef);
                string formalName = GetObjectName(formalRef);
                string mappedJuliaName = MapName(juliaName);

                // If names don't match even after mapping, might be wrong reference
                if (mappedJuliaName != formalName && juliaName != formalName)
                {
                    // Check if we should remap this reference
                    var mapped = FindEquivalentObject(juliaRef, juliaTransforms, formalTransforms);
                    if (mapped != null && mapped != formalRef)
                    {
                        _fixItems.Add(new FixItem
                        {
                            Category = "Mismatched Reference",
                            ObjectPath = path,
                            ComponentType = typeName,
                            FieldName = juliaProp.propertyPath,
                            OldValue = formalName,
                            NewValue = GetObjectName(mapped),
                            CanFix = true,
                            FixAction = () => {
                                formalProp.objectReferenceValue = mapped;
                                formalSO.ApplyModifiedProperties();
                            }
                        });
                    }
                }
            }
        }

        private Object FindEquivalentObject(Object juliaRef, Dictionary<string, Transform> juliaTransforms,
            Dictionary<string, Transform> formalTransforms)
        {
            if (juliaRef == null) return null;

            string name = juliaRef.name;
            string mappedName = MapName(name);

            if (juliaRef is Transform juliaT)
            {
                // Try mapped name first
                if (formalTransforms.TryGetValue(mappedName, out var t)) return t;
                // Try original name
                if (formalTransforms.TryGetValue(name, out t)) return t;

                // Try finding by partial match (for tools/cargo objects)
                var partial = formalTransforms.Keys.FirstOrDefault(k => k.Contains(name) || name.Contains(k));
                if (partial != null && formalTransforms.TryGetValue(partial, out t)) return t;
            }
            else if (juliaRef is GameObject go)
            {
                if (formalTransforms.TryGetValue(mappedName, out var t)) return t.gameObject;
                if (formalTransforms.TryGetValue(name, out t)) return t.gameObject;

                // Try finding by partial match
                var partial = formalTransforms.Keys.FirstOrDefault(k => k.Contains(name) || name.Contains(k));
                if (partial != null && formalTransforms.TryGetValue(partial, out t)) return t.gameObject;
            }
            else if (juliaRef is Component comp)
            {
                string compObjName = MapName(comp.gameObject.name);

                // Try mapped name
                if (formalTransforms.TryGetValue(compObjName, out var t))
                {
                    var equiv = t.GetComponent(comp.GetType());
                    if (equiv != null) return equiv;
                }
                // Try original name
                if (formalTransforms.TryGetValue(comp.gameObject.name, out t))
                {
                    var equiv = t.GetComponent(comp.GetType());
                    if (equiv != null) return equiv;
                }

                // For components, also search all objects of matching type
                foreach (var kvp in formalTransforms)
                {
                    if (kvp.Key == compObjName || kvp.Key == comp.gameObject.name)
                    {
                        var equiv = kvp.Value.GetComponent(comp.GetType());
                        if (equiv != null) return equiv;
                    }
                }
            }

            return null;
        }

        private string MapName(string name)
        {
            if (BoneMapping.TryGetValue(name, out var mapped))
                return mapped;
            return name;
        }

        private Dictionary<string, Transform> BuildTransformDictionary(Transform root)
        {
            var dict = new Dictionary<string, Transform>();
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!dict.ContainsKey(t.name))
                    dict[t.name] = t;
            }
            return dict;
        }

        private string GetObjectPath(Transform t, Transform root)
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

        private string GetObjectName(Object obj)
        {
            if (obj == null) return "null";
            return obj.name;
        }

        private void FixAll()
        {
            if (_fixItems.Count == 0)
            {
                Analyze();
            }

            if (_juliaFormal != null)
            {
                Undo.RegisterFullObjectHierarchyUndo(_juliaFormal, "Fix Julia_Formal");
            }

            int fixedCount = 0;
            foreach (var item in _fixItems)
            {
                if (item.CanFix && !item.WasFixed && item.FixAction != null)
                {
                    try
                    {
                        item.FixAction();
                        item.WasFixed = true;
                        fixedCount++;
                        Debug.Log($"Fixed: [{item.Category}] {item.ComponentType}.{item.FieldName}: {item.OldValue} → {item.NewValue}");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Failed to fix {item.FieldName}: {e.Message}");
                    }
                }
            }

            if (_juliaFormal != null)
            {
                EditorUtility.SetDirty(_juliaFormal);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            Debug.Log($"Fixed {fixedCount} issues.");
            EditorUtility.DisplayDialog("Done", $"Fixed {fixedCount} issues.\n\nNow click 'Apply to Prefab' to save changes.", "OK");
        }

        private void ApplyToPrefab()
        {
            if (_juliaFormal == null)
            {
                EditorUtility.DisplayDialog("Error", "Julia_Formal not assigned!", "OK");
                return;
            }

            var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(_juliaFormal);
            if (string.IsNullOrEmpty(prefabPath))
            {
                EditorUtility.DisplayDialog("Error", "Julia_Formal is not a prefab instance!", "OK");
                return;
            }

            PrefabUtility.ApplyPrefabInstance(_juliaFormal, InteractionMode.UserAction);
            Debug.Log($"Applied overrides to {prefabPath}");
            EditorUtility.DisplayDialog("Success", $"Applied all overrides to:\n{prefabPath}", "OK");
        }

        private void CopyAnimatorController()
        {
            if (_julia == null || _juliaFormal == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign both Julia and Julia_Formal!", "OK");
                return;
            }

            var juliaAnimator = _julia.GetComponentInChildren<Animator>(true);
            var formalAnimator = _juliaFormal.GetComponentInChildren<Animator>(true);

            if (juliaAnimator == null || formalAnimator == null)
            {
                EditorUtility.DisplayDialog("Error", "Animator not found!", "OK");
                return;
            }

            Undo.RecordObject(formalAnimator, "Copy Animator Controller");

            formalAnimator.runtimeAnimatorController = juliaAnimator.runtimeAnimatorController;

            EditorUtility.SetDirty(formalAnimator);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log($"Copied Animator Controller: {juliaAnimator.runtimeAnimatorController?.name} -> Julia_Formal");
            EditorUtility.DisplayDialog("Done",
                $"Copied Animator Controller:\n{juliaAnimator.runtimeAnimatorController?.name}\n\n" +
                "Test in Play Mode to see if chopping works now.",
                "OK");
        }

        private void FixCargoTransforms()
        {
            if (_julia == null || _juliaFormal == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign both Julia and Julia_Formal!", "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(_juliaFormal, "Fix Cargo Transforms");

            // Include [Inventory] and all cargo-related objects
            var objectNames = new[] { "[Inventory]", "[Cargo]", "Cargo_Tomato", "Cargo_DollarPacks" };
            int fixed_count = 0;
            var sb = new StringBuilder();
            sb.AppendLine("=== CARGO TRANSFORM FIX REPORT ===\n");

            foreach (var objName in objectNames)
            {
                // Find in Julia
                Transform juliaObj = null;
                foreach (var t in _julia.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == objName)
                    {
                        juliaObj = t;
                        break;
                    }
                }

                if (juliaObj == null)
                {
                    sb.AppendLine($"{objName}: NOT FOUND in Julia");
                    continue;
                }

                // Find in Julia_Formal
                Transform formalObj = null;
                foreach (var t in _juliaFormal.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == objName)
                    {
                        formalObj = t;
                        break;
                    }
                }

                if (formalObj == null)
                {
                    sb.AppendLine($"{objName}: NOT FOUND in Julia_Formal");
                    continue;
                }

                // Log before values
                sb.AppendLine($"\n{objName}:");
                sb.AppendLine($"  Julia parent: {juliaObj.parent?.name}");
                sb.AppendLine($"  Formal parent: {formalObj.parent?.name}");
                sb.AppendLine($"  Julia localPos: {juliaObj.localPosition}");
                sb.AppendLine($"  Julia localRot: {juliaObj.localEulerAngles}");
                sb.AppendLine($"  Julia localScale: {juliaObj.localScale}");
                sb.AppendLine($"  Formal localPos (before): {formalObj.localPosition}");
                sb.AppendLine($"  Formal localRot (before): {formalObj.localEulerAngles}");
                sb.AppendLine($"  Formal localScale (before): {formalObj.localScale}");

                // Check parent chain for scale issues
                sb.AppendLine($"  Julia world scale: {juliaObj.lossyScale}");
                sb.AppendLine($"  Formal world scale (before): {formalObj.lossyScale}");

                // Copy local transform from Julia
                formalObj.localPosition = juliaObj.localPosition;
                formalObj.localRotation = juliaObj.localRotation;
                formalObj.localScale = juliaObj.localScale;

                sb.AppendLine($"  Formal localScale (after): {formalObj.localScale}");
                sb.AppendLine($"  Formal world scale (after): {formalObj.lossyScale}");

                EditorUtility.SetDirty(formalObj);
                fixed_count++;
            }

            // Also check if parent bones have scale issues
            sb.AppendLine("\n=== PARENT BONE SCALE CHECK ===");
            var boneNames = new[] { "Spine1_M", "Abdomen", "Spine2_M", "Torso", "Chest_M", "Chest" };
            foreach (var boneName in boneNames)
            {
                Transform juliabone = null;
                Transform formalBone = null;

                foreach (var t in _julia.GetComponentsInChildren<Transform>(true))
                    if (t.name == boneName) { juliabone = t; break; }
                foreach (var t in _juliaFormal.GetComponentsInChildren<Transform>(true))
                    if (t.name == boneName) { formalBone = t; break; }

                if (juliabone != null)
                    sb.AppendLine($"Julia {boneName}: localScale={juliabone.localScale}, worldScale={juliabone.lossyScale}");
                if (formalBone != null)
                    sb.AppendLine($"Formal {boneName}: localScale={formalBone.localScale}, worldScale={formalBone.lossyScale}");
            }

            // Find where the 100x scale comes from by checking parent chain of INVENTORY
            sb.AppendLine("\n=== INVENTORY PARENT CHAIN (Formal) ===");
            Transform inventoryFormal = null;
            foreach (var t in _juliaFormal.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "INVENTORY")
                {
                    inventoryFormal = t;
                    break;
                }
            }

            if (inventoryFormal != null)
            {
                var current = inventoryFormal;
                while (current != null)
                {
                    sb.AppendLine($"  {current.name}: localScale={current.localScale}, worldScale={current.lossyScale}");
                    current = current.parent;
                }

                // Calculate scale factor difference
                Transform inventoryJulia = null;
                foreach (var t in _julia.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "INVENTORY")
                    {
                        inventoryJulia = t;
                        break;
                    }
                }

                if (inventoryJulia != null)
                {
                    float scaleFactor = inventoryFormal.lossyScale.x / inventoryJulia.lossyScale.x;
                    sb.AppendLine($"\n=== SCALE FACTOR: {scaleFactor}x ===");

                    if (Mathf.Abs(scaleFactor - 1f) > 0.01f)
                    {
                        sb.AppendLine($"Compensating by setting INVENTORY localScale to {1f/scaleFactor}");

                        // Compensate the scale on INVENTORY
                        float compensation = 1f / scaleFactor;
                        inventoryFormal.localScale = new Vector3(compensation, compensation, compensation);
                        EditorUtility.SetDirty(inventoryFormal);

                        sb.AppendLine($"INVENTORY new localScale: {inventoryFormal.localScale}");
                        sb.AppendLine($"INVENTORY new worldScale: {inventoryFormal.lossyScale}");
                    }

                    // Also fix rotation to match Julia's world rotation
                    sb.AppendLine($"\n=== ROTATION FIX ===");
                    sb.AppendLine($"Julia INVENTORY worldRot: {inventoryJulia.rotation.eulerAngles}");
                    sb.AppendLine($"Formal INVENTORY worldRot (before): {inventoryFormal.rotation.eulerAngles}");
                    sb.AppendLine($"Julia INVENTORY localRot: {inventoryJulia.localEulerAngles}");
                    sb.AppendLine($"Formal INVENTORY localRot (before): {inventoryFormal.localEulerAngles}");

                    // Calculate the rotation difference and compensate
                    // We want Formal's world rotation to match Julia's world rotation
                    Quaternion juliaWorldRot = inventoryJulia.rotation;
                    Quaternion formalParentWorldRot = inventoryFormal.parent != null ? inventoryFormal.parent.rotation : Quaternion.identity;

                    // localRotation = inverse(parentWorldRot) * targetWorldRot
                    Quaternion newLocalRot = Quaternion.Inverse(formalParentWorldRot) * juliaWorldRot;
                    inventoryFormal.localRotation = newLocalRot;

                    sb.AppendLine($"Formal INVENTORY localRot (after): {inventoryFormal.localEulerAngles}");
                    sb.AppendLine($"Formal INVENTORY worldRot (after): {inventoryFormal.rotation.eulerAngles}");

                    EditorUtility.SetDirty(inventoryFormal);
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log(sb.ToString());

            EditorUtility.DisplayDialog("Done",
                $"Fixed {fixed_count} object transforms.\n\n" +
                "Check Console for detailed report.\n" +
                "Test in Play Mode to verify cargo appearance.",
                "OK");
        }

        private void CheckCriticalComponents()
        {
            if (_juliaFormal == null)
            {
                Debug.LogError("Julia_Formal not assigned!");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("=== CRITICAL COMPONENTS CHECK ===\n");

            // Check CharacterInventory (Cargo)
            sb.AppendLine("--- CharacterInventory (Cargo) ---");
            var inventory = _juliaFormal.GetComponent<MonoBehaviour>();
            foreach (var comp in _juliaFormal.GetComponents<MonoBehaviour>())
            {
                if (comp == null) continue;
                if (comp.GetType().Name != "CharacterInventory") continue;

                var so = new SerializedObject(comp);
                var cargosProp = so.FindProperty("_cargos");
                if (cargosProp != null && cargosProp.isArray)
                {
                    sb.AppendLine($"_cargos array size: {cargosProp.arraySize}");
                    for (int i = 0; i < cargosProp.arraySize; i++)
                    {
                        var elem = cargosProp.GetArrayElementAtIndex(i);
                        var val = elem.objectReferenceValue;
                        sb.AppendLine($"  [{i}]: {(val != null ? val.name : "NULL")}");
                    }
                }
            }

            // Check Cargo transforms and parent bones
            sb.AppendLine("\n--- Cargo Transforms ---");
            var cargoNames = new[] { "Cargo_Tomato", "Cargo_DollarPacks" };
            foreach (var cargoName in cargoNames)
            {
                var formalCargo = _juliaFormal.transform.Find($"[Cargo]/{cargoName}");
                if (formalCargo == null)
                {
                    // Try to find anywhere in hierarchy
                    foreach (var t in _juliaFormal.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name == cargoName)
                        {
                            formalCargo = t;
                            break;
                        }
                    }
                }

                if (formalCargo != null)
                {
                    sb.AppendLine($"\n{cargoName} (Julia_Formal):");
                    sb.AppendLine($"  Parent: {formalCargo.parent?.name ?? "NULL"}");
                    sb.AppendLine($"  LocalPos: {formalCargo.localPosition}");
                    sb.AppendLine($"  LocalRot: {formalCargo.localEulerAngles}");
                    sb.AppendLine($"  LocalScale: {formalCargo.localScale}");
                    sb.AppendLine($"  WorldPos: {formalCargo.position}");
                }
                else
                {
                    sb.AppendLine($"\n{cargoName}: NOT FOUND in Julia_Formal");
                }

                if (_julia != null)
                {
                    Transform juliaCargo = null;
                    foreach (var t in _julia.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name == cargoName)
                        {
                            juliaCargo = t;
                            break;
                        }
                    }

                    if (juliaCargo != null)
                    {
                        sb.AppendLine($"{cargoName} (Julia):");
                        sb.AppendLine($"  Parent: {juliaCargo.parent?.name ?? "NULL"}");
                        sb.AppendLine($"  LocalPos: {juliaCargo.localPosition}");
                        sb.AppendLine($"  LocalRot: {juliaCargo.localEulerAngles}");
                        sb.AppendLine($"  LocalScale: {juliaCargo.localScale}");
                        sb.AppendLine($"  WorldPos: {juliaCargo.position}");
                    }
                }
            }

            // Check CharacterMeleeAttackTool (Chopping)
            sb.AppendLine("\n--- CharacterMeleeAttackTool (Chopping) ---");
            var sickle = _juliaFormal.transform.Find("[Mechanics]/[Tools]/Tool_Sickle");
            if (sickle != null)
            {
                foreach (var comp in sickle.GetComponents<MonoBehaviour>())
                {
                    if (comp == null) continue;
                    if (comp.GetType().Name != "CharacterMeleeAttackTool") continue;

                    var so = new SerializedObject(comp);
                    CheckProperty(so, "_sensor", sb);
                    CheckProperty(so, "_animEventReceiver", sb);
                    CheckProperty(so, "_attackCenter", sb);
                    CheckProperty(so, "_attackFx", sb);
                    CheckProperty(so, "_characterRoot", sb);
                }
            }
            else
            {
                sb.AppendLine("Tool_Sickle NOT FOUND!");
            }

            // Check all CharacterToolBase components for _characterRoot
            sb.AppendLine("\n--- All CharacterToolBase _characterRoot ---");
            var toolsRoot = _juliaFormal.transform.Find("[Mechanics]/[Tools]");
            if (toolsRoot != null)
            {
                foreach (Transform toolTransform in toolsRoot)
                {
                    foreach (var comp in toolTransform.GetComponents<MonoBehaviour>())
                    {
                        if (comp == null) continue;
                        var so = new SerializedObject(comp);
                        var charRootProp = so.FindProperty("_characterRoot");
                        if (charRootProp != null)
                        {
                            var val = charRootProp.objectReferenceValue;
                            sb.AppendLine($"  {toolTransform.name}/{comp.GetType().Name}._characterRoot: {(val != null ? val.name : "NULL <<<")}");
                        }
                    }
                }
            }

            // Check CharacterDampingRig (Camera)
            sb.AppendLine("\n--- CharacterDampingRig (Camera damping) ---");
            foreach (var comp in _juliaFormal.GetComponents<MonoBehaviour>())
            {
                if (comp == null) continue;
                if (comp.GetType().Name != "CharacterDampingRig") continue;

                var so = new SerializedObject(comp);
                CheckProperty(so, "_bone", sb);
            }

            // Check LootCollector
            sb.AppendLine("\n--- LootCollector (Loot collection) ---");
            foreach (var comp in _juliaFormal.GetComponents<MonoBehaviour>())
            {
                if (comp == null) continue;
                if (comp.GetType().Name != "LootCollector") continue;

                var so = new SerializedObject(comp);
                CheckProperty(so, "_lootCollectPoint", sb);
                CheckProperty(so, "_cargo", sb);
                CheckProperty(so, "_cargos", sb);
                CheckProperty(so, "_defaultCollectAnimation", sb);
                CheckProperty(so, "_cargoItemAppearAnimation", sb);
            }

            // Check CharacterLootPickuper (Pickuper)
            sb.AppendLine("\n--- CharacterLootPickuper (Item pickup) ---");
            var pickuper = _juliaFormal.transform.Find("[Mechanics]/Pickuper");
            if (pickuper != null)
            {
                foreach (var comp in pickuper.GetComponents<MonoBehaviour>())
                {
                    if (comp == null) continue;
                    if (comp.GetType().Name != "CharacterLootPickuper") continue;

                    var so = new SerializedObject(comp);
                    CheckProperty(so, "_sensor", sb);
                    CheckProperty(so, "_inventory", sb);
                    CheckProperty(so, "_movementAnim", sb);
                }
            }

            // Compare Julia vs Julia_Formal for critical components
            if (_julia != null)
            {
                sb.AppendLine("\n--- COMPARISON with Julia ---");

                // CharacterDampingRig._bone
                ComparePropertyBetween("CharacterDampingRig", "_bone", sb);

                // LootCollector
                ComparePropertyBetween("LootCollector", "_lootCollectPoint", sb);
                ComparePropertyBetween("LootCollector", "_cargo", sb);
                ComparePropertyBetween("LootCollector", "_useMultipleCargos", sb);

                // CharacterLootPickuper
                ComparePropertyBetween("CharacterLootPickuper", "_sensor", sb);
                ComparePropertyBetween("CharacterLootPickuper", "_inventory", sb);

                // CharacterMeleeAttackTool
                ComparePropertyBetween("CharacterMeleeAttackTool", "_sensor", sb);
                ComparePropertyBetween("CharacterMeleeAttackTool", "_animEventReceiver", sb);
                ComparePropertyBetween("CharacterMeleeAttackTool", "_attackCenter", sb);

                // Animator
                sb.AppendLine("\n--- Animator ---");
                var juliaAnimator = _julia.GetComponentInChildren<Animator>(true);
                var formalAnimator = _juliaFormal.GetComponentInChildren<Animator>(true);
                if (juliaAnimator != null && formalAnimator != null)
                {
                    var juliaController = juliaAnimator.runtimeAnimatorController;
                    var formalController = formalAnimator.runtimeAnimatorController;
                    sb.AppendLine($"  Julia Animator Controller: {(juliaController != null ? juliaController.name : "NULL")}");
                    sb.AppendLine($"  Formal Animator Controller: {(formalController != null ? formalController.name : "NULL")}");

                    if (juliaController != null && formalController != null)
                    {
                        bool match = juliaController.name == formalController.name;
                        sb.AppendLine($"  Controllers match: {(match ? "YES" : "NO <<<")}");
                    }

                    // Check Avatar
                    sb.AppendLine($"  Julia Avatar: {(juliaAnimator.avatar != null ? juliaAnimator.avatar.name : "NULL")}");
                    sb.AppendLine($"  Formal Avatar: {(formalAnimator.avatar != null ? formalAnimator.avatar.name : "NULL")}");
                }

                // MecanimEventReceiver
                sb.AppendLine("\n--- MecanimEventReceiver ---");
                var juliaEventReceiver = _julia.GetComponentInChildren<MonoBehaviour>(true);
                var formalEventReceiver = _juliaFormal.GetComponentInChildren<MonoBehaviour>(true);

                foreach (var comp in _julia.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (comp != null && comp.GetType().Name == "MecanimEventReceiver")
                    {
                        sb.AppendLine($"  Julia: {comp.name} (enabled={comp.enabled})");
                        break;
                    }
                }
                foreach (var comp in _juliaFormal.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (comp != null && comp.GetType().Name == "MecanimEventReceiver")
                    {
                        sb.AppendLine($"  Formal: {comp.name} (enabled={comp.enabled})");
                        break;
                    }
                }
            }

            Debug.Log(sb.ToString());
        }

        private void CheckProperty(SerializedObject so, string propName, StringBuilder sb)
        {
            var prop = so.FindProperty(propName);
            if (prop == null)
            {
                sb.AppendLine($"  {propName}: (property not found)");
                return;
            }

            if (prop.propertyType == SerializedPropertyType.ObjectReference)
            {
                var val = prop.objectReferenceValue;
                string status = val != null ? val.name : "NULL <<<";
                sb.AppendLine($"  {propName}: {status}");
            }
            else if (prop.isArray)
            {
                sb.AppendLine($"  {propName}: array[{prop.arraySize}]");
            }
            else
            {
                sb.AppendLine($"  {propName}: (not object reference)");
            }
        }

        private void ComparePropertyBetween(string componentName, string propName, StringBuilder sb)
        {
            MonoBehaviour juliaComp = null;
            MonoBehaviour formalComp = null;

            foreach (var comp in _julia.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (comp != null && comp.GetType().Name == componentName)
                {
                    juliaComp = comp;
                    break;
                }
            }

            foreach (var comp in _juliaFormal.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (comp != null && comp.GetType().Name == componentName)
                {
                    formalComp = comp;
                    break;
                }
            }

            if (juliaComp == null || formalComp == null)
            {
                sb.AppendLine($"  {componentName}.{propName}: (component not found)");
                return;
            }

            var so1 = new SerializedObject(juliaComp);
            var so2 = new SerializedObject(formalComp);

            var prop1 = so1.FindProperty(propName);
            var prop2 = so2.FindProperty(propName);

            if (prop1 == null || prop2 == null)
            {
                sb.AppendLine($"  {componentName}.{propName}: (property not found)");
                return;
            }

            string val1 = GetPropertyValueString(prop1);
            string val2 = GetPropertyValueString(prop2);

            bool match = val1 == val2;
            string status = match ? "MATCH" : "DIFFERENT <<<";

            sb.AppendLine($"  {componentName}.{propName}: Julia='{val1}' vs Formal='{val2}' [{status}]");
        }

        private string GetPropertyValueString(SerializedProperty prop)
        {
            if (prop.propertyType == SerializedPropertyType.ObjectReference)
            {
                var val = prop.objectReferenceValue;
                return val != null ? val.name : "null";
            }
            else if (prop.isArray)
            {
                return $"array[{prop.arraySize}]";
            }
            else if (prop.propertyType == SerializedPropertyType.Boolean)
            {
                return prop.boolValue.ToString();
            }
            else if (prop.propertyType == SerializedPropertyType.Integer)
            {
                return prop.intValue.ToString();
            }
            else if (prop.propertyType == SerializedPropertyType.Float)
            {
                return prop.floatValue.ToString("F2");
            }
            else if (prop.propertyType == SerializedPropertyType.String)
            {
                return prop.stringValue;
            }
            else
            {
                return prop.propertyType.ToString();
            }
        }

        private void CopyMissingObjects()
        {
            if (_julia == null || _juliaFormal == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign both Julia and Julia_Formal!", "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(_juliaFormal, "Copy Missing Objects");

            var juliaTransforms = BuildTransformDictionary(_julia.transform);
            var formalTransforms = BuildTransformDictionary(_juliaFormal.transform);

            int copied = 0;
            var failedToCopy = new List<string>();

            // Objects to skip (unused mechanics)
            var skipObjects = new HashSet<string>
            {
                "VacuumCleaner", "FeedingTool", "CollectPoint", "VacuumCone",
                "FX_Flow", "Meshes", "cone_1", "cone_2", "Glow", "Circles"
            };

            // Find all objects in Julia that don't exist in Julia_Formal
            foreach (var kvp in juliaTransforms)
            {
                string objName = kvp.Key;
                Transform juliaObj = kvp.Value;

                // Skip slot_* objects - there are hundreds of them
                if (objName.StartsWith("slot_")) continue;

                // Skip bones (they have different names in new skeleton)
                if (BoneMapping.ContainsKey(objName)) continue;

                // Skip unused mechanics
                if (skipObjects.Contains(objName)) continue;

                // Skip if already exists in formal (by name or mapped name)
                string mappedName = MapName(objName);
                if (formalTransforms.ContainsKey(objName) || formalTransforms.ContainsKey(mappedName))
                    continue;

                // Check if it has any important components (not just Transform)
                var components = juliaObj.GetComponents<Component>()
                    .Where(c => c != null && !(c is Transform))
                    .ToList();

                // Also copy mesh objects (but not unused mechanics)
                bool isMesh = juliaObj.GetComponent<MeshFilter>() != null ||
                              juliaObj.GetComponent<MeshRenderer>() != null ||
                              juliaObj.GetComponent<SkinnedMeshRenderer>() != null;

                if (components.Count == 0 && !isMesh)
                    continue;

                // Find equivalent parent in Julia_Formal
                var juliaParent = juliaObj.parent;
                if (juliaParent == null) continue;

                string parentName = MapName(juliaParent.name);
                Transform formalParent = null;

                if (formalTransforms.TryGetValue(parentName, out formalParent) ||
                    formalTransforms.TryGetValue(juliaParent.name, out formalParent))
                {
                    // Check if we already copied this object's parent (it might have been copied as part of hierarchy)
                    if (formalParent.Find(objName) != null)
                        continue;

                    // Copy the object with its children
                    var copy = Object.Instantiate(juliaObj.gameObject, formalParent);
                    copy.name = juliaObj.name; // Remove (Clone) suffix

                    Debug.Log($"Copied '{objName}' to '{formalParent.name}' (components: {string.Join(", ", components.Select(c => c.GetType().Name))})");
                    copied++;

                    // Refresh formal transforms after each copy
                    formalTransforms = BuildTransformDictionary(_juliaFormal.transform);
                }
                else
                {
                    failedToCopy.Add($"{objName} (parent: {juliaParent.name})");
                }
            }

            // Remap all references in copied objects
            if (copied > 0)
            {
                var allJuliaTransforms = BuildTransformDictionary(_julia.transform);
                var allFormalTransforms = BuildTransformDictionary(_juliaFormal.transform);

                foreach (var comp in _juliaFormal.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (comp == null) continue;
                    RemapReferencesInComponent(comp, allJuliaTransforms, allFormalTransforms);
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            var msg = $"Copied {copied} objects.";
            if (failedToCopy.Count > 0)
            {
                msg += $"\n\nFailed to copy {failedToCopy.Count} objects (parent not found):\n" +
                       string.Join("\n", failedToCopy.Take(10));
            }

            EditorUtility.DisplayDialog("Done", msg + "\n\nRun ANALYZE again to check for remaining issues.", "OK");
        }

        private void RemapReferencesInComponent(MonoBehaviour comp, Dictionary<string, Transform> juliaTransforms,
            Dictionary<string, Transform> formalTransforms)
        {
            var so = new SerializedObject(comp);
            var prop = so.GetIterator();
            bool hasChanges = false;

            while (prop.NextVisible(true))
            {
                if (prop.propertyType == SerializedPropertyType.ObjectReference)
                {
                    var objRef = prop.objectReferenceValue;
                    if (objRef == null) continue;

                    // Check if this reference points to Julia hierarchy (not Julia_Formal)
                    bool isJuliaRef = false;
                    string refName = objRef.name;

                    if (objRef is Transform t && t.root.name == "Julia")
                        isJuliaRef = true;
                    else if (objRef is GameObject go && go.transform.root.name == "Julia")
                        isJuliaRef = true;
                    else if (objRef is Component c && c.transform.root.name == "Julia")
                        isJuliaRef = true;

                    if (isJuliaRef)
                    {
                        var mapped = FindEquivalentObject(objRef, juliaTransforms, formalTransforms);
                        if (mapped != null && mapped != objRef)
                        {
                            prop.objectReferenceValue = mapped;
                            hasChanges = true;
                            Debug.Log($"Remapped {comp.GetType().Name}.{prop.name}: {objRef.name} -> {mapped.name}");
                        }
                    }
                }
            }

            if (hasChanges)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(comp);
            }
        }

        private void RemapReferencesInHierarchy(Transform root, Dictionary<string, Transform> juliaTransforms,
            Dictionary<string, Transform> formalTransforms)
        {
            foreach (var comp in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (comp == null) continue;

                var so = new SerializedObject(comp);
                var prop = so.GetIterator();
                bool hasChanges = false;

                while (prop.NextVisible(true))
                {
                    if (prop.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        var objRef = prop.objectReferenceValue;
                        if (objRef == null) continue;

                        // Check if this reference points to Julia hierarchy
                        if (objRef is Transform t && juliaTransforms.ContainsValue(t))
                        {
                            var mapped = FindEquivalentObject(objRef, juliaTransforms, formalTransforms);
                            if (mapped != null && mapped != objRef)
                            {
                                prop.objectReferenceValue = mapped;
                                hasChanges = true;
                            }
                        }
                        else if (objRef is GameObject go && juliaTransforms.ContainsKey(go.name))
                        {
                            var mapped = FindEquivalentObject(objRef, juliaTransforms, formalTransforms);
                            if (mapped != null && mapped != objRef)
                            {
                                prop.objectReferenceValue = mapped;
                                hasChanges = true;
                            }
                        }
                        else if (objRef is Component c && juliaTransforms.ContainsKey(c.gameObject.name))
                        {
                            var mapped = FindEquivalentObject(objRef, juliaTransforms, formalTransforms);
                            if (mapped != null && mapped != objRef)
                            {
                                prop.objectReferenceValue = mapped;
                                hasChanges = true;
                            }
                        }
                    }
                }

                if (hasChanges)
                {
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(comp);
                }
            }
        }

        private void ListAllObjects(GameObject root, string label)
        {
            if (root == null)
            {
                Debug.LogWarning($"{label} is null");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"=== {label} Objects ===\n");

            var transforms = root.GetComponentsInChildren<Transform>(true);
            var grouped = transforms.GroupBy(t => t.parent?.name ?? "(root)").OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                sb.AppendLine($"Parent: {group.Key}");
                foreach (var t in group.OrderBy(x => x.name))
                {
                    var compTypes = t.GetComponents<Component>()
                        .Where(c => c != null && !(c is Transform))
                        .Select(c => c.GetType().Name);
                    var comps = compTypes.Any() ? $" [{string.Join(", ", compTypes)}]" : "";
                    sb.AppendLine($"  - {t.name}{comps}");
                }
            }

            Debug.Log(sb.ToString());
        }
    }
}
