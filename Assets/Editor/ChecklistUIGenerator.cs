using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class ChecklistUIGenerator : EditorWindow
{
    [MenuItem("Tools/Generar Checklist UI (PREMIUM)")]
    public static void GenerateUI()
    {
        // 1. Encontrar o crear Canvas
        Canvas canvas = null;
        if (Selection.activeGameObject != null)
            canvas = Selection.activeGameObject.GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindFirstObjectByType<Canvas>();
            
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
            
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        // 1.5 Destruir cualquier panel anterior para evitar duplicados y bugs de Singleton
        Transform oldPanel = canvas.transform.Find("Panel_Checklist");
        while (oldPanel != null)
        {
            DestroyImmediate(oldPanel.gameObject);
            oldPanel = canvas.transform.Find("Panel_Checklist");
        }

        // 2. Crear Panel Principal (Estilo Cristal oscuro/Minimalista)
        GameObject panelObj = new GameObject("Panel_Checklist", typeof(RectTransform));
        panelObj.transform.SetParent(canvas.transform, false);
        
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.localScale = Vector3.one;
        panelRect.localRotation = Quaternion.identity;
        panelRect.anchoredPosition3D = new Vector3(-30, -30, 0); 
        panelRect.anchorMin = new Vector2(1, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(1, 1);
        panelRect.sizeDelta = new Vector2(330, 270); // Un poco más amplio

        Image panelImg = panelObj.AddComponent<Image>();
        panelImg.color = new Color(0.06f, 0.06f, 0.08f, 0.92f); // Casi negro, sutilmente azulado

        // Outline dorado sutil para darle profundidad y elegancia
        Outline outline = panelObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.8f, 0.7f, 0.2f, 0.3f);
        outline.effectDistance = new Vector2(1, -1);

        VerticalLayoutGroup layout = panelObj.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(25, 25, 25, 25); // Más respiro interno
        layout.spacing = 15;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        // 3. Contenedor del Título + Separador
        GameObject headerObj = new GameObject("Header", typeof(RectTransform));
        headerObj.transform.SetParent(panelObj.transform, false);
        RectTransform headerRect = headerObj.GetComponent<RectTransform>();
        headerRect.localScale = Vector3.one;
        headerRect.localRotation = Quaternion.identity;
        headerRect.anchoredPosition3D = Vector3.zero;
        
        VerticalLayoutGroup headerLayout = headerObj.AddComponent<VerticalLayoutGroup>();
        headerLayout.spacing = 10;
        headerLayout.childControlHeight = true;
        headerLayout.childControlWidth = true;
        headerLayout.childForceExpandHeight = false;
        headerLayout.childForceExpandWidth = true;

        // Título con espaciado de letras
        GameObject titleObj = new GameObject("Titulo", typeof(RectTransform));
        titleObj.transform.SetParent(headerObj.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.localScale = Vector3.one;
        titleRect.localRotation = Quaternion.identity;
        titleRect.anchoredPosition3D = Vector3.zero;

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "OBJETIVOS";
        titleText.fontSize = 22;
        titleText.fontStyle = FontStyles.Bold;
        titleText.characterSpacing = 8; // Efecto espaciado muy moderno
        titleText.color = new Color(0.95f, 0.85f, 0.3f); // Dorado brillante
        titleText.alignment = TextAlignmentOptions.Center;
        
        Shadow titleShadow = titleObj.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(0, 0, 0, 0.7f);
        titleShadow.effectDistance = new Vector2(1, -1);

        // Separador (Línea delgada bajo el título)
        GameObject separatorObj = new GameObject("Separator", typeof(RectTransform));
        separatorObj.transform.SetParent(headerObj.transform, false);
        RectTransform sepRect = separatorObj.GetComponent<RectTransform>();
        sepRect.localScale = Vector3.one;
        sepRect.localRotation = Quaternion.identity;
        sepRect.anchoredPosition3D = Vector3.zero;
        
        Image sepImg = separatorObj.AddComponent<Image>();
        sepImg.color = new Color(1f, 1f, 1f, 0.15f);
        LayoutElement sepLayout = separatorObj.AddComponent<LayoutElement>();
        sepLayout.minHeight = 2; // Línea de 2px

        // 4. Configurar las Tareas (Tasks)
        TaskManager taskManager = panelObj.AddComponent<TaskManager>();
        taskManager.tasks = new TaskManager.TaskItem[4];

        string[] taskIds = { "cuadro", "llave", "caja", "libros" };
        string[] taskLabels = { "Reacomodar el cuadro", "Obtener la llave", "Abrir la caja fuerte", "Recoger los libros" };

        for (int i = 0; i < 4; i++)
        {
            GameObject taskRow = new GameObject("Task_" + taskIds[i], typeof(RectTransform));
            taskRow.transform.SetParent(panelObj.transform, false);
            RectTransform rowRect = taskRow.GetComponent<RectTransform>();
            rowRect.localScale = Vector3.one;
            rowRect.localRotation = Quaternion.identity;
            rowRect.anchoredPosition3D = Vector3.zero;
            
            HorizontalLayoutGroup rowLayout = taskRow.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 15; // Más espacio entre la caja y el texto
            rowLayout.childControlHeight = true;
            rowLayout.childControlWidth = false;
            rowLayout.childForceExpandHeight = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;

            // Contenedor del Icono (Para mantener la caja vacía y el check en el mismo lugar)
            GameObject iconContainer = new GameObject("IconContainer", typeof(RectTransform));
            iconContainer.transform.SetParent(taskRow.transform, false);
            RectTransform iconRect = iconContainer.GetComponent<RectTransform>();
            iconRect.localScale = Vector3.one;
            iconRect.localRotation = Quaternion.identity;
            iconRect.anchoredPosition3D = Vector3.zero;
            
            LayoutElement iconLayout = iconContainer.AddComponent<LayoutElement>();
            iconLayout.minWidth = 22;
            iconLayout.minHeight = 22;

            // La caja vacía de fondo
            GameObject boxObj = new GameObject("Box", typeof(RectTransform));
            boxObj.transform.SetParent(iconContainer.transform, false);
            RectTransform boxRect = boxObj.GetComponent<RectTransform>();
            boxRect.localScale = Vector3.one;
            boxRect.localRotation = Quaternion.identity;
            boxRect.anchoredPosition3D = Vector3.zero;
            boxRect.anchorMin = Vector2.zero;
            boxRect.anchorMax = Vector2.one;
            boxRect.sizeDelta = Vector2.zero;

            Image boxImg = boxObj.AddComponent<Image>();
            boxImg.color = new Color(1, 1, 1, 0.05f); // Muy tenue
            Outline boxOutline = boxObj.AddComponent<Outline>();
            boxOutline.effectColor = new Color(1, 1, 1, 0.4f); // Borde de la caja
            boxOutline.effectDistance = new Vector2(1, -1);

            // Contenedor del Checkmark (Rotado -45 grados para formar la paloma perfecta)
            GameObject checkObj = new GameObject("CheckmarkRoot", typeof(RectTransform));
            checkObj.transform.SetParent(iconContainer.transform, false);
            RectTransform checkRect = checkObj.GetComponent<RectTransform>();
            checkRect.localScale = Vector3.one;
            checkRect.localRotation = Quaternion.Euler(0, 0, -45);
            checkRect.anchoredPosition3D = new Vector3(-2, 3, 0); // Ajuste fino al centro de la caja
            checkRect.sizeDelta = new Vector2(16, 16);

            // Pierna corta de la paloma
            GameObject shortLeg = new GameObject("ShortLeg", typeof(RectTransform), typeof(Image));
            shortLeg.transform.SetParent(checkObj.transform, false);
            RectTransform shortRect = shortLeg.GetComponent<RectTransform>();
            shortRect.pivot = Vector2.zero;
            shortRect.anchorMin = Vector2.zero;
            shortRect.anchorMax = Vector2.zero;
            shortRect.sizeDelta = new Vector2(9, 3.5f);
            shortRect.anchoredPosition = Vector2.zero;
            shortLeg.GetComponent<Image>().color = new Color(0.2f, 0.9f, 0.4f);

            // Pierna larga de la paloma
            GameObject longLeg = new GameObject("LongLeg", typeof(RectTransform), typeof(Image));
            longLeg.transform.SetParent(checkObj.transform, false);
            RectTransform longRect = longLeg.GetComponent<RectTransform>();
            longRect.pivot = Vector2.zero;
            longRect.anchorMin = Vector2.zero;
            longRect.anchorMax = Vector2.zero;
            longRect.sizeDelta = new Vector2(3.5f, 18);
            longRect.anchoredPosition = new Vector2(5.5f, 0); // Se alinea perfectamente con la pierna corta
            longLeg.GetComponent<Image>().color = new Color(0.2f, 0.9f, 0.4f);

            // Texto de la tarea
            GameObject textObj = new GameObject("Texto", typeof(RectTransform));
            textObj.transform.SetParent(taskRow.transform, false);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.localScale = Vector3.one;
            textRect.localRotation = Quaternion.identity;
            textRect.anchoredPosition3D = Vector3.zero;

            TextMeshProUGUI textTMP = textObj.AddComponent<TextMeshProUGUI>();
            textTMP.text = taskLabels[i];
            textTMP.fontSize = 17;
            textTMP.color = new Color(0.95f, 0.95f, 0.95f, 1f); // Blanco crudo suave
            textTMP.textWrappingMode = TextWrappingModes.Normal;
            
            LayoutElement textLayout = textObj.AddComponent<LayoutElement>();
            textLayout.flexibleWidth = 1;

            taskManager.tasks[i] = new TaskManager.TaskItem
            {
                id = taskIds[i],
                checkmarkRoot = checkObj.transform, // Animamos el contenedor de la paloma
                textComponent = textTMP
            };
        }

        Selection.activeGameObject = panelObj;
        Debug.Log("¡Checklist UI PREMIUM y Animado generado exitosamente!");
    }
}
