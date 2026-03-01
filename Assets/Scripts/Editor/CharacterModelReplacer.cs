using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;

namespace Editor
{
    public class CharacterModelReplacer : EditorWindow
    {
        private const string SOURCE_FBX_PATH = "Assets/Art/Models/Characters/Formal.fbx";
        private const string TARGET_PREFAB_PATH = "Assets/Prefabs/Characters/Julia.prefab";
        private const string NEW_PREFAB_PATH = "Assets/Prefabs/Characters/Julia_Formal.prefab";

        private GameObject sourceFbx;
        private GameObject targetPrefab;

        // Bone mapping: TS_Character_Rig bone name -> Formal.fbx bone name
        // This maps OLD skeleton bones to NEW skeleton bones
        private static readonly Dictionary<string, string> BoneMapping = new Dictionary<string, string>
        {
            // Root/Spine
            {"Root_M", "Root"},
            {"Hip_M", "Hips"},
            {"Spine1_M", "Abdomen"},
            {"Spine2_M", "Torso"},
            {"Chest_M", "Chest"},
            {"Neck_M", "Neck"},
            {"Head_M", "Head"},

            // Left Arm
            {"Scapula_L", "Shoulder.L"},
            {"Shoulder_L", "UpperArm.L"},
            {"Elbow_L", "LowerArm.L"},
            {"Wrist_L", "Wrist.L"},

            // Right Arm
            {"Scapula_R", "Shoulder.R"},
            {"Shoulder_R", "UpperArm.R"},
            {"Elbow_R", "LowerArm.R"},
            {"Wrist_R", "Wrist.R"},

            // Left Leg
            {"Hip_L", "UpperLeg.L"},
            {"Knee_L", "LowerLeg.L"},
            {"Ankle_L", "Foot.L"},

            // Right Leg
            {"Hip_R", "UpperLeg.R"},
            {"Knee_R", "LowerLeg.R"},
            {"Ankle_R", "Foot.R"},

            // Fingers Left
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
            {"ThumbFinger1_L", "Thumb1.L"},
            {"ThumbFinger2_L", "Thumb2.L"},
            {"ThumbFinger3_L", "Thumb3.L"},

            // Fingers Right
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
            {"ThumbFinger1_R", "Thumb1.R"},
            {"ThumbFinger2_R", "Thumb2.R"},
            {"ThumbFinger3_R", "Thumb3.R"},
        };

        // Data structure to store gameplay objects that need to be preserved
        private class SavedGameplayObject
        {
            public GameObject gameObject;
            public string parentBoneName;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
        }

        [MenuItem("Tools/Character Model Replacer")]
        public static void ShowWindow()
        {
            var window = GetWindow<CharacterModelReplacer>("Model Replacer");
            window.minSize = new Vector2(500, 400);
        }

        private void OnEnable()
        {
            LoadAssets();
        }

        private void LoadAssets()
        {
            sourceFbx = AssetDatabase.LoadAssetAtPath<GameObject>(SOURCE_FBX_PATH);
            targetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TARGET_PREFAB_PATH);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Character Model Replacer", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            sourceFbx = (GameObject)EditorGUILayout.ObjectField("Source FBX (Formal)", sourceFbx, typeof(GameObject), false);
            targetPrefab = (GameObject)EditorGUILayout.ObjectField("Original Prefab (Julia)", targetPrefab, typeof(GameObject), false);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField($"New prefab will be saved to: {NEW_PREFAB_PATH}");

            EditorGUILayout.Space(20);

            if (GUILayout.Button("1. Configure FBX Import", GUILayout.Height(30)))
            {
                ConfigureFbxImport();
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("2. Show Bone Mapping", GUILayout.Height(30)))
            {
                ShowBoneMapping();
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("2.5 Create Animator Controller", GUILayout.Height(30)))
            {
                CreateAnimatorControllerForFormal();
            }

            EditorGUILayout.Space(10);

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("3. CREATE NEW PREFAB\n(Copy Julia + Replace Model)", GUILayout.Height(50)))
            {
                CreateNewPrefabWithReplacedModel();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This will:\n" +
                "1. Duplicate Julia.prefab (keeps ALL components & settings)\n" +
                "2. Replace 'Character' object's skeleton with Formal.fbx\n" +
                "3. Remap bone references using predefined mapping\n" +
                "4. Keep all scripts, colliders, and other components",
                MessageType.Info);
        }

        private void ConfigureFbxImport()
        {
            var importer = AssetImporter.GetAtPath(SOURCE_FBX_PATH) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"Cannot find ModelImporter for {SOURCE_FBX_PATH}");
                return;
            }

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
            importer.importAnimation = true;
            // Import materials from FBX (external materials mode)
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.External;
            importer.globalScale = 1f;
            importer.useFileScale = true;

            // Get all animation clip names from FBX
            var clipAnimations = importer.defaultClipAnimations;
            if (clipAnimations != null && clipAnimations.Length > 0)
            {
                Debug.Log($"Found {clipAnimations.Length} animation clips in FBX:");
                var newClips = new ModelImporterClipAnimation[clipAnimations.Length];
                for (int i = 0; i < clipAnimations.Length; i++)
                {
                    newClips[i] = clipAnimations[i];
                    // Configure loop for animations
                    string clipName = clipAnimations[i].name.ToLower();
                    if (clipName.Contains("idle") || clipName.Contains("run") || clipName.Contains("walk") || clipName.Contains("sword_slash"))
                    {
                        newClips[i].loopTime = true;
                        newClips[i].loop = true;
                    }
                    Debug.Log($"  - {clipAnimations[i].name} (frames {clipAnimations[i].firstFrame}-{clipAnimations[i].lastFrame})");
                }
                importer.clipAnimations = newClips;
            }

            importer.SaveAndReimport();
            LoadAssets();

            Debug.Log("FBX import configured with animations!");
        }

        private void ShowBoneMapping()
        {
            Debug.Log("=== BONE MAPPING ===");
            foreach (var kvp in BoneMapping)
            {
                Debug.Log($"  {kvp.Key} -> {kvp.Value}");
            }

            // Show actual bones in both
            Debug.Log("\n=== Formal.fbx actual bones ===");
            var formalSmr = sourceFbx?.GetComponentInChildren<SkinnedMeshRenderer>();
            if (formalSmr != null)
            {
                foreach (var bone in formalSmr.bones.Where(b => b != null).Take(30))
                {
                    bool mapped = BoneMapping.ContainsKey(bone.name);
                    Debug.Log($"  {bone.name} {(mapped ? "✓" : "?")}");
                }
            }
        }

        private void CreateAnimatorControllerForFormal()
        {
            const string CONTROLLER_PATH = "Assets/Animations/Characters/Controller_Formal.controller";

            // Load all animation clips from Formal.fbx
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(SOURCE_FBX_PATH);
            var clips = allAssets.OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToList();

            if (clips.Count == 0)
            {
                Debug.LogError("No animation clips found in Formal.fbx! Click '1. Configure FBX Import' first.");
                return;
            }

            Debug.Log($"Found {clips.Count} animation clips:");
            foreach (var clip in clips)
            {
                Debug.Log($"  - {clip.name}");
            }

            // Delete existing controller if exists
            if (File.Exists(CONTROLLER_PATH))
            {
                AssetDatabase.DeleteAsset(CONTROLLER_PATH);
            }

            // Create animator controller
            var controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);

            // Get the root state machine
            var rootStateMachine = controller.layers[0].stateMachine;

            // Find key clips from Formal.fbx
            AnimationClip idleClip = clips.FirstOrDefault(c => c.name.Contains("Idle") && !c.name.Contains("Gun") && !c.name.Contains("Sword") && !c.name.Contains("Neutral"));
            AnimationClip runClip = clips.FirstOrDefault(c => c.name.EndsWith("|Run"));
            AnimationClip walkClip = clips.FirstOrDefault(c => c.name.Contains("Walk"));
            AnimationClip interactClip = clips.FirstOrDefault(c => c.name.Contains("Interact"));

            // Add parameters matching Controller_MainCharacter
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsCustomStateActive", AnimatorControllerParameterType.Bool);
            controller.AddParameter("UpdateCustomState", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("CustomStateType", AnimatorControllerParameterType.Int);
            controller.AddParameter("ActionType", AnimatorControllerParameterType.Int);
            controller.AddParameter("IsDoingAction", AnimatorControllerParameterType.Bool);

            // Create states
            AnimatorState idleState = null;
            AnimatorState runState = null;

            if (idleClip != null)
            {
                idleState = rootStateMachine.AddState("Idle", new Vector3(300, 0, 0));
                idleState.motion = idleClip;
                rootStateMachine.defaultState = idleState;
                Debug.Log($"Added Idle state with clip: {idleClip.name}");
            }

            if (runClip != null)
            {
                runState = rootStateMachine.AddState("Run", new Vector3(300, 100, 0));
                runState.motion = runClip;
                Debug.Log($"Added Run state with clip: {runClip.name}");
            }
            else if (walkClip != null)
            {
                runState = rootStateMachine.AddState("Run", new Vector3(300, 100, 0));
                runState.motion = walkClip;
                Debug.Log($"Added Run state with Walk clip: {walkClip.name}");
            }

            // Add transitions using IsMoving (like Controller_MainCharacter)
            if (idleState != null && runState != null)
            {
                var toRun = idleState.AddTransition(runState);
                toRun.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
                toRun.hasExitTime = false;
                toRun.duration = 0.1f;

                var toIdle = runState.AddTransition(idleState);
                toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
                toIdle.hasExitTime = false;
                toIdle.duration = 0.1f;
            }

            // Add interact/action state if available
            if (interactClip != null && idleState != null)
            {
                var actionState = rootStateMachine.AddState("Action", new Vector3(500, 50, 0));
                actionState.motion = interactClip;

                var toAction = idleState.AddTransition(actionState);
                toAction.AddCondition(AnimatorConditionMode.If, 0, "IsDoingAction");
                toAction.hasExitTime = false;
                toAction.duration = 0.1f;

                var fromAction = actionState.AddTransition(idleState);
                fromAction.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDoingAction");
                fromAction.hasExitTime = true;
                fromAction.exitTime = 0.9f;
                fromAction.duration = 0.1f;
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            Debug.Log($"AnimatorController created at: {CONTROLLER_PATH}");
            Debug.Log("Parameters: IsMoving, IsCustomStateActive, UpdateCustomState, CustomStateType, ActionType, IsDoingAction");
            EditorUtility.DisplayDialog("Success",
                $"AnimatorController created at:\n{CONTROLLER_PATH}\n\n" +
                $"Clips: {clips.Count}\n" +
                $"Parameters match Controller_MainCharacter",
                "OK");

            Selection.activeObject = controller;
            EditorGUIUtility.PingObject(controller);
        }

        private void CreateNewPrefabWithReplacedModel()
        {
            // Step 1: Load original prefab contents
            var originalContents = PrefabUtility.LoadPrefabContents(TARGET_PREFAB_PATH);

            try
            {
                // Step 2: Find the "Character" object (has Animator)
                var animator = originalContents.GetComponentInChildren<Animator>();
                if (animator == null)
                {
                    Debug.LogError("No Animator found in original prefab!");
                    return;
                }

                Transform characterObj = animator.transform;
                Debug.Log($"Found Character object: {characterObj.name}");

                // Save animator controller
                var animController = animator.runtimeAnimatorController;

                // Step 3: Find DeformationSystem and Geometry
                Transform deformationSystem = characterObj.Find("DeformationSystem");
                Transform geometry = characterObj.Find("Geometry");

                if (deformationSystem == null)
                {
                    Debug.LogError("DeformationSystem not found!");
                    return;
                }

                // Step 4: Build mapping of old bone names to transforms
                var oldBoneDict = new Dictionary<string, Transform>();
                foreach (Transform t in characterObj.GetComponentsInChildren<Transform>(true))
                {
                    if (!oldBoneDict.ContainsKey(t.name))
                        oldBoneDict[t.name] = t;
                }
                Debug.Log($"Collected {oldBoneDict.Count} transforms from old skeleton");

                // Step 5: Collect all components that reference bones (before modification)
                // Store bone NAMES, not references (which will become invalid after deletion)
                var componentsWithBoneRefs = CollectComponentsWithTransformRefs(originalContents, oldBoneDict);
                Debug.Log($"Found {componentsWithBoneRefs.Count} components with bone references");

                // Step 6: Find gameplay objects (objects with MonoBehaviour) inside DeformationSystem
                // First pass: identify all gameplay objects without moving them
                var savedGameplayObjects = new List<SavedGameplayObject>();
                IdentifyGameplayObjects(deformationSystem, savedGameplayObjects);
                Debug.Log($"Identified {savedGameplayObjects.Count} gameplay objects from skeleton");

                // Second pass: move all identified objects to temp parent
                foreach (var saved in savedGameplayObjects)
                {
                    if (saved.gameObject != null)
                    {
                        Debug.Log($"  Moving {saved.gameObject.name} from {saved.parentBoneName} to temp parent");
                        saved.gameObject.transform.SetParent(characterObj, false);
                    }
                }

                // Step 7: Delete DeformationSystem and Geometry only
                if (deformationSystem != null)
                    DestroyImmediate(deformationSystem.gameObject);
                if (geometry != null)
                    DestroyImmediate(geometry.gameObject);
                Debug.Log("Deleted DeformationSystem and Geometry");

                // Step 8: Instantiate Formal.fbx content
                var formalInstance = Object.Instantiate(sourceFbx);

                // Move all children under Character object
                var formalChildren = new List<Transform>();
                foreach (Transform child in formalInstance.transform)
                {
                    formalChildren.Add(child);
                }
                foreach (var child in formalChildren)
                {
                    child.SetParent(characterObj, false);
                }
                DestroyImmediate(formalInstance);
                Debug.Log($"Added {formalChildren.Count} new children from Formal.fbx");

                // Step 9: Build new bone dictionary
                var newBoneDict = new Dictionary<string, Transform>();
                foreach (Transform t in characterObj.GetComponentsInChildren<Transform>(true))
                {
                    if (!newBoneDict.ContainsKey(t.name))
                        newBoneDict[t.name] = t;
                }
                Debug.Log($"New skeleton has {newBoneDict.Count} transforms");

                // Step 10: Create combined mapping (old bone name -> new transform)
                var combinedMapping = new Dictionary<string, Transform>();
                foreach (var oldName in oldBoneDict.Keys)
                {
                    // Try direct mapping (old bone name -> new bone name)
                    if (BoneMapping.TryGetValue(oldName, out string newBoneName))
                    {
                        if (newBoneDict.TryGetValue(newBoneName, out var newBone))
                        {
                            combinedMapping[oldName] = newBone;
                            continue;
                        }
                    }

                    // Try same name match
                    if (newBoneDict.TryGetValue(oldName, out var sameName))
                    {
                        combinedMapping[oldName] = sameName;
                    }
                }
                Debug.Log($"Created mapping for {combinedMapping.Count} bones");

                // Step 11: Re-parent saved gameplay objects to equivalent bones
                int reparentedCount = 0;
                foreach (var saved in savedGameplayObjects)
                {
                    if (saved.gameObject == null) continue;

                    Transform newParent = null;
                    if (BoneMapping.TryGetValue(saved.parentBoneName, out string newBoneName))
                    {
                        newBoneDict.TryGetValue(newBoneName, out newParent);
                    }
                    if (newParent == null)
                    {
                        newBoneDict.TryGetValue(saved.parentBoneName, out newParent);
                    }

                    if (newParent != null)
                    {
                        saved.gameObject.transform.SetParent(newParent, false);
                        saved.gameObject.transform.localPosition = saved.localPosition;
                        saved.gameObject.transform.localRotation = saved.localRotation;
                        saved.gameObject.transform.localScale = saved.localScale;
                        Debug.Log($"  Re-parented {saved.gameObject.name} from {saved.parentBoneName} to {newParent.name}");
                        reparentedCount++;
                    }
                    else
                    {
                        // Parent to Character as fallback
                        saved.gameObject.transform.SetParent(characterObj, false);
                        Debug.LogWarning($"  Could not find new parent for {saved.gameObject.name} (was: {saved.parentBoneName})");
                    }
                }
                Debug.Log($"Re-parented {reparentedCount} gameplay objects");

                // Step 12: Update bone references in components
                int updatedRefs = 0;
                int failedRefs = 0;
                foreach (var (component, fieldName, oldBoneName) in componentsWithBoneRefs)
                {
                    if (component == null)
                    {
                        Debug.LogWarning($"  Component is null for field {fieldName}, oldBoneName: {oldBoneName}");
                        failedRefs++;
                        continue;
                    }

                    Transform newTransform = null;
                    if (oldBoneName != null && combinedMapping.TryGetValue(oldBoneName, out newTransform))
                    {
                        SetFieldValue(component, fieldName, newTransform);
                        Debug.Log($"  Remapped {component.GetType().Name}.{fieldName}: {oldBoneName} -> {newTransform.name}");
                        updatedRefs++;
                    }
                    else if (oldBoneName != null)
                    {
                        // Try to find by name in the current hierarchy (might be a re-parented gameplay object)
                        var found = characterObj.GetComponentsInChildren<Transform>(true)
                            .FirstOrDefault(t => t.name == oldBoneName);
                        if (found != null)
                        {
                            SetFieldValue(component, fieldName, found);
                            Debug.Log($"  Found {component.GetType().Name}.{fieldName}: {oldBoneName} by name search");
                            updatedRefs++;
                        }
                        else
                        {
                            // Also search in the entire prefab (object might be outside Character)
                            found = originalContents.GetComponentsInChildren<Transform>(true)
                                .FirstOrDefault(t => t.name == oldBoneName);
                            if (found != null)
                            {
                                SetFieldValue(component, fieldName, found);
                                Debug.Log($"  Found {component.GetType().Name}.{fieldName}: {oldBoneName} in prefab root");
                                updatedRefs++;
                            }
                            else
                            {
                                Debug.LogWarning($"  Could not remap {component.GetType().Name}.{fieldName}: {oldBoneName} -> ??? (object not found)");
                                failedRefs++;
                            }
                        }
                    }
                }
                Debug.Log($"Updated {updatedRefs} bone references, failed: {failedRefs}");

                // Step 13: Set animator controller (use new Formal controller if exists, otherwise keep old)
                var newAnimator = characterObj.GetComponent<Animator>();
                if (newAnimator == null)
                    newAnimator = characterObj.gameObject.AddComponent<Animator>();

                // Try to use the new Formal controller
                const string FORMAL_CONTROLLER_PATH = "Assets/Animations/Characters/Controller_Formal.controller";
                var formalController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(FORMAL_CONTROLLER_PATH);
                if (formalController != null)
                {
                    newAnimator.runtimeAnimatorController = formalController;
                    Debug.Log($"Using new Formal AnimatorController: {FORMAL_CONTROLLER_PATH}");
                }
                else
                {
                    newAnimator.runtimeAnimatorController = animController;
                    Debug.LogWarning($"Formal AnimatorController not found at {FORMAL_CONTROLLER_PATH}, using original controller. Click '2.5 Create Animator Controller' first.");
                }

                // Step 14: Keep original materials from Formal.fbx (don't override)

                // Step 15: Save as new prefab
                PrefabUtility.SaveAsPrefabAsset(originalContents, NEW_PREFAB_PATH);

                Debug.Log($"SUCCESS! New prefab saved to: {NEW_PREFAB_PATH}");
                EditorUtility.DisplayDialog("Success",
                    $"New prefab created: {NEW_PREFAB_PATH}\n\n" +
                    $"Gameplay objects preserved: {savedGameplayObjects.Count}\n" +
                    $"Re-parented to new skeleton: {reparentedCount}\n" +
                    $"Bone refs updated: {updatedRefs}\n\n" +
                    "Please check the prefab and manually fix any remaining references.",
                    "OK");

                // Select new prefab
                var newPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NEW_PREFAB_PATH);
                Selection.activeObject = newPrefab;
                EditorGUIUtility.PingObject(newPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(originalContents);
            }
        }

        private void IdentifyGameplayObjects(Transform root, List<SavedGameplayObject> savedObjects)
        {
            // Recursively find all objects that have gameplay components (MonoBehaviour, nested prefabs, etc.)
            // This just identifies them without moving - movement happens in a separate pass
            var childrenToProcess = new List<Transform>();
            foreach (Transform child in root)
            {
                childrenToProcess.Add(child);
            }

            foreach (var child in childrenToProcess)
            {
                // Check if this object is a gameplay object (not just a skeleton bone)
                bool isGameplayObject = IsGameplayObject(child.gameObject);

                if (isGameplayObject)
                {
                    // This is a gameplay object - save its info
                    var saved = new SavedGameplayObject
                    {
                        gameObject = child.gameObject,
                        parentBoneName = child.parent.name,
                        localPosition = child.localPosition,
                        localRotation = child.localRotation,
                        localScale = child.localScale
                    };
                    savedObjects.Add(saved);
                    Debug.Log($"  Identified gameplay object: {child.name} (parent: {saved.parentBoneName})");
                    // Don't recurse into gameplay objects - they'll be moved with all children
                }
                else
                {
                    // Check if it has children that are gameplay objects - recurse to find them
                    IdentifyGameplayObjects(child, savedObjects);
                }
            }
        }

        private bool IsGameplayObject(GameObject obj)
        {
            // Check if object has MonoBehaviour components
            if (obj.GetComponents<MonoBehaviour>().Length > 0)
                return true;

            // Check if object is a nested prefab instance root
            // During prefab editing, nested prefabs are identified by having a different prefab source
            var prefabAssetType = PrefabUtility.GetPrefabAssetType(obj);
            if (prefabAssetType == PrefabAssetType.Regular || prefabAssetType == PrefabAssetType.Variant)
            {
                // Check if this object is a nested prefab root (not the main prefab we're editing)
                if (PrefabUtility.IsAnyPrefabInstanceRoot(obj))
                {
                    return true;
                }
            }

            // Check if object corresponds to a different prefab source (nested prefab)
            var correspondingObject = PrefabUtility.GetCorrespondingObjectFromSource(obj);
            if (correspondingObject != null)
            {
                var sourceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(obj);
                if (sourceRoot != null && sourceRoot != obj.transform.root.gameObject)
                {
                    // This is part of a nested prefab
                    return true;
                }
            }

            // Check if object has more than just Transform component (e.g., MeshRenderer, Collider, etc.)
            var components = obj.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp == null) continue;
                var type = comp.GetType();
                // Skip Transform - that's always present
                if (type == typeof(Transform)) continue;
                // Skip Animator - bones can have this
                if (type == typeof(Animator)) continue;
                // If there's any other component, it's a gameplay object
                return true;
            }

            // Check if the object name doesn't look like a skeleton bone
            // Skeleton bones typically have patterns like Name_M, Name_L, Name_R
            string name = obj.name;
            bool looksLikeBone = name.EndsWith("_M") || name.EndsWith("_L") || name.EndsWith("_R") ||
                                 name.StartsWith("Root") || name.StartsWith("Hip") || name.StartsWith("Spine") ||
                                 name.StartsWith("Chest") || name.StartsWith("Neck") || name.StartsWith("Head") ||
                                 name.StartsWith("Shoulder") || name.StartsWith("Elbow") || name.StartsWith("Wrist") ||
                                 name.StartsWith("Knee") || name.StartsWith("Ankle") || name.StartsWith("Thumb") ||
                                 name.StartsWith("Index") || name.StartsWith("Middle") || name.StartsWith("Ring") ||
                                 name.StartsWith("Pinky") || name.StartsWith("Scapula") || name.StartsWith("Finger");

            // If it doesn't look like a bone and has children with components, it might be a container
            if (!looksLikeBone)
            {
                return true;
            }

            return false;
        }

        private List<(Component, string, string)> CollectComponentsWithTransformRefs(GameObject root, Dictionary<string, Transform> boneDict)
        {
            // Returns (component, fieldName, oldBoneName) - store NAME not reference
            // For arrays/lists, fieldName includes index: "fieldName[0]", "fieldName[1]", etc.
            var result = new List<(Component, string, string)>();

            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null) continue;

                var type = component.GetType();
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                foreach (var field in fields)
                {
                    // Handle single Transform
                    if (field.FieldType == typeof(Transform))
                    {
                        var value = field.GetValue(component) as Transform;
                        if (value != null && boneDict.ContainsKey(value.name))
                        {
                            result.Add((component, field.Name, value.name));
                        }
                    }
                    // Handle Transform[]
                    else if (field.FieldType == typeof(Transform[]))
                    {
                        var array = field.GetValue(component) as Transform[];
                        if (array != null)
                        {
                            for (int i = 0; i < array.Length; i++)
                            {
                                if (array[i] != null && boneDict.ContainsKey(array[i].name))
                                {
                                    result.Add((component, $"{field.Name}[{i}]", array[i].name));
                                }
                            }
                        }
                    }
                    // Handle List<Transform>
                    else if (field.FieldType == typeof(List<Transform>))
                    {
                        var list = field.GetValue(component) as List<Transform>;
                        if (list != null)
                        {
                            for (int i = 0; i < list.Count; i++)
                            {
                                if (list[i] != null && boneDict.ContainsKey(list[i].name))
                                {
                                    result.Add((component, $"{field.Name}[{i}]", list[i].name));
                                }
                            }
                        }
                    }
                    // Handle single GameObject
                    else if (field.FieldType == typeof(GameObject))
                    {
                        var value = field.GetValue(component) as GameObject;
                        if (value != null && boneDict.ContainsKey(value.name))
                        {
                            result.Add((component, $"GO:{field.Name}", value.name));
                        }
                    }
                    // Handle GameObject[]
                    else if (field.FieldType == typeof(GameObject[]))
                    {
                        var array = field.GetValue(component) as GameObject[];
                        if (array != null)
                        {
                            for (int i = 0; i < array.Length; i++)
                            {
                                if (array[i] != null && boneDict.ContainsKey(array[i].name))
                                {
                                    result.Add((component, $"GO:{field.Name}[{i}]", array[i].name));
                                }
                            }
                        }
                    }
                    // Handle List<GameObject>
                    else if (field.FieldType == typeof(List<GameObject>))
                    {
                        var list = field.GetValue(component) as List<GameObject>;
                        if (list != null)
                        {
                            for (int i = 0; i < list.Count; i++)
                            {
                                if (list[i] != null && boneDict.ContainsKey(list[i].name))
                                {
                                    result.Add((component, $"GO:{field.Name}[{i}]", list[i].name));
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        private void SetFieldValue(Component component, string fieldName, Transform newValue)
        {
            var type = component.GetType();
            bool isGameObject = fieldName.StartsWith("GO:");
            if (isGameObject)
            {
                fieldName = fieldName.Substring(3); // Remove "GO:" prefix
            }

            // Check if it's an array/list access like "fieldName[0]"
            int bracketIndex = fieldName.IndexOf('[');
            if (bracketIndex > 0)
            {
                string actualFieldName = fieldName.Substring(0, bracketIndex);
                int index = int.Parse(fieldName.Substring(bracketIndex + 1, fieldName.Length - bracketIndex - 2));

                var field = type.GetField(actualFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    if (isGameObject)
                    {
                        if (field.FieldType == typeof(GameObject[]))
                        {
                            var array = field.GetValue(component) as GameObject[];
                            if (array != null && index < array.Length)
                            {
                                array[index] = newValue.gameObject;
                            }
                        }
                        else if (field.FieldType == typeof(List<GameObject>))
                        {
                            var list = field.GetValue(component) as List<GameObject>;
                            if (list != null && index < list.Count)
                            {
                                list[index] = newValue.gameObject;
                            }
                        }
                    }
                    else
                    {
                        if (field.FieldType == typeof(Transform[]))
                        {
                            var array = field.GetValue(component) as Transform[];
                            if (array != null && index < array.Length)
                            {
                                array[index] = newValue;
                            }
                        }
                        else if (field.FieldType == typeof(List<Transform>))
                        {
                            var list = field.GetValue(component) as List<Transform>;
                            if (list != null && index < list.Count)
                            {
                                list[index] = newValue;
                            }
                        }
                    }
                }
            }
            else
            {
                // Simple field access
                var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    if (isGameObject)
                    {
                        field.SetValue(component, newValue.gameObject);
                    }
                    else
                    {
                        field.SetValue(component, newValue);
                    }
                }
            }
        }
    }
}
