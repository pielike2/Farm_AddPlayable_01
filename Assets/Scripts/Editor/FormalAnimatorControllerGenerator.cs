using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Editor
{
    /// <summary>
    /// Generates Animator Controller for Formal character matching Julia's Controller_MainCharacter structure.
    /// Uses animations from Formal.fbx.
    /// </summary>
    public class FormalAnimatorControllerGenerator : EditorWindow
    {
        private const string SOURCE_FBX_PATH = "Assets/Art/Models/Characters/Formal.fbx";
        private const string CONTROLLER_PATH = "Assets/Animations/Characters/Controller_Formal.controller";

        private Dictionary<string, AnimationClip> clips;

        [MenuItem("Tools/Generate Formal Animator Controller")]
        public static void ShowWindow()
        {
            var window = GetWindow<FormalAnimatorControllerGenerator>("Formal Animator Generator");
            window.minSize = new Vector2(400, 300);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Formal Animator Controller Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This will create Controller_Formal.controller with the same structure as Controller_MainCharacter:\n\n" +
                "- 6 Parameters: IsMoving, IsCustomStateActive, UpdateCustomState, CustomStateType, ActionType, IsDoingAction\n" +
                "- 4 State Machines: Default, Tool_10, Tool_11, Tool_13\n" +
                "- Each with: Idle, Run, Attack, RunAttack states\n" +
                "- All transitions matching Julia's controller",
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
                    "No animation clips found in Formal.fbx!\n\nMake sure to configure FBX import first.",
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

            // Add parameters (matching Controller_MainCharacter)
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsCustomStateActive", AnimatorControllerParameterType.Bool);
            controller.AddParameter("UpdateCustomState", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("CustomStateType", AnimatorControllerParameterType.Int);
            controller.AddParameter("ActionType", AnimatorControllerParameterType.Int);
            controller.AddParameter("IsDoingAction", AnimatorControllerParameterType.Bool);

            var rootStateMachine = controller.layers[0].stateMachine;

            // Get animation clips
            var idleClip = GetClip("CharacterArmature|Idle");
            var runClip = GetClip("CharacterArmature|Run");
            var interactClip = GetClip("CharacterArmature|Interact");
            var idleSwordClip = GetClip("CharacterArmature|Idle_Sword");
            var punchRightClip = GetClip("CharacterArmature|Punch_Right");
            var swordSlashClip = GetClip("CharacterArmature|Sword_Slash");
            var kickRightClip = GetClip("CharacterArmature|Kick_Right");
            var runShootClip = GetClip("CharacterArmature|Run_Shoot");

            // Create sub-state machines
            var defaultSM = rootStateMachine.AddStateMachine("Default", new Vector3(320, 160, 0));
            var tool10SM = rootStateMachine.AddStateMachine("Tool_10", new Vector3(600, 290, 0));
            var tool11SM = rootStateMachine.AddStateMachine("Tool_11", new Vector3(600, 400, 0));
            var tool13SM = rootStateMachine.AddStateMachine("Tool_13", new Vector3(600, 500, 0));

            // === DEFAULT STATE MACHINE ===
            var defaultIdle = CreateState(defaultSM, "Idle", idleClip, new Vector3(380, 20, 0));
            var defaultRun = CreateState(defaultSM, "Run", runClip, new Vector3(380, 110, 0));
            var defaultInteract = CreateState(defaultSM, "InteractAction", interactClip, new Vector3(130, 110, 0));
            defaultSM.defaultState = defaultIdle;

            // Default transitions
            // Idle -> Run (IsMoving = true)
            AddTransition(defaultIdle, defaultRun, "IsMoving", true, 0.1f);
            // Idle -> InteractAction (IsDoingAction = true AND ActionType == 20)
            var idleToInteract = defaultIdle.AddTransition(defaultInteract);
            idleToInteract.AddCondition(AnimatorConditionMode.If, 0, "IsDoingAction");
            idleToInteract.AddCondition(AnimatorConditionMode.Equals, 20, "ActionType");
            idleToInteract.hasExitTime = false;
            idleToInteract.duration = 0.25f;

            // Run -> Idle (IsMoving = false)
            AddTransition(defaultRun, defaultIdle, "IsMoving", false, 0.1f);

            // InteractAction -> Idle (IsMoving = true, for run transition)
            AddTransition(defaultInteract, defaultRun, "IsMoving", true, 0.1f);
            // InteractAction -> Idle (IsDoingAction = false)
            AddTransition(defaultInteract, defaultIdle, "IsDoingAction", false, 0.1f);

            // === TOOL_10 STATE MACHINE ===
            var tool10Idle = CreateState(tool10SM, "Idle", idleClip, new Vector3(140, 0, 0));
            var tool10Run = CreateState(tool10SM, "Run", runClip, new Vector3(140, 60, 0));
            var tool10Attack = CreateState(tool10SM, "Attack", swordSlashClip, new Vector3(420, 0, 0), 1.5f);
            var tool10RunAttack = CreateState(tool10SM, "RunAttack", runShootClip ?? runClip, new Vector3(420, 60, 0), 1.5f);
            tool10SM.defaultState = tool10Idle;

            SetupToolStateMachineTransitions(tool10Idle, tool10Run, tool10Attack, tool10RunAttack);

            // === TOOL_11 STATE MACHINE ===
            var tool11Idle = CreateState(tool11SM, "Idle", idleClip, new Vector3(140, 0, 0));
            var tool11Run = CreateState(tool11SM, "Run", runClip, new Vector3(140, 60, 0));
            var tool11Attack = CreateState(tool11SM, "Attack", swordSlashClip, new Vector3(420, 0, 0), 1.25f);
            var tool11RunAttack = CreateState(tool11SM, "RunAttack", runShootClip ?? runClip, new Vector3(420, 60, 0), 1f);
            tool11SM.defaultState = tool11Idle;

            SetupToolStateMachineTransitions(tool11Idle, tool11Run, tool11Attack, tool11RunAttack);

            // === TOOL_13 STATE MACHINE ===
            var tool13Idle = CreateState(tool13SM, "Idle", idleSwordClip ?? idleClip, new Vector3(140, -10, 0));
            var tool13Run = CreateState(tool13SM, "Run", runClip, new Vector3(140, 60, 0));
            var tool13Attack = CreateState(tool13SM, "Attack", swordSlashClip, new Vector3(420, 0, 0), 1.1f);
            var tool13RunAttack = CreateState(tool13SM, "RunAttack", runShootClip ?? runClip, new Vector3(420, 60, 0), 1.1f);
            tool13SM.defaultState = tool13Idle;

            SetupToolStateMachineTransitions(tool13Idle, tool13Run, tool13Attack, tool13RunAttack);

            // === ANY STATE TRANSITIONS ===
            // AnyState -> Default (UpdateCustomState trigger + IsCustomStateActive = false)
            var anyToDefault = rootStateMachine.AddAnyStateTransition(defaultSM);
            anyToDefault.AddCondition(AnimatorConditionMode.If, 0, "UpdateCustomState");
            anyToDefault.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCustomStateActive");
            anyToDefault.hasExitTime = false;
            anyToDefault.duration = 0.25f;

            // AnyState -> Tool_10 (UpdateCustomState + IsCustomStateActive + CustomStateType == 10)
            var anyToTool10 = rootStateMachine.AddAnyStateTransition(tool10SM);
            anyToTool10.AddCondition(AnimatorConditionMode.If, 0, "UpdateCustomState");
            anyToTool10.AddCondition(AnimatorConditionMode.If, 0, "IsCustomStateActive");
            anyToTool10.AddCondition(AnimatorConditionMode.Equals, 10, "CustomStateType");
            anyToTool10.hasExitTime = false;
            anyToTool10.duration = 0.2f;

            // AnyState -> Tool_11 (UpdateCustomState + IsCustomStateActive + CustomStateType == 11)
            var anyToTool11 = rootStateMachine.AddAnyStateTransition(tool11SM);
            anyToTool11.AddCondition(AnimatorConditionMode.If, 0, "UpdateCustomState");
            anyToTool11.AddCondition(AnimatorConditionMode.If, 0, "IsCustomStateActive");
            anyToTool11.AddCondition(AnimatorConditionMode.Equals, 11, "CustomStateType");
            anyToTool11.hasExitTime = false;
            anyToTool11.duration = 0.25f;

            // AnyState -> Tool_13 (UpdateCustomState + IsCustomStateActive + CustomStateType == 13)
            var anyToTool13 = rootStateMachine.AddAnyStateTransition(tool13SM);
            anyToTool13.AddCondition(AnimatorConditionMode.If, 0, "UpdateCustomState");
            anyToTool13.AddCondition(AnimatorConditionMode.If, 0, "IsCustomStateActive");
            anyToTool13.AddCondition(AnimatorConditionMode.Equals, 13, "CustomStateType");
            anyToTool13.hasExitTime = false;
            anyToTool13.duration = 0.25f;

            // Set default state machine entry
            rootStateMachine.defaultState = defaultIdle;

            // Save
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Controller created at: {CONTROLLER_PATH}");
            Debug.Log("Structure:");
            Debug.Log("  - Default: Idle, Run, InteractAction");
            Debug.Log("  - Tool_10: Idle, Run, Attack (Punch), RunAttack");
            Debug.Log("  - Tool_11: Idle, Run, Attack (Sword), RunAttack");
            Debug.Log("  - Tool_13: Idle, Run, Attack (Kick), RunAttack");

            EditorUtility.DisplayDialog("Success",
                $"Controller created at:\n{CONTROLLER_PATH}\n\n" +
                "Structure matches Controller_MainCharacter:\n" +
                "- 6 parameters\n" +
                "- 4 state machines (Default, Tool_10, Tool_11, Tool_13)\n" +
                "- All transitions configured",
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

        private void AddTransition(AnimatorState from, AnimatorState to, string condition, bool value, float duration, bool hasExitTime = false, float exitTime = 0f)
        {
            var transition = from.AddTransition(to);
            transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, condition);
            transition.hasExitTime = hasExitTime;
            transition.exitTime = exitTime;
            transition.duration = duration;
        }

        private void SetupToolStateMachineTransitions(AnimatorState idle, AnimatorState run, AnimatorState attack, AnimatorState runAttack)
        {
            // Idle -> Run (IsMoving = true, IsDoingAction = false)
            var idleToRun = idle.AddTransition(run);
            idleToRun.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
            idleToRun.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDoingAction");
            idleToRun.hasExitTime = false;
            idleToRun.duration = 0.15f;

            // Idle -> Attack (IsDoingAction = true, IsMoving = false)
            var idleToAttack = idle.AddTransition(attack);
            idleToAttack.AddCondition(AnimatorConditionMode.If, 0, "IsDoingAction");
            idleToAttack.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
            idleToAttack.hasExitTime = false;
            idleToAttack.duration = 0.1f;

            // Idle -> RunAttack (IsDoingAction = true, IsMoving = true)
            var idleToRunAttack = idle.AddTransition(runAttack);
            idleToRunAttack.AddCondition(AnimatorConditionMode.If, 0, "IsDoingAction");
            idleToRunAttack.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
            idleToRunAttack.hasExitTime = false;
            idleToRunAttack.duration = 0.1f;

            // Run -> Idle (IsMoving = false)
            var runToIdle = run.AddTransition(idle);
            runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
            runToIdle.hasExitTime = false;
            runToIdle.duration = 0.1f;

            // Run -> RunAttack (IsDoingAction = true)
            var runToRunAttack = run.AddTransition(runAttack);
            runToRunAttack.AddCondition(AnimatorConditionMode.If, 0, "IsDoingAction");
            runToRunAttack.hasExitTime = false;
            runToRunAttack.duration = 0.2f;

            // Attack -> Idle (IsDoingAction = false)
            var attackToIdle = attack.AddTransition(idle);
            attackToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDoingAction");
            attackToIdle.hasExitTime = false;
            attackToIdle.duration = 0.1f;

            // Attack -> RunAttack (IsDoingAction = true, IsMoving = true)
            var attackToRunAttack = attack.AddTransition(runAttack);
            attackToRunAttack.AddCondition(AnimatorConditionMode.If, 0, "IsDoingAction");
            attackToRunAttack.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
            attackToRunAttack.hasExitTime = false;
            attackToRunAttack.duration = 0.05f;

            // RunAttack -> Idle (IsMoving = false, with exit time)
            var runAttackToIdle = runAttack.AddTransition(idle);
            runAttackToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
            runAttackToIdle.hasExitTime = true;
            runAttackToIdle.exitTime = 0.63f;
            runAttackToIdle.duration = 0.15f;

            // RunAttack -> Attack (IsDoingAction = true, IsMoving = false)
            var runAttackToAttack = runAttack.AddTransition(attack);
            runAttackToAttack.AddCondition(AnimatorConditionMode.If, 0, "IsDoingAction");
            runAttackToAttack.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
            runAttackToAttack.hasExitTime = false;
            runAttackToAttack.duration = 0.05f;

            // RunAttack -> Run (IsDoingAction = false, with exit time)
            var runAttackToRun = runAttack.AddTransition(run);
            runAttackToRun.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDoingAction");
            runAttackToRun.hasExitTime = true;
            runAttackToRun.exitTime = 0.6f;
            runAttackToRun.duration = 0.1f;
        }
    }
}
