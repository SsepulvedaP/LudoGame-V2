using UnityEngine;
using UnityEditor;

public class LightingFixerV2 : EditorWindow
{
    [MenuItem("Tools/Apply Final Lighting Fixes (Blink & Unlock)")]
    public static void FixLighting()
    {
        // 1. Hacer que la luz del puzzle parpadee
        GameObject puzzleLightObj = GameObject.Find("PuzzleLight");
        if (puzzleLightObj != null)
        {
            // Remover la luz estática (y sus dependencias de URP si existen)
            Light staticLight = puzzleLightObj.GetComponent<Light>();
            if (staticLight != null)
            {
                // Deseleccionar antes de destruir para evitar MissingReferenceException en el Inspector
                Selection.activeObject = null;
                EditorUtility.SetDirty(puzzleLightObj);

                Component urpData = puzzleLightObj.GetComponent("UniversalAdditionalLightData");
                if (urpData != null)
                {
                    Undo.DestroyObjectImmediate(urpData);
                }
                Undo.DestroyObjectImmediate(staticLight);
            }
            
            // Añadir el script que la hace parpadear
            InteractableHighlighter highlighter = puzzleLightObj.GetComponent<InteractableHighlighter>();
            if (highlighter == null)
            {
                highlighter = Undo.AddComponent<InteractableHighlighter>(puzzleLightObj);
                highlighter.lightColor = new Color(1f, 0.9f, 0.6f);
                highlighter.maxIntensity = 5f;
                highlighter.maxEmission = 0f; // No hay material, solo luz
                highlighter.enableMaterialEmission = false;
            }
            Debug.Log("PuzzleLight actualizada para parpadear.");
        }

        // 2. Desbloquear la luz del locker
        Light[] allLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        int extractedCount = 0;
        GameObject lastExtracted = null;
        
        // Tratar de encontrar el Keypad para buscar la luz más cercana
        GameObject keypadObj = GameObject.Find("Keypad Variant");
        if (keypadObj == null) 
        {
            MonoBehaviour[] scripts = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach(var s in scripts) {
                if (s.GetType().Name == "Keypad") keypadObj = s.gameObject;
            }
        }

        if (keypadObj != null)
        {
            Light closestLight = null;
            float minDistance = 3f; // Distancia máxima para considerarla la luz del locker
            
            foreach (var light in allLights)
            {
                // Ignorar la luz direccional y la luz del puzzle
                if (light.type == LightType.Directional || light.gameObject.name == "PuzzleLight" || light.gameObject.name == "Free_Locker_Light") 
                    continue;
                
                float dist = Vector3.Distance(light.transform.position, keypadObj.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestLight = light;
                }
            }
            
            if (closestLight != null)
            {
                // Si la luz está en un prefab, la duplicamos, si no, simplemente la usamos
                GameObject freeLightObj;
                if (PrefabUtility.IsPartOfAnyPrefab(closestLight.gameObject))
                {
                    freeLightObj = Instantiate(closestLight.gameObject);
                    Undo.RegisterCreatedObjectUndo(freeLightObj, "Extraer luz del locker");
                    Undo.RecordObject(closestLight.gameObject, "Desactivar luz original");
                    closestLight.gameObject.SetActive(false);
                }
                else
                {
                    freeLightObj = closestLight.gameObject;
                    Undo.RecordObject(freeLightObj.transform, "Ajustar luz del locker");
                }

                freeLightObj.name = "Free_Locker_Light";
                
                // Posicionarla un poco hacia arriba y hacia atrás/adelante del keypad
                freeLightObj.transform.position = keypadObj.transform.position + Vector3.up * 0.5f + keypadObj.transform.forward * 0.6f;
                freeLightObj.transform.LookAt(keypadObj.transform);
                
                freeLightObj.transform.SetParent(null); // Mover a la raíz
                
                lastExtracted = freeLightObj;
                extractedCount++;
            }

            // 3. Desactivar la luz automática del locker (ya que el usuario clonó la manual)
            InteractableHighlighter lockerHighlighter = keypadObj.GetComponentInParent<InteractableHighlighter>();
            if (lockerHighlighter != null)
            {
                Undo.RecordObject(lockerHighlighter, "Desactivar luz por defecto del locker");
                lockerHighlighter.enableLight = false;
                lockerHighlighter.enableMaterialEmission = false;
                Debug.Log("Se desactivó la generación de luz por defecto en el Locker.");
            }
        }
        
        // Seleccionar la luz del locker para que el usuario la vea de inmediato
        if (lastExtracted != null)
        {
            Selection.activeGameObject = lastExtracted;
        }

        EditorUtility.DisplayDialog("Ajustes Aplicados", 
            $"1. La luz del puzzle ahora parpadeará (lo verás al darle Play).\n" +
            $"2. Se extrajo la luz del locker y ahora se llama 'Free_Locker_Light'. " +
            $"Ya puedes moverla y ajustarla libremente.", 
            "OK");
    }
}
