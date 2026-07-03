using UnityEngine;
using UnityEditor;

public class LightingFixer : EditorWindow
{
    [MenuItem("Tools/Fix Lighting (Interactables, Locker, Puzzle)")]
    public static void FixLighting()
    {
        // 1. Fix Interactable Highlighters
        InteractableHighlighter[] highlighters = Object.FindObjectsOfType<InteractableHighlighter>();
        int highlighterCount = 0;
        foreach (var highlighter in highlighters)
        {
            Undo.RecordObject(highlighter, "Fix Highlighter");
            highlighter.lightColor = new Color(1f, 0.9f, 0.6f); 
            highlighter.maxIntensity = 5f; 
            highlighter.maxEmission = 0.15f; 
            EditorUtility.SetDirty(highlighter);
            highlighterCount++;
        }
        Debug.Log($"Fixed {highlighterCount} InteractableHighlighter objects in the scene.");

        // 2. Add Light for the Puzzle on the tool board
        // Tool board position: -3.81863, 0.277, 14.9846
        GameObject puzzleLightObj = GameObject.Find("PuzzleLight");
        if (puzzleLightObj == null)
        {
            puzzleLightObj = new GameObject("PuzzleLight");
            Light pLight = puzzleLightObj.AddComponent<Light>();
            pLight.type = LightType.Spot;
            pLight.spotAngle = 60f;
            pLight.intensity = 15f; 
            pLight.range = 5f;
            pLight.color = new Color(1f, 0.95f, 0.9f); // Luz cálida
            
            // Colocar la luz 1.5 unidades arriba de la mesa y apuntando hacia abajo
            puzzleLightObj.transform.position = new Vector3(-3.81863f, 0.277f + 1.5f, 14.9846f);
            puzzleLightObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f); 
            Undo.RegisterCreatedObjectUndo(puzzleLightObj, "Create Puzzle Light");
            Debug.Log("Created PuzzleLight above the tool board.");
        }
        else
        {
            Debug.Log("PuzzleLight already exists.");
        }

        // 3. Fix Locker Light
        // Buscar luces cerca del locker o keypad
        Light[] allLights = Object.FindObjectsOfType<Light>();
        int lockerLightCount = 0;
        foreach (var light in allLights)
        {
            string name = light.gameObject.name.ToLower();
            string parentName = light.transform.parent != null ? light.transform.parent.name.ToLower() : "";
            
            // Si la luz está asociada al locker o keypad
            if (name.Contains("locker") || name.Contains("keypad") || parentName.Contains("locker") || parentName.Contains("keypad"))
            {
                Undo.RecordObject(light, "Fix Locker Light");
                
                // Reducir la intensidad (si es muy alta la limitamos, sino la bajamos a un 30%)
                if (light.intensity > 5f)
                    light.intensity = 2f; 
                else
                    light.intensity *= 0.3f;
                
                EditorUtility.SetDirty(light);
                lockerLightCount++;
            }
        }
        
        Debug.Log($"Fixed {lockerLightCount} Locker/Keypad lights.");
        
        // Mostrar popup al usuario
        EditorUtility.DisplayDialog("Lighting Fixes Applied", 
            $"Se han actualizado:\n- {highlighterCount} objetos interactuables.\n- {lockerLightCount} luces del locker.\n- Luz del puzzle (sobre la mesa) creada/verificada.", 
            "OK");
    }
}
