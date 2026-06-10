using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Genera en tiempo de ejecución un Canvas para el Cuestionario de Motivación (3 botones),
/// y rellena los campos de <see cref="MotivationInGameUI"/> si siguen vacíos.
/// Coloca este componente en el mismo GameObject que <see cref="MotivationInGameUI"/> (el que se activa para el puzzle).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MotivationInGameUI))]
[DefaultExecutionOrder(-100)]
public class MotivationQuestionnaireRuntimeLayout : MonoBehaviour
{
    [Tooltip("Si los campos de MotivationInGameUI no están asignados, se crea la jerarquía UI automáticamente.")]
    public bool buildAtRuntimeIfIncomplete = true;

    [Tooltip("Copia la baseUrl desde UserRegistrationClient si existe en la escena (misma API que el registro).")]
    public bool copyBaseUrlFromRegistrationClient = true;

    private void Awake()
    {
        MotivationInGameUI ui = GetComponent<MotivationInGameUI>();

        if (copyBaseUrlFromRegistrationClient)
        {
            UserRegistrationClient reg = FindAnyObjectByType<UserRegistrationClient>();
            if (reg != null && !string.IsNullOrWhiteSpace(reg.baseUrl))
            {
                ui.baseUrl = reg.baseUrl.TrimEnd('/');
            }
        }

        if (!buildAtRuntimeIfIncomplete || IsUiAlreadyWired(ui))
        {
            return;
        }

        EnsureEventSystemExists();
        BuildHierarchy(ui);
    }

    private static bool IsUiAlreadyWired(MotivationInGameUI ui)
    {
        if (ui.questionnairePanel == null || ui.promptText == null || ui.statusText == null)
        {
            return false;
        }

        if (ui.optionButtons == null || ui.optionButtons.Length < 3)
        {
            return false;
        }

        for (int i = 0; i < 3; i++)
        {
            if (ui.optionButtons[i] == null)
            {
                return false;
            }
        }

        return true;
    }

    private void BuildHierarchy(MotivationInGameUI ui)
    {
        Transform root = transform;

        // 1. Crear el Canvas principal para el cuestionario
        GameObject canvasGo = new GameObject("MotivationCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(root, false);
        StretchFull(canvasGo.GetComponent<RectTransform>());

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 101; // Un sorting order ligeramente diferente si es necesario

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // 2. Panel principal de fondo (Oscuro semitransparente)
        GameObject mainPanel = CreateUiPanel("MainPanel", canvasGo.transform, new Color(0.07f, 0.07f, 0.09f, 0.94f));

        // 3. Texto de la pregunta (Prompt)
        TMP_Text prompt = CreateTmp("PromptText", mainPanel.transform, 32, TextAlignmentOptions.TopLeft,
            new Vector2(80, -140), new Vector2(-80, -320));
        prompt.textWrappingMode = TextWrappingModes.Normal;

        // 4. Texto de estado/error (Status)
        TMP_Text status = CreateTmp("StatusText", mainPanel.transform, 18, TextAlignmentOptions.Bottom,
            new Vector2(80, 40), new Vector2(-80, 120));
        status.color = new Color(1f, 0.55f, 0.45f, 1f);
        status.fontStyle = FontStyles.Italic;

        // 5. Host de los botones de opciones (Layout vertical)
        GameObject optionsHost = new GameObject("Options", typeof(RectTransform), typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter));
        RectTransform optRt = optionsHost.GetComponent<RectTransform>();
        optRt.SetParent(mainPanel.transform, false);
        optRt.anchorMin = new Vector2(0.5f, 0.35f);
        optRt.anchorMax = new Vector2(0.5f, 0.35f);
        optRt.pivot = new Vector2(0.5f, 0.5f);
        optRt.sizeDelta = new Vector2(760, 300);
        optRt.anchoredPosition = new Vector2(0, -40);

        VerticalLayoutGroup vlg = optionsHost.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 12f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childForceExpandWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter fitter = optionsHost.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 6. Crear los 3 botones para las opciones A, B, C de motivación
        var buttons = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            buttons[i] = CreateOptionButton(optionsHost.transform, $"Opción {i + 1}", i);
        }

        // 7. Enlazar variables a MotivationInGameUI
        ui.questionnairePanel = mainPanel;
        ui.promptText = prompt;
        ui.statusText = status;
        ui.optionButtons = buttons;

        // Desactivamos el panel completo por defecto (el script se encargará de mostrarlo)
        mainPanel.SetActive(false);
    }

    private static void EnsureEventSystemExists()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    private static GameObject CreateUiPanel(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        StretchFull(go.GetComponent<RectTransform>());
        go.GetComponent<Image>().color = color;
        go.GetComponent<Image>().raycastTarget = true;
        return go;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    private static TMP_Text CreateTmp(string name, Transform parent, float fontSize, TextAlignmentOptions align,
        Vector2 anchoredMinOffset, Vector2 anchoredMaxOffset)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = anchoredMinOffset;
        rt.offsetMax = anchoredMaxOffset;

        TMP_Text tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = string.Empty;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        // Load Special Elite font asset from Resources if available
        TMP_FontAsset specialElite = Resources.Load<TMP_FontAsset>("Fonts & Materials/SpecialElite SDF");
        if (specialElite != null)
        {
            tmp.font = specialElite;
        }

        return tmp;
    }

    private static Button CreateOptionButton(Transform parent, string placeholderLabel, int index)
    {
        GameObject go = new GameObject($"Option_{index}", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(0f, 72f);

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minHeight = 72f;
        le.preferredHeight = 72f;
        le.flexibleWidth = 1f;

        Image img = go.GetComponent<Image>();
        img.color = new Color(0.22f, 0.24f, 0.32f, 1f);

        Button btn = go.GetComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(0.32f, 0.36f, 0.48f, 1f);
        colors.pressedColor = new Color(0.2f, 0.22f, 0.3f, 1f);
        btn.colors = colors;

        GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        StretchFull(textGo.GetComponent<RectTransform>());
        textGo.transform.SetParent(go.transform, false);

        TMP_Text tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.text = placeholderLabel;
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = Color.white;
        tmp.margin = new Vector4(24f, 8f, 24f, 8f);
        tmp.raycastTarget = false;

        // Load Special Elite font asset from Resources if available
        TMP_FontAsset specialElite = Resources.Load<TMP_FontAsset>("Fonts & Materials/SpecialElite SDF");
        if (specialElite != null)
        {
            tmp.font = specialElite;
        }

        return btn;
    }
}
