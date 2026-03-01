using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Editor
{
    /// <summary>
    /// Generates Animator Controller for QueueCharacter_New.
    /// Uses animations from Formal.fbx (CharacterArmature|Idle, CharacterArmature|Run).
    /// </summary>
    public class NewQueueCharacterAnimatorGenerator : EditorWindow
    {
        private const string SOURCE_FBX_PATH = "Assets/Art/Models/Characters/Formal.fbx";
        private const string CONTROLLER_PATH = "Assets/Animations/Characters/Controller_NewQueueCharacter.controller";

        private Dictionary<string, AnimationClip> clips;

        [MenuItem("Tools/QueueCharacter_New/1. Generate Animator Controller")]
        public static void ShowWindow()
        {
            var window = GetWindow<NewQueueCharacterAnimatorGenerator>("New QueueCharacter Animator");
            window.minSize = new Vector2(450, 350);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("QueueCharacter_New Animator Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Creates Controller_NewQueueCharacter.controller with:\n\n" +
                "Parameters:\n" +
                "  - IsMoving (bool)\n" +
                "  - HandHold (bool)\n\n" +
                "States:\n" +
                "  - Idle (default)\n" +
                "  - Run (when IsMoving=true)\n" +
                "  - HandHold (when HandHold=true, not moving)\n" +
                "  - RunHold (when HandHold=true AND IsMoving=true)\n\n" +
                "Animations from: Formal.fbx",
                MessageType.Info);

            EditorGUILayout.Space(10);

            if (GUILayout.Button("List Available Animations", GUILayout.Height(30)))
            {
                ListAnimations();
            }

            EditorGUILayout.Space(5);

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Generate Controller", GUILayout.Height(40)))
            {
                GenerateController();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField($"Source: {SOURCE_FBX_PATH}");
            EditorGUILayout.LabelField($"Output: {CONTROLLER_PATH}");
        }

        private void ListAnimations()
        {
            LoadClips();
            Debug.Log("=== Animations in Formal.fbx ===");
            foreach (var kvp in clips.OrderBy(k => k.Key))
            {
                Debug.Log($"  {kvp.Key} ({kvp.Value.length:F2}s, loop: {kvp.Value.isLooping})");
            }
        }

        private void LoadClips()
        {
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(SOURCE_FBX_PATH);
            clips = allAssets
                .OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__"))
                .ToDictionary(c => c.name, c => c);

            Debug.Log($"Loaded {clips.Count} animation clips from {SOURCE_FBX_PATH}");
        }

        private void GenerateController()
        {
            LoadClips();

            if (clips.Count == 0)
            {
                EditorUtility.DisplayDialog("Error",
                    $"No animation clips found in {SOURCE_FBX_PATH}!\n\nMake sure the FBX exists and has animations.",
                    "OK");
                return;
            }

            // Delete existing controller
            if (File.Exists(CONTROLLER_PATH))
            {
                AssetDatabase.DeleteAsset(CONTROLLER_PATH);
            }

            // Create controller
            var controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);

            // Add parameters (matching QueueCharacter.cs)
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("HandHold", AnimatorControllerParameterType.Bool);

            var rootStateMachine = controller.layers[0].stateMachine;

            // Get animation clips
            var idleClip = GetClip("CharacterArmature|Idle");
            var runClip = GetClip("CharacterArmature|Run");
            var walkClip = GetClip("CharacterArmature|Walk"); // Fallback for HandHold

            if (idleClip == null || runClip == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "Required animations not found:\n" +
                    $"  - CharacterArmature|Idle: {(idleClip != null ? "OK" : "MISSING")}\n" +
                    $"  - CharacterArmature|Run: {(runClip != null ? "OK" : "MISSING")}",
                    "OK");
                return;
            }

            // Use Idle for HandHold (same as Run but standing)
            var handHoldClip = idleClip;
            var runHoldClip = runClip;

            // Create states
            var idle = CreateState(rootStateMachine, "Idle", idleClip, new Vector3(320, 110, 0));
            var run = CreateState(rootStateMachine, "Run", runClip, new Vector3(320, 180, 0));
            var handHold = CreateState(rootStateMachine, "HandHold", handHoldClip, new Vector3(560, 110, 0));
            var runHold = CreateState(rootStateMachine, "RunHold", runHoldClip, new Vector3(560, 180, 0));

            rootStateMachine.defaultState = idle;

            // === TRANSITIONS ===

            // Idle -> Run (IsMoving = true, HandHold = false)
            var idleToRun = idle.AddTransition(run);
            idleToRun.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
            idleToRun.hasExitTime = false;
            idleToRun.duration = 0.1f;

            // Idle -> HandHold (HandHold = true, IsMoving = false)
            var idleToHandHold = idle.AddTransition(handHold);
            idleToHandHold.AddCondition(AnimatorConditionMode.If, 0, "HandHold");
            idleToHandHold.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
            idleToHandHold.hasExitTime = false;
            idleToHandHold.duration = 0.1f;

            // Run -> Idle (IsMoving = false)
            var runToIdle = run.AddTransition(idle);
            runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
            runToIdle.hasExitTime = false;
            runToIdle.duration = 0.1f;

            // Run -> RunHold (HandHold = true)
            var runToRunHold = run.AddTransition(runHold);
            runToRunHold.AddCondition(AnimatorConditionMode.If, 0, "HandHold");
            runToRunHold.hasExitTime = false;
            runToRunHold.duration = 0.1f;

            // HandHold -> Idle (HandHold = false, IsMoving = false)
            var handHoldToIdle = handHold.AddTransition(idle);
            handHoldToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "HandHold");
            handHoldToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
            handHoldToIdle.hasExitTime = false;
            handHoldToIdle.duration = 0.1f;

            // HandHold -> RunHold (IsMoving = true)
            var handHoldToRunHold = handHold.AddTransition(runHold);
            handHoldToRunHold.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
            handHoldToRunHold.hasExitTime = false;
            handHoldToRunHold.duration = 0.1f;

            // RunHold -> HandHold (IsMoving = false)
            var runHoldToHandHold = runHold.AddTransition(handHold);
            runHoldToHandHold.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
            runHoldToHandHold.hasExitTime = false;
            runHoldToHandHold.duration = 0.15f;

            // RunHold -> Run (HandHold = false)
            var runHoldToRun = runHold.AddTransition(run);
            runHoldToRun.AddCondition(AnimatorConditionMode.IfNot, 0, "HandHold");
            runHoldToRun.hasExitTime = false;
            runHoldToRun.duration = 0.1f;

            // Save
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"=== Controller created at: {CONTROLLER_PATH} ===");
            Debug.Log("Parameters: IsMoving, HandHold");
            Debug.Log("States: Idle, Run, HandHold, RunHold");

            EditorUtility.DisplayDialog("Success",
                $"Controller created at:\n{CONTROLLER_PATH}\n\n" +
                "Parameters:\n" +
                "  - IsMoving (bool)\n" +
                "  - HandHold (bool)\n\n" +
                "States:\n" +
                "  - Idle (default)\n" +
                "  - Run\n" +
                "  - HandHold\n" +
                "  - RunHold",
                "OK");

            Selection.activeObject = controller;
            EditorGUIUtility.PingObject(controller);
        }

        private AnimationClip GetClip(string name)
        {
            if (clips.TryGetValue(name, out var clip))
                return clip;

            Debug.LogWarning($"Animation clip not found: {name}");
            return null;
        }

        private AnimatorState CreateState(AnimatorStateMachine sm, string name, AnimationClip clip, Vector3 position, float speed = 1f)
        {
            var state = sm.AddState(name, position);
            state.motion = clip;
            state.speed = speed;
            return state;
        }
    }
}
