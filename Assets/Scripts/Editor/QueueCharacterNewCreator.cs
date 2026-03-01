using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Utility;

namespace Editor
{
    /// <summary>
    /// Creates QueueCharacter_New prefab with 5 new character models from Assets/New/Workers/.
    /// Supports skin switching via SimpleSkinSwitcher.
    /// </summary>
    public class QueueCharacterNewCreator : EditorWindow
    {
        private const string SOURCE_PREFAB_PATH = "Assets/Prefabs/Characters/QueueCharacter.prefab";
        private const string OUTPUT_PATH = "Assets/Prefabs/Characters/QueueCharacter_New.prefab";
        private const string CONTROLLER_PATH = "Assets/Animations/Characters/Controller_NewQueueCharacter.controller";

        private static readonly string[] FBX_PATHS = {
            "Assets/New/Workers/Worker/Worker.fbx",
            "Assets/New/Workers/Adventurer/Adventurer.fbx",
            "Assets/New/Workers/Animated Woman/Casual.fbx",
            "Assets/New/Workers/Suit/Suit.fbx",
            "Assets/New/Workers/Punk/Punk.fbx"
        };

        private static readonly string[] MODEL_NAMES = {
            "Worker",
            "Adventurer",
            "Casual",
            "Suit",
            "Punk"
        };

        private Vector2 scrollPos;
        private bool showAnalysis;
        private string analysisLog = "";

        [MenuItem("Tools/QueueCharacter_New/2. Create Prefab")]
        public static void ShowWindow()
        {
            var window = GetWindow<QueueCharacterNewCreator>("QueueCharacter_New Creator");
            window.minSize = new Vector2(500, 600);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("QueueCharacter_New Prefab Creator", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Creates QueueCharacter_New.prefab with:\n\n" +
                "1. 5 character models from Assets/New/Workers/:\n" +
                "   - Worker, Adventurer, Casual, Suit, Punk\n\n" +
                "2. SimpleSkinSwitcher configured for all 5 models\n\n" +
                "3. Preserved components: QueueCharacter, BaseCargo\n\n" +
                "4. New Animator Controller: Controller_NewQueueCharacter",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // Check prerequisites
            bool controllerExists = System.IO.File.Exists(CONTROLLER_PATH.Replace("Assets/", Application.dataPath + "/"));

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Prerequisites:", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Toggle("Source Prefab", AssetDatabase.LoadAssetAtPath<GameObject>(SOURCE_PREFAB_PATH) != null);
            EditorGUILayout.Toggle("Animator Controller", controllerExists);
            EditorGUI.EndDisabledGroup();

            if (!controllerExists)
            {
                EditorGUILayout.HelpBox("Please run 'Tools > QueueCharacter_New > 1. Generate Animator Controller' first!", MessageType.Warning);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // FBX Models list
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("FBX Models to use:", EditorStyles.boldLabel);
            for (int i = 0; i < FBX_PATHS.Length; i++)
            {
                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(FBX_PATHS[i]);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"  {i + 1}. {MODEL_NAMES[i]}", GUILayout.Width(120));
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.Toggle(fbx != null, GUILayout.Width(20));
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.LabelField(FBX_PATHS[i], EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Analyze Structure", GUILayout.Height(30)))
            {
                AnalyzeStructure();
                showAnalysis = true;
            }

            EditorGUILayout.Space(5);

            GUI.backgroundColor = Color.green;
            EditorGUI.BeginDisabledGroup(!controllerExists);
            if (GUILayout.Button("CREATE QUEUECHARACTER_NEW PREFAB", GUILayout.Height(50)))
            {
                CreatePrefab();
            }
            EditorGUI.EndDisabledGroup();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField($"Output: {OUTPUT_PATH}");

            // Analysis log
            if (showAnalysis && !string.IsNullOrEmpty(analysisLog))
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Analysis:", EditorStyles.boldLabel);
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
                EditorGUILayout.TextArea(analysisLog, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }
        }

        private void AnalyzeStructure()
        {
            analysisLog = "";
            Log("=== Analyzing QueueCharacter Structure ===\n");

            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SOURCE_PREFAB_PATH);
            if (sourcePrefab == null)
            {
                Log("ERROR: Source prefab not found!");
                return;
            }

            // Analyze source prefab
            Log($"Source: {SOURCE_PREFAB_PATH}");
            Log($"Root: {sourcePrefab.name}");
            Log("");

            // List components
            Log("Components on root:");
            foreach (var comp in sourcePrefab.GetComponents<Component>())
            {
                if (comp != null)
                    Log($"  - {comp.GetType().Name}");
            }
            Log("");

            // List children
            Log("Children:");
            foreach (Transform child in sourcePrefab.transform)
            {
                Log($"  - {child.name}");
                foreach (Transform grandchild in child)
                {
                    Log($"      - {grandchild.name}");
                }
            }
            Log("");

            // Analyze FBX models
            Log("=== FBX Models ===\n");
            foreach (var path in FBX_PATHS)
            {
                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (fbx != null)
                {
                    Log($"{path}:");
                    var smr = fbx.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (smr != null)
                    {
                        Log($"  SkinnedMeshRenderer: {smr.name}");
                        Log($"  Bones count: {smr.bones?.Length ?? 0}");
                        Log($"  Root bone: {smr.rootBone?.name ?? "null"}");
                    }
                    else
                    {
                        Log("  No SkinnedMeshRenderer found");
                    }
                    Log("");
                }
                else
                {
                    Log($"{path}: NOT FOUND");
                    Log("");
                }
            }

            Debug.Log(analysisLog);
        }

        private void Log(string message)
        {
            analysisLog += message + "\n";
        }

        private void CreatePrefab()
        {
            // Load source prefab
            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SOURCE_PREFAB_PATH);
            if (sourcePrefab == null)
            {
                EditorUtility.DisplayDialog("Error", $"Source prefab not found at {SOURCE_PREFAB_PATH}", "OK");
                return;
            }

            // Load animator controller
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CONTROLLER_PATH);
            if (controller == null)
            {
                EditorUtility.DisplayDialog("Error",
                    $"Animator controller not found at {CONTROLLER_PATH}\n\n" +
                    "Please run 'Tools > QueueCharacter_New > 1. Generate Animator Controller' first!",
                    "OK");
                return;
            }

            // Load FBX models
            var fbxModels = new List<GameObject>();
            for (int i = 0; i < FBX_PATHS.Length; i++)
            {
                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(FBX_PATHS[i]);
                if (fbx == null)
                {
                    EditorUtility.DisplayDialog("Error", $"FBX not found: {FBX_PATHS[i]}", "OK");
                    return;
                }
                fbxModels.Add(fbx);
            }

            // Load prefab for editing
            var contents = PrefabUtility.LoadPrefabContents(SOURCE_PREFAB_PATH);

            try
            {
                // Rename root
                contents.name = "QueueCharacter_New";

                // Find key objects
                var deformationSystem = contents.transform.Find("DeformationSystem");
                var geometry = contents.transform.Find("Geometry");
                var cargo = contents.transform.Find("Cargo");
                var widgetOrigin = contents.transform.Find("WidgetOrigin");

                if (deformationSystem == null)
                {
                    Debug.LogError("DeformationSystem not found!");
                    return;
                }

                // Step 1: Delete old skeleton and geometry
                Debug.Log("Removing old skeleton and geometry...");
                Object.DestroyImmediate(deformationSystem.gameObject);
                if (geometry != null)
                    Object.DestroyImmediate(geometry.gameObject);

                // Step 2: Instantiate first FBX and extract CharacterArmature (skeleton)
                Debug.Log("Adding new skeleton from FBX...");
                var tempFbxInstance = Object.Instantiate(fbxModels[0]);

                // Find CharacterArmature inside FBX (skeleton root)
                var characterArmature = FindBoneByName(tempFbxInstance.transform, "CharacterArmature");
                if (characterArmature == null)
                {
                    // Fallback: look for any armature-like object
                    characterArmature = FindSkeletonRoot(tempFbxInstance.transform);
                }

                if (characterArmature == null)
                {
                    Debug.LogError("CharacterArmature not found in FBX!");
                    Object.DestroyImmediate(tempFbxInstance);
                    return;
                }

                // Move CharacterArmature directly under root (important for animation paths!)
                characterArmature.SetParent(contents.transform, false);
                characterArmature.SetAsFirstSibling();
                Debug.Log($"Moved {characterArmature.name} to root level");

                // Remove mesh objects from skeleton (we'll recreate them in Geometry)
                var meshesToRemove = characterArmature.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (var smr in meshesToRemove)
                {
                    Object.DestroyImmediate(smr.gameObject);
                }

                // Destroy the temp FBX container (now empty)
                Object.DestroyImmediate(tempFbxInstance);

                // Reference to skeleton for bone binding
                var skeletonRoot = characterArmature;

                // Step 3: Create Geometry container for all skin meshes
                var newGeometry = new GameObject("Geometry");
                newGeometry.transform.SetParent(contents.transform, false);

                // Step 4: Add ALL SkinnedMeshRenderers for each model (Body, Feet, Head, Legs)
                var skinGroupRenderers = new List<Renderer>(); // First renderer of each group for SimpleSkinSwitcher

                for (int i = 0; i < fbxModels.Count; i++)
                {
                    var modelName = MODEL_NAMES[i];

                    // Get ALL SkinnedMeshRenderers from FBX (not just one!)
                    var allSmrs = fbxModels[i].GetComponentsInChildren<SkinnedMeshRenderer>();
                    if (allSmrs.Length == 0)
                    {
                        Debug.LogWarning($"No SkinnedMeshRenderers in {FBX_PATHS[i]}");
                        continue;
                    }

                    Debug.Log($"FBX {modelName} has {allSmrs.Length} SkinnedMeshRenderers");

                    // Create parent container for this skin group
                    var skinGroup = new GameObject(modelName);
                    skinGroup.transform.SetParent(newGeometry.transform, false);

                    // Add MeshRenderer to parent so SimpleSkinSwitcher can use it
                    // (SimpleSkinSwitcher enables/disables gameObject, so this enables all children)
                    var groupRenderer = skinGroup.AddComponent<MeshRenderer>();
                    skinGroupRenderers.Add(groupRenderer);

                    // Copy each mesh part (Body, Feet, Head, Legs)
                    foreach (var srcSmr in allSmrs)
                    {
                        // Create child object for this mesh part
                        var meshPartName = $"{modelName}_{srcSmr.name}";
                        var meshPart = new GameObject(meshPartName);
                        meshPart.transform.SetParent(skinGroup.transform, false);

                        // Copy SkinnedMeshRenderer
                        var newSmr = meshPart.AddComponent<SkinnedMeshRenderer>();
                        newSmr.sharedMesh = srcSmr.sharedMesh;
                        newSmr.sharedMaterials = srcSmr.sharedMaterials;

                        // Rebind bones to skeleton (CharacterArmature)
                        RebindToSkeleton(newSmr, srcSmr, skeletonRoot);

                        Debug.Log($"  Added mesh: {meshPartName}");
                    }

                    // Set active state (first is active, rest inactive)
                    skinGroup.SetActive(i == 0);
                    Debug.Log($"Added skin group: {modelName} with {allSmrs.Length} parts");
                }

                // Step 5: Configure SimpleSkinSwitcher with group renderers
                var skinSwitcher = contents.GetComponent<SimpleSkinSwitcher>();
                if (skinSwitcher == null)
                {
                    skinSwitcher = contents.AddComponent<SimpleSkinSwitcher>();
                }

                // Use reflection to set private field
                var renderersField = typeof(SimpleSkinSwitcher).GetField("_renderers",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (renderersField != null)
                {
                    renderersField.SetValue(skinSwitcher, skinGroupRenderers.ToArray());
                    Debug.Log($"SimpleSkinSwitcher configured with {skinGroupRenderers.Count} skin groups");
                }

                // Step 6: Configure Animator
                var animator = contents.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = contents.AddComponent<Animator>();
                }
                animator.runtimeAnimatorController = controller;
                Debug.Log("Animator controller assigned");

                // Step 7: Reposition Cargo if needed
                if (cargo != null)
                {
                    // Find spine/chest bone for cargo attachment
                    var chestBone = FindBoneByName(skeletonRoot, "Chest") ??
                                    FindBoneByName(skeletonRoot, "Torso") ??
                                    FindBoneByName(skeletonRoot, "Spine");

                    if (chestBone != null)
                    {
                        Debug.Log($"Cargo reference bone: {chestBone.name}");
                    }
                }

                // Step 8: Update QueueCharacter component references
                var queueCharacter = contents.GetComponent<Playable.Gameplay.NPCs.QueueCharacter>();
                if (queueCharacter != null)
                {
                    // Update serialized fields using SerializedObject
                    var so = new SerializedObject(queueCharacter);

                    var animatorProp = so.FindProperty("_animator");
                    if (animatorProp != null)
                        animatorProp.objectReferenceValue = animator;

                    var controllerProp = so.FindProperty("_controller");
                    if (controllerProp != null)
                        controllerProp.objectReferenceValue = controller;

                    so.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log("QueueCharacter component updated");
                }

                // Save new prefab
                PrefabUtility.SaveAsPrefabAsset(contents, OUTPUT_PATH);

                Debug.Log($"\n=== SUCCESS! Created {OUTPUT_PATH} ===");
                Debug.Log($"Skin groups: {skinGroupRenderers.Count}");

                EditorUtility.DisplayDialog("Success",
                    $"Created {OUTPUT_PATH}\n\n" +
                    $"Skin groups: {skinGroupRenderers.Count}\n" +
                    $"  - {string.Join("\n  - ", MODEL_NAMES)}\n\n" +
                    "Each group contains Body, Feet, Head, Legs.\n\n" +
                    "Test in Play mode with QueueController!",
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

        private Transform FindSkeletonRoot(Transform root)
        {
            // Common skeleton root names
            string[] rootNames = { "Root_M", "Root", "Armature", "CharacterArmature", "Skeleton" };

            foreach (var name in rootNames)
            {
                var bone = FindBoneByName(root, name);
                if (bone != null)
                    return bone;
            }

            // Fallback: find first child that's not a SkinnedMeshRenderer
            foreach (Transform child in root)
            {
                if (child.GetComponent<SkinnedMeshRenderer>() == null)
                    return child;
            }

            return null;
        }

        private Transform FindBoneByName(Transform root, string name)
        {
            if (root.name == name)
                return root;

            foreach (Transform child in root)
            {
                var result = FindBoneByName(child, name);
                if (result != null)
                    return result;
            }

            return null;
        }

        private void RebindToSkeleton(SkinnedMeshRenderer target, SkinnedMeshRenderer source, Transform skeletonRoot)
        {
            if (source.bones == null || source.bones.Length == 0)
            {
                Debug.LogWarning($"Source {source.name} has no bones");
                return;
            }

            var newBones = new Transform[source.bones.Length];

            for (int i = 0; i < source.bones.Length; i++)
            {
                if (source.bones[i] == null) continue;

                var boneName = source.bones[i].name;
                var newBone = FindBoneByName(skeletonRoot, boneName);

                if (newBone != null)
                {
                    newBones[i] = newBone;
                }
                else
                {
                    Debug.LogWarning($"Bone not found: {boneName}");
                }
            }

            target.bones = newBones;

            // Set root bone
            if (source.rootBone != null)
            {
                target.rootBone = FindBoneByName(skeletonRoot, source.rootBone.name);
            }
        }
    }
}
