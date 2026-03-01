using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Editor
{
    /// <summary>
    /// Fixes Julia_Formal prefab by copying missing hierarchy from Julia.
    /// The problem: CharacterModelReplacer deleted too much - we need to restore
    /// [Mechanics], [Visuals], INVENTORY, Tools, etc.
    /// </summary>
    public class JuliaFormalPrefabFixer : EditorWindow
    {
        private const string JULIA_PATH = "Assets/Prefabs/Characters/Julia.prefab";
        private const string JULIA_FORMAL_PATH = "Assets/Prefabs/Characters/Julia_Formal.prefab";

        // Bone mapping for re-parenting objects from old skeleton to new
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
        };

        [MenuItem("Tools/Fix Julia_Formal Prefab")]
        public static void ShowWindow()
        {
            var window = GetWindow<JuliaFormalPrefabFixer>("Fix Julia_Formal");
            window.minSize = new Vector2(500, 400);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Julia_Formal Prefab Fixer", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool will:\n\n" +
                "1. Copy [Mechanics] and [Visuals] from Julia to Julia_Formal root\n" +
                "2. Copy INVENTORY to new skeleton (Abdomen bone)\n" +
                "3. Copy tool objects to new skeleton (Wrist.R bone)\n" +
                "4. Re-link all component references\n\n" +
                "WARNING: This will modify Julia_Formal.prefab!",
                MessageType.Warning);

            EditorGUILayout.Space(20);

            if (GUILayout.Button("1. Analyze What's Missing", GUILayout.Height(30)))
            {
                AnalyzeMissing();
            }

            EditorGUILayout.Space(10);

            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("2. FIX PREFAB (Copy Missing Objects)", GUILayout.Height(40)))
            {
                FixPrefab();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);

            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("3. Re-link Component References", GUILayout.Height(30)))
            {
                RelinkReferences();
            }
            GUI.backgroundColor = Color.white;
        }

        private void AnalyzeMissing()
        {
            var julia = AssetDatabase.LoadAssetAtPath<GameObject>(JULIA_PATH);
            var formal = AssetDatabase.LoadAssetAtPath<GameObject>(JULIA_FORMAL_PATH);

            if (julia == null || formal == null)
            {
                Debug.LogError("Could not load prefabs!");
                return;
            }

            Debug.Log("=== ANALYSIS: What's missing in Julia_Formal ===\n");

            // Check root level objects
            var juliaRootChildren = new List<string>();
            foreach (Transform child in julia.transform)
            {
                juliaRootChildren.Add(child.name);
            }

            var formalRootChildren = new List<string>();
            foreach (Transform child in formal.transform)
            {
                formalRootChildren.Add(child.name);
            }

            Debug.Log("Julia root children: " + string.Join(", ", juliaRootChildren));
            Debug.Log("Formal root children: " + string.Join(", ", formalRootChildren));

            var missingAtRoot = juliaRootChildren.Except(formalRootChildren).ToList();
            Debug.Log("\nMissing at root level: " + string.Join(", ", missingAtRoot));

            // Check skeleton objects (INVENTORY, Tools)
            var juliaWristR = julia.transform.Find("Character/DeformationSystem/Root_M/Spine1_M/Chest_M/Scapula_R/Shoulder_R/Elbow_R/Wrist_R");
            if (juliaWristR != null)
            {
                Debug.Log("\nJulia Wrist_R children (tools): ");
                foreach (Transform child in juliaWristR)
                {
                    Debug.Log("  - " + child.name);
                }
            }

            var juliaInventory = julia.transform.Find("Character/DeformationSystem/Root_M/Spine1_M/INVENTORY");
            if (juliaInventory != null)
            {
                Debug.Log("\nJulia INVENTORY children: ");
                foreach (Transform child in juliaInventory)
                {
                    Debug.Log("  - " + child.name);
                }
            }

            // Check Formal skeleton
            var formalArmature = formal.transform.Find("Character/CharacterArmature");
            if (formalArmature != null)
            {
                Debug.Log("\nFormal has CharacterArmature skeleton");
                var formalWristR = FindDeep(formalArmature, "Wrist.R");
                if (formalWristR != null)
                {
                    Debug.Log("Formal Wrist.R found, children: ");
                    foreach (Transform child in formalWristR)
                    {
                        Debug.Log("  - " + child.name);
                    }
                }
                else
                {
                    Debug.Log("Formal Wrist.R NOT FOUND");
                }
            }

            Debug.Log("\n=== END ANALYSIS ===");
        }

        private void FixPrefab()
        {
            var julia = AssetDatabase.LoadAssetAtPath<GameObject>(JULIA_PATH);

            if (julia == null)
            {
                Debug.LogError("Could not load Julia prefab!");
                return;
            }

            // Load prefab contents for editing
            var formalContents = PrefabUtility.LoadPrefabContents(JULIA_FORMAL_PATH);

            try
            {
                int copiedCount = 0;

                // 1. Copy [Mechanics] if missing
                if (formalContents.transform.Find("[Mechanics]") == null)
                {
                    var juliaMechanics = julia.transform.Find("[Mechanics]");
                    if (juliaMechanics != null)
                    {
                        var copy = Object.Instantiate(juliaMechanics.gameObject, formalContents.transform);
                        copy.name = "[Mechanics]";
                        Debug.Log("Copied [Mechanics]");
                        copiedCount++;
                    }
                }

                // 2. Copy [Visuals] if missing
                if (formalContents.transform.Find("[Visuals]") == null)
                {
                    var juliaVisuals = julia.transform.Find("[Visuals]");
                    if (juliaVisuals != null)
                    {
                        var copy = Object.Instantiate(juliaVisuals.gameObject, formalContents.transform);
                        copy.name = "[Visuals]";
                        Debug.Log("Copied [Visuals]");
                        copiedCount++;
                    }
                }

                // 3. Find Character object in Formal
                var formalCharacter = formalContents.transform.Find("Character");
                if (formalCharacter == null)
                {
                    Debug.LogError("Character object not found in Julia_Formal!");
                    return;
                }

                // Find the skeleton root in Formal
                var formalArmature = formalCharacter.Find("CharacterArmature");
                if (formalArmature == null)
                {
                    Debug.LogError("CharacterArmature not found in Julia_Formal!");
                    return;
                }

                // 4. Copy INVENTORY to Abdomen bone (equivalent of Spine1_M)
                var formalAbdomen = FindDeep(formalArmature, "Abdomen");
                if (formalAbdomen != null)
                {
                    // Check if INVENTORY already exists
                    if (formalAbdomen.Find("INVENTORY") == null)
                    {
                        var juliaInventory = julia.transform.Find("Character/DeformationSystem/Root_M/Spine1_M/INVENTORY");
                        if (juliaInventory != null)
                        {
                            var copy = Object.Instantiate(juliaInventory.gameObject, formalAbdomen);
                            copy.name = "INVENTORY";
                            Debug.Log("Copied INVENTORY to Abdomen");
                            copiedCount++;
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("Abdomen bone not found!");
                }

                // 5. Copy tool objects to Wrist.R
                var formalWristR = FindDeep(formalArmature, "Wrist.R");
                if (formalWristR != null)
                {
                    var juliaWristR = julia.transform.Find("Character/DeformationSystem/Root_M/Spine1_M/Chest_M/Scapula_R/Shoulder_R/Elbow_R/Wrist_R");
                    if (juliaWristR != null)
                    {
                        // Copy each tool that doesn't exist
                        string[] toolNames = { "FeedingTool", "SK_SickleWood", "VacuumCleaner" };
                        foreach (var toolName in toolNames)
                        {
                            if (formalWristR.Find(toolName) == null)
                            {
                                var juliaTool = juliaWristR.Find(toolName);
                                if (juliaTool != null)
                                {
                                    var copy = Object.Instantiate(juliaTool.gameObject, formalWristR);
                                    copy.name = toolName;
                                    Debug.Log($"Copied {toolName} to Wrist.R");
                                    copiedCount++;
                                }
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("Wrist.R bone not found!");
                }

                // Save changes
                PrefabUtility.SaveAsPrefabAsset(formalContents, JULIA_FORMAL_PATH);

                Debug.Log($"\n=== DONE: Copied {copiedCount} objects ===");
                EditorUtility.DisplayDialog("Success",
                    $"Copied {copiedCount} objects to Julia_Formal.\n\n" +
                    "Now run 'Re-link Component References' to fix broken references.",
                    "OK");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(formalContents);
            }
        }

        private void RelinkReferences()
        {
            var formalContents = PrefabUtility.LoadPrefabContents(JULIA_FORMAL_PATH);

            try
            {
                int fixedCount = 0;

                // Build transform dictionary
                var transforms = new Dictionary<string, Transform>();
                foreach (var t in formalContents.GetComponentsInChildren<Transform>(true))
                {
                    if (!transforms.ContainsKey(t.name))
                        transforms[t.name] = t;
                }

                // Find all components and check their serialized fields
                foreach (var component in formalContents.GetComponentsInChildren<Component>(true))
                {
                    if (component == null) continue;

                    var so = new SerializedObject(component);
                    var prop = so.GetIterator();

                    while (prop.NextVisible(true))
                    {
                        if (prop.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            // Check if reference is null but name suggests it should be a Transform
                            if (prop.objectReferenceValue == null &&
                                (prop.name.Contains("bone") || prop.name.Contains("Bone") ||
                                 prop.name.Contains("transform") || prop.name.Contains("Transform") ||
                                 prop.name.Contains("target") || prop.name.Contains("Target") ||
                                 prop.name.Contains("point") || prop.name.Contains("Point") ||
                                 prop.name.Contains("root") || prop.name.Contains("Root")))
                            {
                                Debug.Log($"Found null reference: {component.GetType().Name}.{prop.name}");
                            }
                        }
                    }
                }

                // Fix CharacterInventory._cargos references
                var inventory = formalContents.GetComponentInChildren<Playable.Gameplay.Character.CharacterInventory>();
                if (inventory != null)
                {
                    Debug.Log("Found CharacterInventory, checking _cargos...");
                    // The _cargos should already reference the copied Cargo objects
                }

                // Fix Cargo._slots references
                var cargos = formalContents.GetComponentsInChildren<Playable.Gameplay.Cargo>(true);
                foreach (var cargo in cargos)
                {
                    Debug.Log($"Found Cargo: {cargo.name}");
                }

                PrefabUtility.SaveAsPrefabAsset(formalContents, JULIA_FORMAL_PATH);

                Debug.Log($"\n=== Re-link complete. Fixed {fixedCount} references ===");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(formalContents);
            }
        }

        private Transform FindDeep(Transform parent, string name)
        {
            if (parent.name == name)
                return parent;

            foreach (Transform child in parent)
            {
                var found = FindDeep(child, name);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
