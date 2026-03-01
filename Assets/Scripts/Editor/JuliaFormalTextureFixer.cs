using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Editor
{
    /// <summary>
    /// Fixes missing/broken textures on Julia_Formal character model.
    /// Extracts materials from Formal.fbx and assigns them properly.
    /// </summary>
    public class JuliaFormalTextureFixer : EditorWindow
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Characters/Julia_Formal.prefab";
        private const string FBX_PATH = "Assets/Art/Models/Characters/Formal.fbx";
        private const string MATERIALS_PATH = "Assets/Art/Models/Characters/materials";
        private const string TEXTURES_PATH = "Assets/Art/Models/Characters/textures";

        private GameObject _sceneInstance;
        private Vector2 _scrollPosition;
        private List<MeshMaterialInfo> _meshInfos = new List<MeshMaterialInfo>();

        private class MeshMaterialInfo
        {
            public SkinnedMeshRenderer Renderer;
            public string MeshName;
            public Material[] CurrentMaterials;
            public string[] MaterialNames;
            public bool HasValidMaterials;
            public string Issues;
        }

        [MenuItem("Tools/Julia Formal Texture Fixer")]
        public static void ShowWindow()
        {
            var window = GetWindow<JuliaFormalTextureFixer>("Texture Fixer");
            window.minSize = new Vector2(650, 550);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Julia_Formal Texture Fixer", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "This tool fixes missing materials on Julia_Formal:\n\n" +
                "The Formal.fbx uses embedded materials (Skin, Brown, Gold, Red, LimeGreen)\n" +
                "which may get lost when Unity reimports. This tool:\n\n" +
                "1. EXTRACT FBX MATERIALS - Extracts materials from FBX to external .mat files\n" +
                "2. REASSIGN MATERIALS - Reassigns extracted materials to the prefab\n\n" +
                "Use on scene instance, then apply to prefab.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // Scene instance field
            EditorGUILayout.BeginHorizontal();
            _sceneInstance = EditorGUILayout.ObjectField("Julia_Formal (Scene)", _sceneInstance, typeof(GameObject), true) as GameObject;
            if (GUILayout.Button("Find", GUILayout.Width(50)))
            {
                _sceneInstance = GameObject.Find("Julia_Formal");
                if (_sceneInstance == null)
                    Debug.LogWarning("Julia_Formal not found in scene. Place the prefab in scene first.");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(15);

            // Main action buttons
            EditorGUILayout.LabelField("Step 1: Extract Materials from FBX", EditorStyles.boldLabel);
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("EXTRACT FBX MATERIALS", GUILayout.Height(40)))
            {
                ExtractFBXMaterials();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Step 2: Analyze & Fix", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("ANALYZE", GUILayout.Height(35)))
            {
                Analyze();
            }

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("REASSIGN MATERIALS", GUILayout.Height(35)))
            {
                ReassignMaterials();
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("Apply to Prefab", GUILayout.Height(35)))
            {
                ApplyToPrefab();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // List available materials
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("List Available Materials", GUILayout.Height(25)))
            {
                ListAvailableMaterials();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);

            // Results display
            if (_meshInfos.Count > 0)
            {
                EditorGUILayout.LabelField($"Found {_meshInfos.Count} SkinnedMeshRenderers:", EditorStyles.boldLabel);

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

                foreach (var info in _meshInfos)
                {
                    Color bgColor = info.HasValidMaterials ? new Color(0.7f, 1f, 0.7f) : new Color(1f, 0.8f, 0.8f);
                    GUI.backgroundColor = bgColor;

                    EditorGUILayout.BeginVertical("box");
                    GUI.backgroundColor = Color.white;

                    EditorGUILayout.LabelField($"Mesh: {info.MeshName}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Materials ({info.CurrentMaterials.Length}):");

                    for (int i = 0; i < info.MaterialNames.Length; i++)
                    {
                        string matInfo = info.MaterialNames[i];
                        if (info.CurrentMaterials[i] != null)
                        {
                            string path = AssetDatabase.GetAssetPath(info.CurrentMaterials[i]);
                            matInfo += string.IsNullOrEmpty(path) ? " (embedded)" : $" ({path})";
                        }
                        EditorGUILayout.LabelField($"  [{i}] {matInfo}");
                    }

                    if (!string.IsNullOrEmpty(info.Issues))
                    {
                        EditorGUILayout.LabelField($"Issues: {info.Issues}", EditorStyles.miniLabel);
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void ExtractFBXMaterials()
        {
            var importer = AssetImporter.GetAtPath(FBX_PATH) as ModelImporter;
            if (importer == null)
            {
                EditorUtility.DisplayDialog("Error", $"FBX not found at {FBX_PATH}", "OK");
                return;
            }

            // Configure material import to use external materials
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.External;

            // Save settings first
            importer.SaveAndReimport();

            // Now extract materials
            var extractedMaterials = new List<string>();
            var fbxAssets = AssetDatabase.LoadAllAssetsAtPath(FBX_PATH);

            foreach (var asset in fbxAssets)
            {
                if (asset is Material mat)
                {
                    string matName = mat.name;
                    string targetPath = $"{MATERIALS_PATH}/{matName}.mat";

                    // Check if material already exists
                    if (AssetDatabase.LoadAssetAtPath<Material>(targetPath) != null)
                    {
                        extractedMaterials.Add($"{matName} (already exists)");
                        continue;
                    }

                    // Create a copy of the material
                    Material newMat = new Material(mat);
                    newMat.name = matName;

                    AssetDatabase.CreateAsset(newMat, targetPath);
                    extractedMaterials.Add(matName);
                    Debug.Log($"Extracted material: {targetPath}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Update FBX external objects to use the extracted materials
            UpdateFBXExternalObjects();

            string msg = extractedMaterials.Count > 0
                ? $"Extracted {extractedMaterials.Count} materials:\n\n" + string.Join("\n", extractedMaterials)
                : "No new materials to extract (may already be extracted).";

            EditorUtility.DisplayDialog("Extract Complete", msg, "OK");
        }

        private void UpdateFBXExternalObjects()
        {
            var importer = AssetImporter.GetAtPath(FBX_PATH) as ModelImporter;
            if (importer == null) return;

            // Get all materials from the FBX
            var fbxAssets = AssetDatabase.LoadAllAssetsAtPath(FBX_PATH);
            var externalObjects = new Dictionary<AssetImporter.SourceAssetIdentifier, Object>();

            foreach (var asset in fbxAssets)
            {
                if (asset is Material embeddedMat)
                {
                    string matName = embeddedMat.name;
                    string externalPath = $"{MATERIALS_PATH}/{matName}.mat";
                    Material externalMat = AssetDatabase.LoadAssetAtPath<Material>(externalPath);

                    if (externalMat != null)
                    {
                        var identifier = new AssetImporter.SourceAssetIdentifier(typeof(Material), matName);
                        importer.AddRemap(identifier, externalMat);
                        Debug.Log($"Remapped material: {matName} -> {externalPath}");
                    }
                }
            }

            importer.SaveAndReimport();
        }

        private void Analyze()
        {
            _meshInfos.Clear();

            if (_sceneInstance == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign Julia_Formal scene instance first!", "OK");
                return;
            }

            var renderers = _sceneInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            foreach (var renderer in renderers)
            {
                var info = new MeshMaterialInfo
                {
                    Renderer = renderer,
                    MeshName = renderer.gameObject.name,
                    CurrentMaterials = renderer.sharedMaterials,
                    MaterialNames = renderer.sharedMaterials.Select(m => m != null ? m.name : "MISSING").ToArray(),
                    HasValidMaterials = true,
                    Issues = ""
                };

                // Check for issues
                var issues = new List<string>();
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat == null)
                    {
                        issues.Add("null/missing material reference");
                        info.HasValidMaterials = false;
                    }
                }

                info.Issues = string.Join("; ", issues);
                _meshInfos.Add(info);
            }

            var sb = new StringBuilder();
            sb.AppendLine("=== MATERIAL ANALYSIS ===\n");

            int valid = _meshInfos.Count(m => m.HasValidMaterials);
            int invalid = _meshInfos.Count - valid;

            sb.AppendLine($"Total SkinnedMeshRenderers: {_meshInfos.Count}");
            sb.AppendLine($"Valid: {valid}");
            sb.AppendLine($"With issues: {invalid}");

            foreach (var info in _meshInfos)
            {
                sb.AppendLine($"\n{info.MeshName}:");
                sb.AppendLine($"  Materials: {string.Join(", ", info.MaterialNames)}");
                sb.AppendLine($"  Valid: {info.HasValidMaterials}");
                if (!string.IsNullOrEmpty(info.Issues))
                    sb.AppendLine($"  Issues: {info.Issues}");
            }

            Debug.Log(sb.ToString());
        }

        private void ReassignMaterials()
        {
            if (_sceneInstance == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign Julia_Formal scene instance first!", "OK");
                return;
            }

            // Load available materials from the materials folder
            var availableMaterials = new Dictionary<string, Material>();
            var matGuids = AssetDatabase.FindAssets("t:Material", new[] { MATERIALS_PATH });
            foreach (var guid in matGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null)
                {
                    availableMaterials[mat.name] = mat;
                }
            }

            if (availableMaterials.Count == 0)
            {
                EditorUtility.DisplayDialog("Error",
                    "No materials found in materials folder!\n\n" +
                    "Click 'EXTRACT FBX MATERIALS' first.", "OK");
                return;
            }

            Debug.Log($"Available materials: {string.Join(", ", availableMaterials.Keys)}");

            Undo.RegisterFullObjectHierarchyUndo(_sceneInstance, "Reassign Julia_Formal Materials");

            int fixedCount = 0;
            var renderers = _sceneInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            foreach (var renderer in renderers)
            {
                bool needsFix = renderer.sharedMaterials.Any(m => m == null);

                if (needsFix)
                {
                    // Try to find materials by name from the mesh
                    Material[] newMaterials = new Material[renderer.sharedMaterials.Length];

                    for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                    {
                        var currentMat = renderer.sharedMaterials[i];

                        if (currentMat != null)
                        {
                            // Material exists, try to find external version
                            if (availableMaterials.TryGetValue(currentMat.name, out Material externalMat))
                            {
                                newMaterials[i] = externalMat;
                            }
                            else
                            {
                                newMaterials[i] = currentMat;
                            }
                        }
                        else
                        {
                            // Material is null, try common material names based on index
                            // Formal.fbx typically uses: Skin (index 0), Brown/Gold/LimeGreen for clothing
                            string[] fallbackNames = { "Skin", "Brown", "LimeGreen", "Gold", "Red" };

                            foreach (var fallback in fallbackNames)
                            {
                                if (availableMaterials.TryGetValue(fallback, out Material fallbackMat))
                                {
                                    newMaterials[i] = fallbackMat;
                                    Debug.Log($"Using fallback material {fallback} for {renderer.gameObject.name}[{i}]");
                                    break;
                                }
                            }

                            // If still null, use first available
                            if (newMaterials[i] == null && availableMaterials.Count > 0)
                            {
                                newMaterials[i] = availableMaterials.Values.First();
                            }
                        }
                    }

                    renderer.sharedMaterials = newMaterials;
                    EditorUtility.SetDirty(renderer);
                    fixedCount++;
                    Debug.Log($"Fixed materials on {renderer.gameObject.name}");
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Analyze();

            EditorUtility.DisplayDialog("Done",
                $"Fixed {fixedCount} renderers.\n\n" +
                "Click 'Apply to Prefab' to save changes.",
                "OK");
        }

        private void ListAvailableMaterials()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== AVAILABLE MATERIALS ===\n");
            sb.AppendLine($"Path: {MATERIALS_PATH}\n");

            var matGuids = AssetDatabase.FindAssets("t:Material", new[] { MATERIALS_PATH });
            foreach (var guid in matGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null)
                {
                    string texInfo = mat.mainTexture != null ? $"texture: {mat.mainTexture.name}" : "no texture";
                    sb.AppendLine($"  {mat.name} ({texInfo})");
                }
            }

            // Also list materials from FBX
            sb.AppendLine($"\n=== MATERIALS IN FBX ===\n");
            sb.AppendLine($"Path: {FBX_PATH}\n");

            var fbxAssets = AssetDatabase.LoadAllAssetsAtPath(FBX_PATH);
            foreach (var asset in fbxAssets)
            {
                if (asset is Material mat)
                {
                    string texInfo = mat.mainTexture != null ? $"texture: {mat.mainTexture.name}" : "no texture (color only)";
                    sb.AppendLine($"  {mat.name} ({texInfo})");
                }
            }

            Debug.Log(sb.ToString());
        }

        private void ApplyToPrefab()
        {
            if (_sceneInstance == null)
            {
                EditorUtility.DisplayDialog("Error", "Julia_Formal scene instance not assigned!", "OK");
                return;
            }

            var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(_sceneInstance);
            if (string.IsNullOrEmpty(prefabPath))
            {
                EditorUtility.DisplayDialog("Error", "Julia_Formal is not a prefab instance!", "OK");
                return;
            }

            PrefabUtility.ApplyPrefabInstance(_sceneInstance, InteractionMode.UserAction);
            Debug.Log($"Applied overrides to {prefabPath}");
            EditorUtility.DisplayDialog("Success", $"Applied all overrides to:\n{prefabPath}", "OK");
        }
    }
}
