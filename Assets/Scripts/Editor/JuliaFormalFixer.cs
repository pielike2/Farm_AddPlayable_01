using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Editor
{
    /// <summary>
    /// Automatically fixes broken Transform references in Julia_Formal.prefab
    /// by comparing with Julia.prefab and remapping bone references.
    /// </summary>
    public class JuliaFormalFixer : EditorWindow
    {
        private const string JULIA_PATH = "Assets/Prefabs/Characters/Julia.prefab";
        private const string JULIA_FORMAL_PATH = "Assets/Prefabs/Characters/Julia_Formal.prefab";

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
            {"Hip_R", "UpperLeg.R"},
            {"Knee_R", "LowerLeg.R"},
            {"Ankle_R", "Foot.R"},
            {"Toes_L", "Toes.L"},
            {"Toes_R", "Toes.R"},
            // Fingers
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
        };

        private Vector2 _scrollPosition;
        private List<FixResult> _results = new List<FixResult>();
        private bool _dryRun = true;

        private class FixResult
        {
            public string ObjectPath;
            public string ComponentType;
            public string FieldName;
            public string OldBoneName;
            public string NewBoneName;
            public bool Fixed;
            public string Message;
        }

        [MenuItem("Tools/Fix Julia_Formal References")]
        public static void ShowWindow()
        {
            var window = GetWindow<JuliaFormalFixer>("Fix Julia_Formal");
            window.minSize = new Vector2(700, 500);
        }

        [MenuItem("Tools/Clean Cargo Lists in Julia_Formal")]
        public static void CleanCargoLists()
        {
            var contents = PrefabUtility.LoadPrefabContents(JULIA_FORMAL_PATH);
            try
            {
                // Find CharacterInventory component
                var inventory = contents.GetComponentInChildren<MonoBehaviour>(true);
                foreach (var comp in contents.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (comp == null) continue;
                    var type = comp.GetType();
                    if (type.Name != "CharacterInventory") continue;

                    // Get _cargos field
                    var field = type.GetField("_cargos", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (field == null) continue;

                    var list = field.GetValue(comp) as System.Collections.IList;
                    if (list == null) continue;

                    int removed = 0;
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        var item = list[i];
                        if (item == null || (item is UnityEngine.Object obj && obj == null))
                        {
                            list.RemoveAt(i);
                            removed++;
                        }
                    }

                    if (removed > 0)
                    {
                        Debug.Log($"Removed {removed} null entries from CharacterInventory._cargos");
                        EditorUtility.SetDirty(comp);
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(contents, JULIA_FORMAL_PATH);
                Debug.Log("Saved Julia_Formal.prefab");
                EditorUtility.DisplayDialog("Done", "Cleaned cargo lists", "OK");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Julia_Formal Reference Fixer", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "This tool automatically fixes broken Transform references in Julia_Formal.prefab:\n\n" +
                "1. Compares Julia.prefab and Julia_Formal.prefab\n" +
                "2. Finds null Transform fields that have values in Julia\n" +
                "3. Maps old bone names to new bone names\n" +
                "4. Sets correct references in Julia_Formal\n\n" +
                "Use 'Analyze' first to see what will be fixed, then 'Fix' to apply.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("1. Analyze (Dry Run)", GUILayout.Height(35)))
            {
                _dryRun = true;
                AnalyzeAndFix();
            }

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("2. FIX REFERENCES", GUILayout.Height(35)))
            {
                _dryRun = false;
                AnalyzeAndFix();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Statistics
            if (_results.Count > 0)
            {
                var fixable = _results.Count(r => !string.IsNullOrEmpty(r.NewBoneName));
                var fixed_ = _results.Count(r => r.Fixed);
                var notFound = _results.Count(r => string.IsNullOrEmpty(r.NewBoneName));

                var msg = _dryRun
                    ? $"Analysis complete:\n  - Can fix: {fixable}\n  - Cannot find target: {notFound}"
                    : $"Fixed: {fixed_}/{_results.Count}";

                EditorGUILayout.HelpBox(msg, fixable > 0 ? MessageType.Warning : MessageType.Info);
            }

            EditorGUILayout.Space(5);

            // Results
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var result in _results)
            {
                Color bgColor = result.Fixed ? new Color(0.8f, 1f, 0.8f) :
                                !string.IsNullOrEmpty(result.NewBoneName) ? new Color(1f, 1f, 0.8f) :
                                new Color(1f, 0.8f, 0.8f);

                GUI.backgroundColor = bgColor;
                EditorGUILayout.BeginVertical("box");
                GUI.backgroundColor = Color.white;

                EditorGUILayout.LabelField($"{result.ObjectPath}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Component: {result.ComponentType}");
                EditorGUILayout.LabelField($"Field: {result.FieldName}");
                EditorGUILayout.LabelField($"Old bone: {result.OldBoneName} → New bone: {result.NewBoneName}");

                if (!string.IsNullOrEmpty(result.Message))
                {
                    EditorGUILayout.LabelField(result.Message, EditorStyles.miniLabel);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.EndScrollView();
        }

        private void AnalyzeAndFix()
        {
            _results.Clear();

            var juliaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(JULIA_PATH);
            var formalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(JULIA_FORMAL_PATH);

            if (juliaPrefab == null || formalPrefab == null)
            {
                Debug.LogError("Could not load prefabs!");
                return;
            }

            // For fixing, we need to load prefab contents
            GameObject formalContents = null;
            if (!_dryRun)
            {
                formalContents = PrefabUtility.LoadPrefabContents(JULIA_FORMAL_PATH);
            }

            try
            {
                var formalRoot = _dryRun ? formalPrefab.transform : formalContents.transform;

                // Build bone dictionaries for both prefabs
                var juliaBones = BuildBoneDictionary(juliaPrefab.transform);
                var formalBones = BuildBoneDictionary(formalRoot);

                Debug.Log($"Julia bones: {juliaBones.Count}, Formal bones: {formalBones.Count}");

                // NEW APPROACH: Find components by type, not by path
                // This handles renamed root object (Julia -> Julia_Formal)
                FindAndFixByComponentType(juliaPrefab, formalRoot.gameObject, juliaBones, formalBones);

                // Save if not dry run
                if (!_dryRun && formalContents != null)
                {
                    PrefabUtility.SaveAsPrefabAsset(formalContents, JULIA_FORMAL_PATH);
                    Debug.Log($"Saved {JULIA_FORMAL_PATH}");

                    EditorUtility.DisplayDialog("Success",
                        $"Fixed {_results.Count(r => r.Fixed)} references in Julia_Formal.prefab",
                        "OK");
                }
            }
            finally
            {
                if (formalContents != null)
                {
                    PrefabUtility.UnloadPrefabContents(formalContents);
                }
            }
        }

        private void FindAndFixByComponentType(GameObject juliaPrefab, GameObject formalPrefab,
            Dictionary<string, Transform> juliaBones, Dictionary<string, Transform> formalBones)
        {
            // Get all MonoBehaviours from both prefabs
            var juliaComponents = juliaPrefab.GetComponentsInChildren<MonoBehaviour>(true);
            var formalComponents = formalPrefab.GetComponentsInChildren<MonoBehaviour>(true);

            // Group by type
            var juliaByType = juliaComponents.Where(c => c != null).GroupBy(c => c.GetType()).ToDictionary(g => g.Key, g => g.ToList());
            var formalByType = formalComponents.Where(c => c != null).GroupBy(c => c.GetType()).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var kvp in juliaByType)
            {
                var compType = kvp.Key;
                var juliaComps = kvp.Value;

                if (!formalByType.TryGetValue(compType, out var formalComps))
                    continue;

                // Match components by index (assuming same order)
                for (int i = 0; i < juliaComps.Count && i < formalComps.Count; i++)
                {
                    var juliaComp = juliaComps[i];
                    var formalComp = formalComps[i];

                    string path = GetGameObjectPath(formalComp.gameObject);
                    CompareAndFixFields(path, juliaComp, formalComp, juliaBones, formalBones);
                }
            }
        }

        private string GetGameObjectPath(GameObject obj)
        {
            var parts = new List<string>();
            var current = obj.transform;
            while (current != null)
            {
                parts.Insert(0, current.name);
                current = current.parent;
            }
            return string.Join("/", parts);
        }

        private Dictionary<string, Transform> BuildBoneDictionary(Transform root)
        {
            var dict = new Dictionary<string, Transform>();
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!dict.ContainsKey(t.name))
                    dict[t.name] = t;
            }
            return dict;
        }

        private void CompareAndFixFields(string path, Component juliaComp, Component formalComp,
            Dictionary<string, Transform> juliaBones, Dictionary<string, Transform> formalBones)
        {
            var type = juliaComp.GetType();
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                // Only check serialized Transform fields
                if (field.FieldType != typeof(Transform))
                    continue;

                if (field.IsPrivate && field.GetCustomAttribute<SerializeField>() == null)
                    continue;

                try
                {
                    var juliaValue = field.GetValue(juliaComp) as Transform;
                    var formalValue = field.GetValue(formalComp) as Transform;

                    // Skip if Julia doesn't have value or Formal already has value
                    if (juliaValue == null || formalValue != null)
                        continue;

                    // Julia has value, Formal is null - this needs fixing
                    string oldBoneName = juliaValue.name;
                    string newBoneName = null;
                    Transform newBone = null;

                    // Try to find new bone
                    // 1. Try bone mapping
                    if (BoneMapping.TryGetValue(oldBoneName, out string mappedName))
                    {
                        if (formalBones.TryGetValue(mappedName, out newBone))
                        {
                            newBoneName = mappedName;
                        }
                    }

                    // 2. Try same name (for non-bone objects)
                    if (newBone == null && formalBones.TryGetValue(oldBoneName, out newBone))
                    {
                        newBoneName = oldBoneName;
                    }

                    // 3. Try finding object by path relative to Character
                    if (newBone == null)
                    {
                        string relativePath = GetRelativePath(juliaValue, "Character");
                        if (!string.IsNullOrEmpty(relativePath))
                        {
                            var characterFormal = formalComp.transform.root.Find("Character");
                            if (characterFormal != null)
                            {
                                newBone = characterFormal.Find(relativePath);
                                if (newBone != null)
                                    newBoneName = newBone.name;
                            }
                        }
                    }

                    var result = new FixResult
                    {
                        ObjectPath = path,
                        ComponentType = type.Name,
                        FieldName = field.Name,
                        OldBoneName = oldBoneName,
                        NewBoneName = newBoneName ?? "(not found)",
                        Fixed = false
                    };

                    if (newBone != null && !_dryRun)
                    {
                        field.SetValue(formalComp, newBone);
                        result.Fixed = true;
                        result.Message = "FIXED!";
                        Debug.Log($"Fixed {type.Name}.{field.Name}: {oldBoneName} -> {newBoneName}");
                    }
                    else if (newBone == null)
                    {
                        result.Message = "Could not find target bone in new skeleton";
                    }

                    _results.Add(result);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Error processing {type.Name}.{field.Name}: {e.Message}");
                }
            }
        }

        private string GetRelativePath(Transform target, string rootName)
        {
            var parts = new List<string>();
            var current = target;

            while (current != null && current.name != rootName)
            {
                parts.Insert(0, current.name);
                current = current.parent;
            }

            if (current == null || current.name != rootName)
                return null;

            return string.Join("/", parts);
        }
    }
}
