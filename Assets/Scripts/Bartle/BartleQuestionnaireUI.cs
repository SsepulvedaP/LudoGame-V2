using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityStandardAssets.Characters.FirstPerson;

/// <summary>
/// Flujo Bartle: GET survey → 8 respuestas vía POST → GET summary.
/// Asigna en el inspector textos, botones (uno por opción; típicamente 4) y URLs opcionales.
/// </summary>
public class BartleQuestionnaireUI : MonoBehaviour
{
    [Header("API")]
    [Tooltip("Misma base que UserRegistrationClient, ej. https://....../api")]
    public string baseUrl = "https://ludo-api-48780a3730ba.herokuapp.com/api";

    [Header("UI")]
    public TMP_Text promptText;
    public TMP_Text progressText;
    public Button[] optionButtons;
    public TMP_Text statusOrErrorText;

    [Header("Resultado")]
    public GameObject summaryPanel;
    public TMP_Text summaryTitleText;
    public TMP_Text summaryDetailsText;

    [Header("Flujo de escenas")]
    [Tooltip("Solo si el Bartle NO está en la misma escena que el juego: escena al pulsar Continuar. Si el Bartle va dentro de Level 1, déjalo vacío y solo se cierra el panel.")]
    public string nextSceneAfterBartle = "";

    [Header("Opcional")]
    [Tooltip("Botón 'Continuar' en el panel de resultado. Si usas BartleQuestionnaireRuntimeLayout, se crea y enlaza solo en Play (no hace falta arrastrar nada en edición).")]
    public Button closeOrContinueButton;

    [Header("Cuándo empezar")]
    [Tooltip("Si está desactivado, no carga el survey hasta que otro script llame BeginQuestionnaire() (p. ej. tras el puzzle en Level 1).")]
    public bool beginAutomaticallyOnStart = false;

    [Header("Jugador FPS durante el cuestionario")]
    [Tooltip("Asigna el First Person Controller del jugador. Se desactiva mientras el cuestionario está abierto para poder usar el ratón en la UI.")]
    public FirstPersonController firstPersonWhileQuestions;

    [Tooltip("Si firstPersonWhileQuestions está vacío, intenta encontrarlo en la escena al empezar (solo uno).")]
    public bool autoFindFirstPersonController = true;

    private List<BartleQuestionOut> _orderedQuestions = new List<BartleQuestionOut>();
    private int _currentIndex;
    private bool _busy;
    private bool _surveyStarted;

    private void Awake()
    {
        if (summaryPanel != null)
        {
            summaryPanel.SetActive(false);
        }

        WireInteractionBindings();
    }

    private void Start()
    {
        // Segunda pasada: si RuntimeLayout asignó referencias tras Awake o el orden de scripts varió.
        WireInteractionBindings();

        if (beginAutomaticallyOnStart)
        {
            BeginQuestionnaire();
        }
    }

    /// <summary>
    /// Conecta listeners al botón Continuar y a las opciones. Llama al generar UI en runtime o si montaste la UI a mano.
    /// </summary>
    public void RefreshInteractionBindings()
    {
        WireInteractionBindings();
    }

    private void WireInteractionBindings()
    {
        if (closeOrContinueButton != null)
        {
            closeOrContinueButton.onClick.RemoveListener(OnCloseOrContinueClicked);
            closeOrContinueButton.onClick.AddListener(OnCloseOrContinueClicked);
        }

        int n = optionButtons != null ? optionButtons.Length : 0;
        for (int i = 0; i < n; i++)
        {
            int captured = i;
            if (optionButtons[i] != null)
            {
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => OnOptionClicked(captured));
            }
        }
    }

    /// <summary>
    /// Inicia GET survey y el flujo de preguntas. Seguro llamar más de una vez: solo efecto la primera vez.
    /// Activa primero el GameObject del panel si estaba desactivado.
    /// </summary>
    public void BeginQuestionnaire()
    {
        if (_surveyStarted)
        {
            return;
        }

        WireInteractionBindings();

        _surveyStarted = true;
        ResolveFirstPersonReference();
        SetFpsAndCursorForUiBlocking(true);
        StartCoroutine(LoadSurveyAndBegin());
    }

    private void ResolveFirstPersonReference()
    {
        if (firstPersonWhileQuestions != null || !autoFindFirstPersonController)
        {
            return;
        }

        firstPersonWhileQuestions = FindFirstObjectByType<FirstPersonController>();
    }

    /// <summary>
    /// Pausa movimiento/mira FPS y deja el cursor libre para la UI, o restaura al terminar.
    /// </summary>
    private void SetFpsAndCursorForUiBlocking(bool questionnaireBlocksFps)
    {
        if (firstPersonWhileQuestions != null)
        {
            firstPersonWhileQuestions.enabled = !questionnaireBlocksFps;
        }

        if (questionnaireBlocksFps)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (firstPersonWhileQuestions != null && firstPersonWhileQuestions.m_MouseLook != null)
            {
                firstPersonWhileQuestions.m_MouseLook.SetCursorLock(false);
            }
        }
        else if (firstPersonWhileQuestions != null && firstPersonWhileQuestions.m_MouseLook != null)
        {
            firstPersonWhileQuestions.m_MouseLook.SetCursorLock(true);
        }
    }

    private IEnumerator LoadSurveyAndBegin()
    {
        if (!UserSession.TryGetUserId(out _))
        {
            SetError("No hay user_id guardado. Regístrate primero o asigna UserSession.Save tras el login.");
            yield break;
        }

        string url = $"{TrimApiBase(baseUrl)}/questions/bartle/survey";
        using UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            SetError($"Survey HTTP {request.responseCode}: {request.error}");
            yield break;
        }

        BartleSurveyOut survey = JsonUtility.FromJson<BartleSurveyOut>(request.downloadHandler.text);
        if (survey?.questions == null || survey.questions.Length == 0)
        {
            SetError("El servidor no devolvió preguntas Bartle.");
            yield break;
        }

        _orderedQuestions = survey.questions.OrderBy(q => q.sort_order).ToList();
        _currentIndex = 0;
        RenderCurrentQuestion();
    }

    private void RenderCurrentQuestion()
    {
        if (_currentIndex < 0 || _currentIndex >= _orderedQuestions.Count)
        {
            StartCoroutine(LoadSummary());
            return;
        }

        BartleQuestionOut q = _orderedQuestions[_currentIndex];
        if (promptText != null)
        {
            promptText.text = q.prompt ?? string.Empty;
        }

        if (progressText != null)
        {
            progressText.text = $"{_currentIndex + 1} / {_orderedQuestions.Count}";
        }

        BartleOptionOut[] opts = q.options ?? Array.Empty<BartleOptionOut>();
        int bi = optionButtons != null ? optionButtons.Length : 0;
        for (int i = 0; i < bi; i++)
        {
            bool show = i < opts.Length;
            if (optionButtons[i] != null)
            {
                optionButtons[i].gameObject.SetActive(show);
                if (show)
                {
                    var label = optionButtons[i].GetComponentInChildren<TMP_Text>(true);
                    if (label != null)
                    {
                        label.text = opts[i].label ?? string.Empty;
                    }
                }
            }
        }

        SetStatus(string.Empty);
    }

    private void OnOptionClicked(int buttonIndex)
    {
        if (_busy || _currentIndex < 0 || _currentIndex >= _orderedQuestions.Count)
        {
            return;
        }

        BartleQuestionOut q = _orderedQuestions[_currentIndex];
        BartleOptionOut[] opts = q.options ?? Array.Empty<BartleOptionOut>();
        if (buttonIndex < 0 || buttonIndex >= opts.Length)
        {
            return;
        }

        if (!UserSession.TryGetUserId(out int userId))
        {
            SetError("Sesión de usuario no válida.");
            return;
        }

        StartCoroutine(PostAnswer(userId, q.id, opts[buttonIndex].id));
    }

    private IEnumerator PostAnswer(int userId, int questionId, int optionId)
    {
        _busy = true;
        string url = $"{TrimApiBase(baseUrl)}/questions/bartle/answers";

        var body = new UserBartleAnswerSubmit
        {
            user_id = userId,
            question_id = questionId,
            option_id = optionId
        };

        string json = JsonUtility.ToJson(body);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

        using UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(jsonBytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            SetError($"Respuesta Bartle HTTP {request.responseCode}: {request.error}\n{request.downloadHandler.text}");
            _busy = false;
            yield break;
        }

        _currentIndex++;
        _busy = false;

        if (_currentIndex >= _orderedQuestions.Count)
        {
            yield return StartCoroutine(LoadSummary());
        }
        else
        {
            RenderCurrentQuestion();
        }
    }

    private IEnumerator LoadSummary()
    {
        if (!UserSession.TryGetUserId(out int userId))
        {
            SetError("No hay user_id para el resumen.");
            yield break;
        }

        string url = $"{TrimApiBase(baseUrl)}/questions/bartle/users/{userId}/summary";
        using UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            if (request.responseCode == 404)
            {
                SetError("Resumen no encontrado (404): el usuario podría no existir en el backend.");
            }
            else
            {
                SetError($"Summary HTTP {request.responseCode}: {request.error}\n{request.downloadHandler.text}");
            }

            yield break;
        }

        BartleSummaryOut summary = JsonUtility.FromJson<BartleSummaryOut>(request.downloadHandler.text);
        ShowSummary(summary);
    }

    private void ShowSummary(BartleSummaryOut summary)
    {
        if (summaryPanel != null)
        {
            summaryPanel.SetActive(true);
        }

        if (summaryTitleText != null)
        {
            string dom = string.IsNullOrEmpty(summary.dominant_type) ? "(sin respuestas aún)" : summary.dominant_type;
            summaryTitleText.text = $"Perfil Bartle: {dom}";
        }

        if (summaryDetailsText != null && summary.counts != null)
        {
            BartleCountsOut c = summary.counts;
            summaryDetailsText.text =
                $"Respondidas: {summary.answered_questions}\n" +
                $"Killer: {c.Killer}  Socializer: {c.Socializer}\n" +
                $"Achiever: {c.Achiever}  Explorer: {c.Explorer}";
        }

        if (promptText != null)
        {
            promptText.text = string.Empty;
        }

        if (optionButtons != null)
        {
            foreach (Button b in optionButtons)
            {
                if (b != null)
                {
                    b.gameObject.SetActive(false);
                }
            }
        }
    }

    private void OnCloseOrContinueClicked()
    {
        if (summaryPanel != null)
        {
            summaryPanel.SetActive(false);
        }

        SetFpsAndCursorForUiBlocking(false);

        if (!string.IsNullOrWhiteSpace(nextSceneAfterBartle))
        {
            SceneManager.LoadScene(nextSceneAfterBartle);
            return;
        }

        // Cierra todo el panel del cuestionario y vuelve al juego con FPS activo.
        gameObject.SetActive(false);
    }

    private void SetError(string message)
    {
        SetStatus(message);
        Debug.LogWarning("[BartleQuestionnaireUI] " + message);
    }

    private void SetStatus(string message)
    {
        if (statusOrErrorText != null)
        {
            statusOrErrorText.text = message;
        }
    }

    private static string TrimApiBase(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        return url.TrimEnd('/');
    }
}
