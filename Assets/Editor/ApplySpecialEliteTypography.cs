using UnityEngine;
using UnityEditor;
using TMPro;
using System.IO;
using UnityEngine.TextCore.LowLevel;

public class ApplySpecialEliteTypography : EditorWindow
{
    [MenuItem("Tools/Antigravity/Apply Special Elite Typography")]
    public static void ApplyTypography()
    {
        // 1. Path definitions
        string ttfPath = "Assets/Fonts/SpecialElite-Regular.ttf";
        string sdfFolder = "Assets/TextMesh Pro/Resources/Fonts & Materials";
        string sdfPath = $"{sdfFolder}/SpecialElite SDF.asset";
        string settingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        // 2. Load the TTF Font
        Font ttfFont = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        if (ttfFont == null)
        {
            Debug.LogError($"[Typography] Could not find TTF font at {ttfPath}. Please download it first.");
            return;
        }

        // Ensure target folder exists
        if (!Directory.Exists(sdfFolder))
        {
            Directory.CreateDirectory(sdfFolder);
        }

        // 3. Load or Create the SDF font asset
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(sdfPath);
        if (fontAsset == null)
        {
            Debug.Log("[Typography] Creating SpecialElite SDF font asset...");
            
            // Create font asset programmatically with Dynamic Atlas Population
            fontAsset = TMP_FontAsset.CreateFontAsset(
                ttfFont, 
                90,            // sampling point size
                9,             // padding
                GlyphRenderMode.SDFAA, 
                512,           // atlas width
                512,           // atlas height
                AtlasPopulationMode.Dynamic
            );

            if (fontAsset == null)
            {
                Debug.LogError("[Typography] Failed to create font asset using TMP_FontAsset.CreateFontAsset.");
                return;
            }

            // Save the asset
            AssetDatabase.CreateAsset(fontAsset, sdfPath);

            // Add material and texture as sub-assets to prevent them from getting lost
            if (fontAsset.material != null)
            {
                fontAsset.material.name = "SpecialElite SDF Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }
            if (fontAsset.atlasTexture != null)
            {
                fontAsset.atlasTexture.name = "SpecialElite SDF Atlas";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"[Typography] Created and saved SpecialElite SDF font asset at {sdfPath}");
        }
        else
        {
            Debug.Log($"[Typography] Found existing SpecialElite SDF font asset at {sdfPath}");
        }

        // 4. Update default font asset in TMP Settings
        TMP_Settings settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(settingsPath);
        if (settings != null)
        {
            SerializedObject so = new SerializedObject(settings);
            SerializedProperty defaultFontProp = so.FindProperty("m_defaultFontAsset");
            if (defaultFontProp != null)
            {
                defaultFontProp.objectReferenceValue = fontAsset;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                Debug.Log($"[Typography] Updated default font in TMP Settings to SpecialElite SDF");
            }
            else
            {
                Debug.LogWarning("[Typography] Could not find m_defaultFontAsset property in TMP Settings.");
            }
        }
        else
        {
            Debug.LogWarning($"[Typography] Could not load TMP Settings at {settingsPath}");
        }

        // Keep track of modified objects
        int sceneObjectsModified = 0;
        int prefabObjectsModified = 0;

        // Save current active scene path to restore it later
        string originalScenePath = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;

        // 5. Scan and update all Scenes
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
        foreach (string guid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            if (!ShouldProcessPath(scenePath))
                continue;

            Debug.Log($"[Typography] Scanning scene: {scenePath}");
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);

            TMP_Text[] textComponents = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            bool sceneModified = false;
            foreach (var tmp in textComponents)
            {
                if (tmp.font != fontAsset)
                {
                    Undo.RecordObject(tmp, "Update Font to SpecialElite");
                    tmp.font = fontAsset;
                    EditorUtility.SetDirty(tmp);
                    sceneModified = true;
                    sceneObjectsModified++;
                }
            }

            if (sceneModified)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                Debug.Log($"[Typography] Saved changes to scene: {scenePath}");
            }
        }

        // Restore original scene
        if (!string.IsNullOrEmpty(originalScenePath) && File.Exists(originalScenePath))
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(originalScenePath);
        }

        // 6. Scan and update all Prefabs
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!ShouldProcessPath(prefabPath))
                continue;

            GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabRoot == null)
                continue;

            TMP_Text[] textComponents = prefabRoot.GetComponentsInChildren<TMP_Text>(true);
            bool prefabModified = false;

            foreach (var tmp in textComponents)
            {
                if (tmp.font != fontAsset)
                {
                    Undo.RecordObject(tmp, "Update Font to SpecialElite");
                    tmp.font = fontAsset;
                    EditorUtility.SetDirty(tmp);
                    prefabModified = true;
                    prefabObjectsModified++;
                }
            }

            if (prefabModified)
            {
                PrefabUtility.SavePrefabAsset(prefabRoot);
                Debug.Log($"[Typography] Saved changes to prefab: {prefabPath}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Typography Applied", 
            $"Successfully generated SpecialElite SDF and applied it to:\n- {sceneObjectsModified} text components in scenes\n- {prefabObjectsModified} text components in prefabs\n\nProject-wide defaults updated.", 
            "OK"
        );

        Debug.Log($"[Typography] Finished applying Special Elite. Modified {sceneObjectsModified} scene components and {prefabObjectsModified} prefab components.");
    }

    private static bool ShouldProcessPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        
        string normalized = path.Replace('\\', '/');
        if (normalized.Contains("/_Recovery/") || normalized.Contains("/TextMesh Pro/") || normalized.Contains("/Packages/"))
        {
            return false;
        }
        return true;
    }
}
