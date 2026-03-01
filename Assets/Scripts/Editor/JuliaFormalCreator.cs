using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Editor
{
    /// <summary>
    /// Creates Julia_Formal by duplicating Julia and replacing ONLY the visual model,
    /// keeping ALL gameplay objects intact.
    /// </summary>
    public class JuliaFormalCreator : EditorWindow
    {
        private const string JULIA_PATH = "Assets/Prefabs/Characters/Julia.prefab";
        private const string FORMAL_FBX_PATH = "Assets/Art/Models/Characters/Formal.fbx";
        private const string OUTPUT_PATH = "Assets/Prefabs/Characters/Julia_Formal.prefab";
        private const string CONTROLLER_PATH = "Assets/Animations/Characters/Controller_Formal.controller";

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

        [MenuItem("Tools/Create Julia_Formal (Clean)")]
        public static void ShowWindow()
        {
            var window = GetWindow<JuliaFormalCreator>("Create Julia_Formal");
            window.minSize = new Vector2(550, 500);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Create Julia_Formal Prefab", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool creates Julia_Formal.prefab correctly:\n\n" +
                "1. Duplicates Julia.prefab (keeps ALL structure)\n" +
                "2. Finds 'Character' object with Animator\n" +
                "3. Collects gameplay objects from skeleton (INVENTORY, Tools)\n" +
                "4. Replaces DeformationSystem + Geometry with Formal.fbx model\n" +
                "5. Re-parents gameplay objects to new skeleton bones\n" +
                "6. Assigns Controller_Formal animator\n\n" +
                "This preserves [Mechanics], [Visuals], and all components!",
                MessageType.Info);

            EditorGUILayout.Space(20);

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("CREATE JULIA_FORMAL PREFAB", GUILayout.Height(50)))
            {
                CreatePrefab();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Output: " + OUTPUT_PATH);
        }

        private void CreatePrefab()
        {
            // Load source assets
            var juliaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(JULIA_PATH);
            var formalFbx = AssetDatabase.LoadAssetAtPath<GameObject>(FORMAL_FBX_PATH);
            var formalController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CONTROLLER_PATH);

            if (juliaPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", $"Julia prefab not found at {JULIA_PATH}", "OK");
                return;
            }

            if (formalFbx == null)
            {
                EditorUtility.DisplayDialog("Error", $"Formal FBX not found at {FORMAL_FBX_PATH}", "OK");
                return;
            }

            // Load prefab for editing
            var contents = PrefabUtility.LoadPrefabContents(JULIA_PATH);

            try
            {
                // Rename root
                contents.name = "Julia_Formal";

                // Find Character object (has Animator)
                var characterObj = contents.transform.Find("Character");
                if (characterObj == null)
                {
                    // Try finding by Animator component
                    var animator = contents.GetComponentInChildren<Animator>();
                    if (animator != null)
                        characterObj = animator.transform;
                }

                if (characterObj == null)
                {
                    Debug.LogError("Could not find Character object!");
                    return;
                }

                Debug.Log($"Found Character object: {characterObj.name}");

                // Find DeformationSystem (old skeleton) and Geometry (old meshes)
                var deformationSystem = characterObj.Find("DeformationSystem");
                var geometry = characterObj.Find("Geometry");

                if (deformationSystem == null)
                {
                    Debug.LogError("DeformationSystem not found!");
                    return;
                }

                // Step 1: Collect all gameplay objects from skeleton
                var gameplayObjects = new List<GameplayObjectInfo>();
                CollectGameplayObjects(deformationSystem, gameplayObjects);

                Debug.Log($"Collected {gameplayObjects.Count} gameplay objects from skeleton:");
                foreach (var gpo in gameplayObjects)
                {
                    Debug.Log($"  - {gpo.GameObject.name} (parent bone: {gpo.ParentBoneName})");
                }

                // Step 2: Temporarily move gameplay objects to Character root
                foreach (var gpo in gameplayObjects)
                {
                    gpo.GameObject.transform.SetParent(characterObj, true);
                }

                // Step 3: Delete old skeleton and meshes
                Object.DestroyImmediate(deformationSystem.gameObject);
                if (geometry != null)
                    Object.DestroyImmediate(geometry.gameObject);

                Debug.Log("Deleted old skeleton and meshes");

                // Step 4: Instantiate Formal.fbx content
                var formalInstance = Object.Instantiate(formalFbx);

                // Move all children from Formal instance to Character
                var formalChildren = new List<Transform>();
                foreach (Transform child in formalInstance.transform)
                {
                    formalChildren.Add(child);
                }

                foreach (var child in formalChildren)
                {
                    child.SetParent(characterObj, false);
                }

                Object.DestroyImmediate(formalInstance);

                Debug.Log($"Added {formalChildren.Count} children from Formal.fbx");

                // Step 5: Build new bone dictionary
                var newBones = new Dictionary<string, Transform>();
                foreach (var t in characterObj.GetComponentsInChildren<Transform>(true))
                {
                    if (!newBones.ContainsKey(t.name))
                        newBones[t.name] = t;
                }

                // Step 6: Re-parent gameplay objects to new skeleton
                int reparented = 0;
                foreach (var gpo in gameplayObjects)
                {
                    // Find target bone in new skeleton
                    Transform targetBone = null;

                    // Try mapped name first
                    if (BoneMapping.TryGetValue(gpo.ParentBoneName, out string newBoneName))
                    {
                        newBones.TryGetValue(newBoneName, out targetBone);
                    }

                    // Try same name
                    if (targetBone == null)
                    {
                        newBones.TryGetValue(gpo.ParentBoneName, out targetBone);
                    }

                    if (targetBone != null)
                    {
                        gpo.GameObject.transform.SetParent(targetBone, false);
                        gpo.GameObject.transform.localPosition = gpo.LocalPosition;
                        gpo.GameObject.transform.localRotation = gpo.LocalRotation;
                        gpo.GameObject.transform.localScale = gpo.LocalScale;
                        Debug.Log($"Re-parented {gpo.GameObject.name}: {gpo.ParentBoneName} -> {targetBone.name}");
                        reparented++;
                    }
                    else
                    {
                        Debug.LogWarning($"Could not find target bone for {gpo.GameObject.name} (was: {gpo.ParentBoneName})");
                        // Keep at Character level as fallback
                    }
                }

                Debug.Log($"Re-parented {reparented}/{gameplayObjects.Count} gameplay objects");

                // Step 7: Set up Animator
                var newAnimator = characterObj.GetComponent<Animator>();
                if (newAnimator == null)
                    newAnimator = characterObj.gameObject.AddComponent<Animator>();

                if (formalController != null)
                {
                    newAnimator.runtimeAnimatorController = formalController;
                    Debug.Log("Assigned Controller_Formal");
                }
                else
                {
                    Debug.LogWarning($"Controller not found at {CONTROLLER_PATH}. Run 'Generate Formal Animator Controller' first.");
                }

                // Save new prefab
                PrefabUtility.SaveAsPrefabAsset(contents, OUTPUT_PATH);

                Debug.Log($"\n=== SUCCESS! Created {OUTPUT_PATH} ===");
                Debug.Log($"Gameplay objects preserved: {gameplayObjects.Count}");
                Debug.Log($"Re-parented to new skeleton: {reparented}");

                EditorUtility.DisplayDialog("Success",
                    $"Created {OUTPUT_PATH}\n\n" +
                    $"Gameplay objects: {gameplayObjects.Count}\n" +
                    $"Re-parented: {reparented}\n\n" +
                    "Check the prefab and test in Play mode!",
                    "OK");

                // Select new prefab
                var newPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OUTPUT_PATH);
                Selection.activeObject = newPrefab;
                EditorGUIUtility.PingObject(newPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private class GameplayObjectInfo
        {
            public GameObject GameObject;
            public string ParentBoneName;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public Vector3 LocalScale;
        }

        private void CollectGameplayObjects(Transform root, List<GameplayObjectInfo> results)
        {
            var children = new List<Transform>();
            foreach (Transform child in root)
            {
                children.Add(child);
            }

            foreach (var child in children)
            {
                if (IsGameplayObject(child.gameObject))
                {
                    results.Add(new GameplayObjectInfo
                    {
                        GameObject = child.gameObject,
                        ParentBoneName = child.parent.name,
                        LocalPosition = child.localPosition,
                        LocalRotation = child.localRotation,
                        LocalScale = child.localScale
                    });
                    // Don't recurse into gameplay objects - they move with all children
                }
                else
                {
                    // Recurse into skeleton bones
                    CollectGameplayObjects(child, results);
                }
            }
        }

        private bool IsGameplayObject(GameObject obj)
        {
            // Has MonoBehaviour components
            if (obj.GetComponents<MonoBehaviour>().Length > 0)
                return true;

            // Has any component other than Transform
            var components = obj.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp == null) continue;
                var type = comp.GetType();
                if (type != typeof(Transform))
                    return true;
            }

            // Name doesn't look like a skeleton bone
            string name = obj.name;
            bool looksLikeBone =
                name.EndsWith("_M") || name.EndsWith("_L") || name.EndsWith("_R") ||
                name.StartsWith("Root") || name.StartsWith("Hip") || name.StartsWith("Spine") ||
                name.StartsWith("Chest") || name.StartsWith("Neck") || name.StartsWith("Head") ||
                name.StartsWith("Shoulder") || name.StartsWith("Elbow") || name.StartsWith("Wrist") ||
                name.StartsWith("Knee") || name.StartsWith("Ankle") || name.StartsWith("Thumb") ||
                name.StartsWith("Index") || name.StartsWith("Middle") || name.StartsWith("Ring") ||
                name.StartsWith("Pinky") || name.StartsWith("Scapula") || name.StartsWith("Finger") ||
                name == "DeformationSystem" || name == "Geometry";

            if (!looksLikeBone)
                return true;

            return false;
        }
    }
}
