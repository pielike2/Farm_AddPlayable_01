using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Editor
{
    /// <summary>
    /// Converts Julia prefab to use Formal model while preserving ALL component references.
    /// Key insight: We duplicate Julia first, then surgically replace only the skeleton/meshes.
    /// </summary>
    public class JuliaToFormalConverter : EditorWindow
    {
        private const string JULIA_PATH = "Assets/Prefabs/Characters/Julia.prefab";
        private const string FORMAL_FBX_PATH = "Assets/Art/Models/Characters/Formal.fbx";
        private const string OUTPUT_PATH = "Assets/Prefabs/Characters/Julia_Formal.prefab";
        private const string CONTROLLER_PATH = "Assets/Animations/Characters/Controller_Formal.controller";

        // Old bone name -> New bone name
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

        [MenuItem("Tools/Convert Julia to Formal (Preserve References)")]
        public static void ShowWindow()
        {
            var window = GetWindow<JuliaToFormalConverter>("Julia → Formal");
            window.minSize = new Vector2(550, 400);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Julia → Formal Converter", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Этот инструмент:\n\n" +
                "1. Дублирует Julia.prefab (ВСЕ ссылки сохраняются)\n" +
                "2. Находит gameplay объекты внутри скелета\n" +
                "3. Удаляет ТОЛЬКО кости скелета и меши\n" +
                "4. Добавляет скелет и меши из Formal.fbx\n" +
                "5. Перемещает gameplay объекты на новые кости\n\n" +
                "Все компоненты и их ссылки остаются нетронутыми!",
                MessageType.Info);

            EditorGUILayout.Space(20);

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("КОНВЕРТИРОВАТЬ", GUILayout.Height(50)))
            {
                Convert();
            }
            GUI.backgroundColor = Color.white;
        }

        private void Convert()
        {
            var formalFbx = AssetDatabase.LoadAssetAtPath<GameObject>(FORMAL_FBX_PATH);
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CONTROLLER_PATH);

            if (formalFbx == null)
            {
                EditorUtility.DisplayDialog("Ошибка", $"Formal.fbx не найден: {FORMAL_FBX_PATH}", "OK");
                return;
            }

            // Step 1: Duplicate Julia prefab (all references preserved)
            if (!AssetDatabase.CopyAsset(JULIA_PATH, OUTPUT_PATH))
            {
                EditorUtility.DisplayDialog("Ошибка", "Не удалось скопировать Julia.prefab", "OK");
                return;
            }
            AssetDatabase.Refresh();

            // Step 2: Load the duplicated prefab for editing
            var prefabContents = PrefabUtility.LoadPrefabContents(OUTPUT_PATH);

            try
            {
                prefabContents.name = "Julia_Formal";

                // Find Character object
                var character = prefabContents.transform.Find("Character");
                if (character == null)
                {
                    Debug.LogError("Character not found!");
                    return;
                }

                var deformationSystem = character.Find("DeformationSystem");
                var geometry = character.Find("Geometry");

                if (deformationSystem == null)
                {
                    Debug.LogError("DeformationSystem not found!");
                    return;
                }

                // Step 3: Collect gameplay objects (non-bone objects inside skeleton)
                var gameplayObjects = new List<(GameObject obj, string parentBone, Vector3 localPos, Quaternion localRot, Vector3 localScale)>();
                CollectGameplayObjectsRecursive(deformationSystem, gameplayObjects);

                Debug.Log($"Found {gameplayObjects.Count} gameplay objects:");
                foreach (var (obj, parentBone, _, _, _) in gameplayObjects)
                {
                    Debug.Log($"  - {obj.name} (parent: {parentBone})");
                }

                // Step 4: Move gameplay objects temporarily to Character root
                foreach (var (obj, _, _, _, _) in gameplayObjects)
                {
                    obj.transform.SetParent(character, true);
                }

                // Step 5: Delete old skeleton and geometry
                Object.DestroyImmediate(deformationSystem.gameObject);
                if (geometry != null)
                    Object.DestroyImmediate(geometry.gameObject);

                // Step 6: Instantiate Formal skeleton and meshes
                // Use Object.Instantiate because we're inside LoadPrefabContents context
                var formalInstance = Object.Instantiate(formalFbx);

                // Move children to Character
                var children = new List<Transform>();
                foreach (Transform child in formalInstance.transform)
                    children.Add(child);

                foreach (var child in children)
                    child.SetParent(character, false);

                Object.DestroyImmediate(formalInstance);

                // Step 7: Build new bone dictionary
                var newBones = new Dictionary<string, Transform>();
                foreach (var t in character.GetComponentsInChildren<Transform>(true))
                {
                    if (!newBones.ContainsKey(t.name))
                        newBones[t.name] = t;
                }

                // Step 8: Re-parent gameplay objects to new skeleton
                int reparented = 0;
                foreach (var (obj, parentBone, localPos, localRot, localScale) in gameplayObjects)
                {
                    Transform targetBone = null;

                    // Try mapped name
                    if (BoneMapping.TryGetValue(parentBone, out var newBoneName))
                        newBones.TryGetValue(newBoneName, out targetBone);

                    // Try same name
                    if (targetBone == null)
                        newBones.TryGetValue(parentBone, out targetBone);

                    if (targetBone != null)
                    {
                        obj.transform.SetParent(targetBone, false);
                        obj.transform.localPosition = localPos;
                        obj.transform.localRotation = localRot;
                        obj.transform.localScale = localScale;
                        Debug.Log($"  {obj.name}: {parentBone} → {targetBone.name}");
                        reparented++;
                    }
                    else
                    {
                        Debug.LogWarning($"  {obj.name}: no target for {parentBone}, keeping at Character");
                    }
                }

                // Step 9: Update Animator
                var animator = character.GetComponent<Animator>();
                if (animator != null && controller != null)
                {
                    animator.runtimeAnimatorController = controller;
                    Debug.Log("Animator controller updated");
                }

                // Save
                PrefabUtility.SaveAsPrefabAsset(prefabContents, OUTPUT_PATH);

                Debug.Log($"\n=== ГОТОВО ===");
                Debug.Log($"Gameplay объектов: {gameplayObjects.Count}");
                Debug.Log($"Перемещено на новый скелет: {reparented}");

                EditorUtility.DisplayDialog("Готово!",
                    $"Создан: {OUTPUT_PATH}\n\n" +
                    $"Gameplay объектов: {gameplayObjects.Count}\n" +
                    $"Перемещено: {reparented}",
                    "OK");

                Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(OUTPUT_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabContents);
            }
        }

        private void CollectGameplayObjectsRecursive(Transform parent, List<(GameObject, string, Vector3, Quaternion, Vector3)> results)
        {
            var children = new List<Transform>();
            foreach (Transform child in parent)
                children.Add(child);

            foreach (var child in children)
            {
                if (IsGameplayObject(child.gameObject))
                {
                    results.Add((
                        child.gameObject,
                        child.parent.name,
                        child.localPosition,
                        child.localRotation,
                        child.localScale
                    ));
                    // Don't recurse - gameplay object moves with all children
                }
                else
                {
                    // This is a skeleton bone - recurse into it
                    CollectGameplayObjectsRecursive(child, results);
                }
            }
        }

        private bool IsGameplayObject(GameObject obj)
        {
            // Has MonoBehaviour = gameplay object
            if (obj.GetComponents<MonoBehaviour>().Length > 0)
                return true;

            // Has Renderer but parent is a bone = mesh attached to skeleton (like tools)
            if (obj.GetComponent<Renderer>() != null)
                return true;

            // Has Collider = gameplay object
            if (obj.GetComponent<Collider>() != null)
                return true;

            // Name patterns that indicate gameplay objects (not bones)
            string name = obj.name;
            if (name.StartsWith("INVENTORY") || name.StartsWith("Cargo") ||
                name.StartsWith("Tool") || name.StartsWith("SK_") ||
                name.StartsWith("FX_") || name.StartsWith("Sensor") ||
                name.Contains("Vacuum") || name.Contains("Feeding") ||
                name.Contains("Sickle") || name.Contains("slot_"))
                return true;

            return false;
        }
    }
}
