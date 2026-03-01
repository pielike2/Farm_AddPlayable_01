using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Linq;

namespace Editor
{
    /// <summary>
    /// Adds Animation Events to attack animations in Controller_Formal.
    /// </summary>
    public class AnimationEventAdder : EditorWindow
    {
        private AnimatorController _controller;
        private List<AnimationClipInfo> _clips = new List<AnimationClipInfo>();
        private Vector2 _scrollPosition;

        private class AnimationClipInfo
        {
            public AnimationClip Clip;
            public string StateName;
            public bool HasEvent;
            public float EventTime;
        }

        [MenuItem("Tools/Add Animation Events to Controller_Formal")]
        public static void ShowWindow()
        {
            var window = GetWindow<AnimationEventAdder>("Animation Event Adder");
            window.minSize = new Vector2(500, 400);
            window.FindController();
        }

        private void FindController()
        {
            // Try to find Controller_Formal
            var guids = AssetDatabase.FindAssets("Controller_Formal t:AnimatorController");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                if (_controller != null)
                {
                    Debug.Log($"Found Controller_Formal at: {path}");
                    AnalyzeController();
                }
            }
            else
            {
                Debug.LogWarning("Controller_Formal not found!");
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Animation Event Adder", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "This tool adds Animation Events to attack animations.\n\n" +
                "The event will call API_ReceiveInt(0) which triggers the attack damage.\n\n" +
                "Select the animation clip and click 'Add Event' to add at the specified time.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            _controller = EditorGUILayout.ObjectField("Animator Controller", _controller, typeof(AnimatorController), false) as AnimatorController;

            if (GUILayout.Button("Analyze Controller"))
            {
                AnalyzeController();
            }

            EditorGUILayout.Space(10);

            if (_clips.Count > 0)
            {
                EditorGUILayout.LabelField($"Found {_clips.Count} animation clips:", EditorStyles.boldLabel);

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

                foreach (var clipInfo in _clips)
                {
                    EditorGUILayout.BeginVertical("box");

                    EditorGUILayout.LabelField($"State: {clipInfo.StateName}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Clip: {clipInfo.Clip.name}");
                    EditorGUILayout.LabelField($"Length: {clipInfo.Clip.length:F2}s");

                    if (clipInfo.HasEvent)
                    {
                        GUI.backgroundColor = Color.green;
                        EditorGUILayout.LabelField($"Has API_ReceiveInt event at {clipInfo.EventTime:F2}s");
                        GUI.backgroundColor = Color.white;
                    }
                    else
                    {
                        GUI.backgroundColor = Color.yellow;
                        EditorGUILayout.LabelField("No API_ReceiveInt event");
                        GUI.backgroundColor = Color.white;
                    }

                    EditorGUILayout.BeginHorizontal();

                    // Check if this looks like an attack animation
                    bool looksLikeAttack = clipInfo.StateName.ToLower().Contains("attack") ||
                                           clipInfo.StateName.ToLower().Contains("chop") ||
                                           clipInfo.StateName.ToLower().Contains("hit") ||
                                           clipInfo.StateName.ToLower().Contains("action") ||
                                           clipInfo.Clip.name.ToLower().Contains("attack") ||
                                           clipInfo.Clip.name.ToLower().Contains("chop") ||
                                           clipInfo.Clip.name.ToLower().Contains("hit");

                    if (looksLikeAttack)
                    {
                        GUI.backgroundColor = Color.cyan;
                        EditorGUILayout.LabelField("[Likely Attack Animation]", EditorStyles.miniLabel);
                        GUI.backgroundColor = Color.white;
                    }

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();

                    float eventTime = clipInfo.Clip.length * 0.3f; // Default to 30% of animation
                    eventTime = EditorGUILayout.FloatField("Event Time (s)", eventTime);

                    if (GUILayout.Button("Add Event", GUILayout.Width(100)))
                    {
                        AddEventToClip(clipInfo.Clip, eventTime);
                        AnalyzeController(); // Refresh
                    }

                    if (clipInfo.HasEvent)
                    {
                        GUI.backgroundColor = Color.red;
                        if (GUILayout.Button("Remove Events", GUILayout.Width(100)))
                        {
                            RemoveEventsFromClip(clipInfo.Clip);
                            AnalyzeController(); // Refresh
                        }
                        GUI.backgroundColor = Color.white;
                    }

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(5);
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space(10);

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Add Event to ALL Attack-like Animations", GUILayout.Height(35)))
            {
                AddEventsToAllAttackAnimations();
            }
            GUI.backgroundColor = Color.white;
        }

        private void AnalyzeController()
        {
            _clips.Clear();

            if (_controller == null)
                return;

            foreach (var layer in _controller.layers)
            {
                AnalyzeStateMachine(layer.stateMachine, "");
            }

            Debug.Log($"Found {_clips.Count} animation clips in {_controller.name}");
        }

        private void AnalyzeStateMachine(AnimatorStateMachine stateMachine, string prefix)
        {
            foreach (var state in stateMachine.states)
            {
                var clip = state.state.motion as AnimationClip;
                if (clip != null)
                {
                    var info = new AnimationClipInfo
                    {
                        Clip = clip,
                        StateName = prefix + state.state.name,
                        HasEvent = false,
                        EventTime = 0
                    };

                    // Check for existing API_ReceiveInt event
                    var events = AnimationUtility.GetAnimationEvents(clip);
                    foreach (var evt in events)
                    {
                        if (evt.functionName == "API_ReceiveInt")
                        {
                            info.HasEvent = true;
                            info.EventTime = evt.time;
                            break;
                        }
                    }

                    _clips.Add(info);
                }

                // Check BlendTree
                var blendTree = state.state.motion as BlendTree;
                if (blendTree != null)
                {
                    AnalyzeBlendTree(blendTree, prefix + state.state.name + "/");
                }
            }

            // Check sub-state machines
            foreach (var subMachine in stateMachine.stateMachines)
            {
                AnalyzeStateMachine(subMachine.stateMachine, prefix + subMachine.stateMachine.name + "/");
            }
        }

        private void AnalyzeBlendTree(BlendTree blendTree, string prefix)
        {
            foreach (var child in blendTree.children)
            {
                var clip = child.motion as AnimationClip;
                if (clip != null)
                {
                    var info = new AnimationClipInfo
                    {
                        Clip = clip,
                        StateName = prefix + clip.name,
                        HasEvent = false,
                        EventTime = 0
                    };

                    var events = AnimationUtility.GetAnimationEvents(clip);
                    foreach (var evt in events)
                    {
                        if (evt.functionName == "API_ReceiveInt")
                        {
                            info.HasEvent = true;
                            info.EventTime = evt.time;
                            break;
                        }
                    }

                    _clips.Add(info);
                }

                var subTree = child.motion as BlendTree;
                if (subTree != null)
                {
                    AnalyzeBlendTree(subTree, prefix);
                }
            }
        }

        private void AddEventToClip(AnimationClip clip, float time)
        {
            // Check if already has this event
            var existingEvents = AnimationUtility.GetAnimationEvents(clip);
            bool hasEvent = existingEvents.Any(e => e.functionName == "API_ReceiveInt");
            if (hasEvent)
            {
                Debug.Log($"Clip {clip.name} already has API_ReceiveInt event");
                return;
            }

            // Get the asset path of the clip
            string clipPath = AssetDatabase.GetAssetPath(clip);

            // Check if this is an FBX file - need to use ModelImporter
            if (clipPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
            {
                AddEventToFbxClip(clip, clipPath, time);
            }
            else
            {
                // For standalone .anim files, use the direct approach
                AddEventToStandaloneClip(clip, time);
            }
        }

        private void AddEventToFbxClip(AnimationClip clip, string fbxPath, float time)
        {
            var modelImporter = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (modelImporter == null)
            {
                Debug.LogError($"Failed to get ModelImporter for {fbxPath}");
                return;
            }

            // Get existing clip animations from importer
            var clipAnimations = modelImporter.clipAnimations;

            // If no custom clip animations, get the default ones
            if (clipAnimations == null || clipAnimations.Length == 0)
            {
                clipAnimations = modelImporter.defaultClipAnimations;
            }

            // Find the matching clip by name
            bool found = false;
            for (int i = 0; i < clipAnimations.Length; i++)
            {
                if (clipAnimations[i].name == clip.name)
                {
                    // Get existing events
                    var events = clipAnimations[i].events?.ToList() ?? new List<AnimationEvent>();

                    // Check if already has the event
                    if (events.Any(e => e.functionName == "API_ReceiveInt"))
                    {
                        Debug.Log($"Clip {clip.name} already has API_ReceiveInt event in importer");
                        return;
                    }

                    // Add new event
                    var newEvent = new AnimationEvent
                    {
                        functionName = "API_ReceiveInt",
                        intParameter = 0,
                        time = time
                    };
                    events.Add(newEvent);

                    clipAnimations[i].events = events.ToArray();
                    found = true;
                    Debug.Log($"Added API_ReceiveInt event to FBX clip {clip.name} at {time:F2}s");
                    break;
                }
            }

            if (!found)
            {
                Debug.LogError($"Could not find clip {clip.name} in FBX importer settings");
                return;
            }

            // Apply changes to importer
            modelImporter.clipAnimations = clipAnimations;
            EditorUtility.SetDirty(modelImporter);
            modelImporter.SaveAndReimport();

            Debug.Log($"Saved and reimported {fbxPath}");
        }

        private void AddEventToStandaloneClip(AnimationClip clip, float time)
        {
            var events = AnimationUtility.GetAnimationEvents(clip).ToList();

            var newEvent = new AnimationEvent
            {
                functionName = "API_ReceiveInt",
                intParameter = 0,
                time = time
            };

            events.Add(newEvent);
            AnimationUtility.SetAnimationEvents(clip, events.ToArray());

            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();

            Debug.Log($"Added API_ReceiveInt event to standalone clip {clip.name} at {time:F2}s");
        }

        private void RemoveEventsFromClip(AnimationClip clip)
        {
            string clipPath = AssetDatabase.GetAssetPath(clip);

            if (clipPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
            {
                RemoveEventsFromFbxClip(clip, clipPath);
            }
            else
            {
                var events = AnimationUtility.GetAnimationEvents(clip).ToList();
                events.RemoveAll(e => e.functionName == "API_ReceiveInt");
                AnimationUtility.SetAnimationEvents(clip, events.ToArray());

                EditorUtility.SetDirty(clip);
                AssetDatabase.SaveAssets();

                Debug.Log($"Removed API_ReceiveInt events from standalone clip {clip.name}");
            }
        }

        private void RemoveEventsFromFbxClip(AnimationClip clip, string fbxPath)
        {
            var modelImporter = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (modelImporter == null)
            {
                Debug.LogError($"Failed to get ModelImporter for {fbxPath}");
                return;
            }

            var clipAnimations = modelImporter.clipAnimations;
            if (clipAnimations == null || clipAnimations.Length == 0)
            {
                clipAnimations = modelImporter.defaultClipAnimations;
            }

            bool found = false;
            for (int i = 0; i < clipAnimations.Length; i++)
            {
                if (clipAnimations[i].name == clip.name)
                {
                    var events = clipAnimations[i].events?.ToList() ?? new List<AnimationEvent>();
                    int removed = events.RemoveAll(e => e.functionName == "API_ReceiveInt");

                    if (removed > 0)
                    {
                        clipAnimations[i].events = events.ToArray();
                        found = true;
                        Debug.Log($"Removed {removed} API_ReceiveInt event(s) from FBX clip {clip.name}");
                    }
                    else
                    {
                        Debug.Log($"No API_ReceiveInt events found in FBX clip {clip.name}");
                        return;
                    }
                    break;
                }
            }

            if (!found)
            {
                Debug.LogError($"Could not find clip {clip.name} in FBX importer settings");
                return;
            }

            modelImporter.clipAnimations = clipAnimations;
            EditorUtility.SetDirty(modelImporter);
            modelImporter.SaveAndReimport();

            Debug.Log($"Saved and reimported {fbxPath}");
        }

        private void AddEventsToAllAttackAnimations()
        {
            int added = 0;

            foreach (var clipInfo in _clips)
            {
                if (clipInfo.HasEvent)
                    continue;

                bool looksLikeAttack = clipInfo.StateName.ToLower().Contains("attack") ||
                                       clipInfo.StateName.ToLower().Contains("chop") ||
                                       clipInfo.StateName.ToLower().Contains("hit") ||
                                       clipInfo.StateName.ToLower().Contains("action") ||
                                       clipInfo.Clip.name.ToLower().Contains("attack") ||
                                       clipInfo.Clip.name.ToLower().Contains("chop") ||
                                       clipInfo.Clip.name.ToLower().Contains("hit");

                if (looksLikeAttack)
                {
                    float eventTime = clipInfo.Clip.length * 0.3f; // 30% into animation
                    AddEventToClip(clipInfo.Clip, eventTime);
                    added++;
                }
            }

            AnalyzeController(); // Refresh

            EditorUtility.DisplayDialog("Done", $"Added events to {added} attack animations.", "OK");
        }
    }
}
