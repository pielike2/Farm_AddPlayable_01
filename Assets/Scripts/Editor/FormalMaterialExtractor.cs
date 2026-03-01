using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace Editor
{
    /// <summary>
    /// Extracts materials from Formal.fbx and configures the FBX to use them.
    /// This ensures the prefab uses the correct materials with original colors.
    /// </summary>
    public class FormalMaterialExtractor : EditorWindow
    {
        private const string FBX_PATH = "Assets/Art/Models/Characters/Formal.fbx";
        private const string MATERIALS_PATH = "Assets/Art/Models/Characters/materials";
        private const string PREFAB_PATH = "Assets/Prefabs/Characters/Julia_Formal.prefab";

        [MenuItem("Tools/Extract Formal Materials (Fix Textures)")]
        public static void ShowWindow()
        {
            var window = GetWindow<FormalMaterialExtractor>("Formal Materials");
            window.minSize = new Vector2(500, 400);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Formal.fbx Material Extractor", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool extracts materials from Formal.fbx and configures\n" +
                "the FBX importer to use them. This fixes missing materials.\n\n" +
                "Materials in Formal.fbx:\n" +
                "- Skin (skin color)\n" +
                "- Brown (hair/clothing)\n" +
                "- Gold (accessories)\n" +
                "- Red (clothing)\n" +
                "- LimeGreen (clothing)",
                MessageType.Info);

            EditorGUILayout.Space(20);

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("EXTRACT & CONFIGURE MATERIALS", GUILayout.Height(50)))
            {
                ExtractAndConfigure();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);

            if (GUILayout.Button("List FBX Materials", GUILayout.Height(30)))
            {
                ListFBXMaterials();
            }

            if (GUILayout.Button("Recreate Julia_Formal Prefab", GUILayout.Height(30)))
            {
                RecreatePrefab();
            }
        }

        private void ExtractAndConfigure()
        {
            var importer = AssetImporter.GetAtPath(FBX_PATH) as ModelImporter;
            if (importer == null)
            {
                EditorUtility.DisplayDialog("Error", $"FBX not found: {FBX_PATH}", "OK");
                return;
            }

            // Step 1: Set material import mode to import embedded materials
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.External;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            // Wait for reimport
            AssetDatabase.Refresh();

            // Step 2: Get materials from FBX and extract them
            var fbxAssets = AssetDatabase.LoadAllAssetsAtPath(FBX_PATH);
            var extractedMaterials = new Dictionary<string, Material>();
            var log = new System.Text.StringBuilder();
            log.AppendLine("=== MATERIAL EXTRACTION ===\n");

            foreach (var asset in fbxAssets)
            {
                if (asset is Material mat)
                {
                    string matName = mat.name;
                    string targetPath = $"{MATERIALS_PATH}/{matName}.mat";

                    log.AppendLine($"Found material: {matName}");
                    log.AppendLine($"  Color: {mat.color}");
                    log.AppendLine($"  Shader: {mat.shader.name}");

                    // Check if external material exists
                    Material existingMat = AssetDatabase.LoadAssetAtPath<Material>(targetPath);

                    if (existingMat == null)
                    {
                        // Create new material copying properties from embedded
                        Material newMat = new Material(Shader.Find("Standard"));
                        newMat.name = matName;
                        newMat.color = mat.color;
                        newMat.SetFloat("_Glossiness", 0f);
                        newMat.SetFloat("_Metallic", 0f);

                        AssetDatabase.CreateAsset(newMat, targetPath);
                        log.AppendLine($"  -> Created: {targetPath}");
                        extractedMaterials[matName] = newMat;
                    }
                    else
                    {
                        // Update existing material color to match FBX
                        existingMat.color = mat.color;
                        EditorUtility.SetDirty(existingMat);
                        log.AppendLine($"  -> Updated existing: {targetPath}");
                        extractedMaterials[matName] = existingMat;
                    }
                }
            }

            AssetDatabase.SaveAssets();

            // Step 3: Configure FBX to use external materials
            log.AppendLine("\n=== CONFIGURING FBX REMAPS ===\n");

            foreach (var kvp in extractedMaterials)
            {
                string matName = kvp.Key;
                Material externalMat = kvp.Value;

                var identifier = new AssetImporter.SourceAssetIdentifier(typeof(Material), matName);
                importer.AddRemap(identifier, externalMat);
                log.AppendLine($"Remapped: {matName} -> {AssetDatabase.GetAssetPath(externalMat)}");
            }

            importer.SaveAndReimport();
            AssetDatabase.Refresh();

            Debug.Log(log.ToString());

            EditorUtility.DisplayDialog("Done",
                $"Extracted/updated {extractedMaterials.Count} materials.\n\n" +
                "Materials are now configured in FBX.\n" +
                "Check Console for details.\n\n" +
                "Click 'Recreate Julia_Formal Prefab' if needed.",
                "OK");
        }

        private void ListFBXMaterials()
        {
            var log = new System.Text.StringBuilder();
            log.AppendLine("=== MATERIALS IN FORMAL.FBX ===\n");

            var fbxAssets = AssetDatabase.LoadAllAssetsAtPath(FBX_PATH);
            int count = 0;

            foreach (var asset in fbxAssets)
            {
                if (asset is Material mat)
                {
                    count++;
                    log.AppendLine($"{count}. {mat.name}");
                    log.AppendLine($"   Color: R={mat.color.r:F2} G={mat.color.g:F2} B={mat.color.b:F2}");
                    log.AppendLine($"   Shader: {mat.shader.name}");

                    // Check if external version exists
                    string extPath = $"{MATERIALS_PATH}/{mat.name}.mat";
                    var extMat = AssetDatabase.LoadAssetAtPath<Material>(extPath);
                    if (extMat != null)
                    {
                        log.AppendLine($"   External: {extPath}");
                        log.AppendLine($"   Ext Color: R={extMat.color.r:F2} G={extMat.color.g:F2} B={extMat.color.b:F2}");
                    }
                    else
                    {
                        log.AppendLine($"   External: NOT FOUND");
                    }
                    log.AppendLine();
                }
            }

            log.AppendLine($"\nTotal: {count} materials");

            Debug.Log(log.ToString());
        }

        private void RecreatePrefab()
        {
            // Use existing JuliaFormalCreator
            if (EditorUtility.DisplayDialog("Recreate Prefab",
                "This will recreate Julia_Formal.prefab using JuliaFormalCreator.\n\n" +
                "The new prefab will use materials from Formal.fbx.\n\n" +
                "Continue?", "Yes", "Cancel"))
            {
                // Open the creator window
                EditorApplication.ExecuteMenuItem("Tools/Create Julia_Formal (Clean)");
            }
        }
    }
}
