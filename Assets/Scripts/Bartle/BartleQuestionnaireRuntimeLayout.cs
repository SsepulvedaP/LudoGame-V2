using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Genera en tiempo de ejecución un Canvas con pregunta, progreso, 4 botones, estado y panel de resumen,
/// y rellena los campos de <see cref="BartleQuestionnaireUI"/> si siguen vacíos.
/// Coloca este componente en el mismo GameObject que <see cref="BartleQuestionnaireUI"/> (el que GameControl activa tras el puzzle).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BartleQuestionnaireUI))]
[DefaultExecutionOrder(-100)]
public class BartleQuestionnaireRuntimeLayout : MonoBehaviour
{
    [Tooltip("Si los campos de BartleQuestionnaireUI no están asignados, se crea la jerarquía UI automáticamente.")]
    public bool buildAtRuntimeIfIncomplete = true;

    [Tooltip("Copia la baseUrl desde UserRegistrationClient si existe en la escena (misma API que el registro).")]
    public bool copyBaseUrlFromRegistrationClient = true;

    private void Awake()
    {
        BartleQuestionnaireUI ui = GetComponent<BartleQuestionnaireUI>();

        if (copyBaseUrlFromRegistrationClient)
        {
            UserRegistrationClient reg = FindFirstObjectByType<UserRegistrationClient>();
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

    private static bool IsUiAlreadyWired(BartleQuestionnaireUI ui)
    {
        if (ui.promptText == null || ui.progressText == null || ui.statusOrErrorText == null)
        {
            return false;
        }

        if (ui.optionButtons == null || ui.optionButtons.Length < 4)
        {
            return false;
        }

        for (int i = 0; i < 4; i++)
        {
            if (ui.optionButtons[i] == null)
            {
                return false;
            }
        }

        return ui.summaryPanel != null && ui.summaryTitleText != null && ui.summaryDetailsText != null &&
               ui.closeOrContinueButton != null;
    }

    private void BuildHierarchy(BartleQuestionnaireUI ui)
    {
        Transform root = transform;

        GameObject canvasGo = new GameObject("BartleCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(root, false);
        StretchFull(canvasGo.GetComponent<RectTransform>());

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject mainPanel = CreateUiPanel("MainPanel", canvasGo.transform, new Color(0.07f, 0.07f, 0.09f, 0.94f));

        TMP_Text prompt = CreateTmp("PromptText", mainPanel.transform, 32, TextAlignmentOptions.TopLeft,
            new Vector2(80, -140), new Vector2(-80, -320));
        prompt.enableWordWrapping = true;

        TMP_Text progress = CreateTmp("ProgressText", mainPanel.transform, 22, TextAlignmentOptions.TopRight,
            new Vector2(80, -80), new Vector2(-80, -130));

        TMP_Text status = CreateTmp("StatusText", mainPanel.transform, 18, TextAlignmentOptions.Bottom,
            new Vector2(80, 40), new Vector2(-80, 120));
        status.color = new Color(1f, 0.55f, 0.45f, 1f);
        status.fontStyle = FontStyles.Italic;

        GameObject optionsHost = new GameObject("Options", typeof(RectTransform), typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter));
        RectTransform optRt = optionsHost.GetComponent<RectTransform>();
        optRt.SetParent(mainPanel.transform, false);
        optRt.anchorMin = new Vector2(0.5f, 0.35f);
        optRt.anchorMax = new Vector2(0.5f, 0.35f);
        optRt.pivot = new Vector2(0.5f, 0.5f);
        optRt.sizeDelta = new Vector2(760, 400);
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

        var buttons = new Button[4];
        for (int i = 0; i < 4; i++)
        {
            buttons[i] = CreateOptionButton(optionsHost.transform, $"Opción {i + 1}", i);
        }

        GameObject summaryRoot = CreateUiPanel("SummaryPanel", canvasGo.transform, new Color(0.02f, 0.02f, 0.04f, 0.96f));
        summaryRoot.SetActive(false);

        GameObject box = new GameObject("SummaryBox", typeof(RectTransform), typeof(Image));
        RectTransform boxRt = box.GetComponent<RectTransform>();
        boxRt.SetParent(summaryRoot.transform, false);
        StretchWithPadding(boxRt, 120f);
        box.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.16f, 1f);

        TMP_Text sumTitle = CreateTmpInParent(box.transform, "SummaryTitle", 28, TextAlignmentOptions.Top,
            new Vector2(32, -32), new Vector2(-32, -80));
        TMP_Text sumDetails = CreateTmpInParent(box.transform, "SummaryDetails", 22, TextAlignmentOptions.TopLeft,
            new Vector2(32, -100), new Vector2(-32, -220));
        sumDetails.enableWordWrapping = true;

        Button continueBtn = CreateCenteredButton(box.transform, "Continuar", new Vector2(0, -280));
        RectTransform cRt = continueBtn.GetComponent<RectTransform>();
        cRt.sizeDelta = new Vector2(260, 52);

        ui.promptText = prompt;
        ui.progressText = progress;
        ui.statusOrErrorText = status;
        ui.optionButtons = buttons;
        ui.summaryPanel = summaryRoot;
        ui.summaryTitleText = sumTitle;
        ui.summaryDetailsText = sumDetails;
        ui.closeOrContinueButton = continueBtn;
        ui.RefreshInteractionBindings();
    }

    private static void EnsureEventSystemExists()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
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

    private static void StretchWithPadding(RectTransform rt, float pad)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(pad, pad);
        rt.offsetMax = new Vector2(-pad, -pad);
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
        return tmp;
    }

    private static TMP_Text CreateTmpInParent(Transform parent, string name, float fontSize,
        TextAlignmentOptions align, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        TMP_Text tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = string.Empty;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button CreateOptionButton(Transform parent, string placeholderLabel, int index)
    {
        GameObject go = new GameObject($"Option_{index}", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(0f, 56f);

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minHeight = 56f;
        le.preferredHeight = 56f;
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
        tmp.fontSize = 22;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = Color.white;
        tmp.margin = new Vector4(16f, 8f, 16f, 8f);
        tmp.raycastTarget = false;

        return btn;
    }

    private static Button CreateCenteredButton(Transform parent, string label, Vector2 anchoredPos)
    {
        GameObject go = new GameObject("ContinueButton", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(220f, 48f);

        go.GetComponent<Image>().color = new Color(0.28f, 0.5f, 0.85f, 1f);
        Button btn = go.GetComponent<Button>();

        GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        StretchFull(textGo.GetComponent<RectTransform>());
        textGo.transform.SetParent(go.transform, false);
        TMP_Text tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 22;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        return btn;
    }
}
