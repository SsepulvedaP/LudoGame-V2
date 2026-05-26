using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityStandardAssets.Characters.FirstPerson;

// Clases de datos para parsear el JSON
[Serializable]
public class MotivationSurveyOut
{
    public MotivationQuestionOut[] questions;
}

[Serializable]
public class MotivationQuestionOut
{
    public int id;
    public string prompt;
    public int sortOrder;
    public MotivationOptionOut[] options;
}

[Serializable]
public class MotivationOptionOut
{
    public int id;
    public string optionLetter;
    public string label;
}

[Serializable]
public class MotivationAnswerSubmit
{
    public int user_id;
    public int question_id;
    public int option_id;
}

public class MotivationInGameUI : MonoBehaviour
{
    public static MotivationInGameUI Instance { get; private set; }

    [Header("API")]
    public string baseUrl = "https://ludo-api-48780a3730ba.herokuapp.com/api";

    [Header("UI (Panel a mostrar)")]
    public GameObject questionnairePanel; // El panel principal que contiene los textos y botones
    public TMP_Text promptText;
    public Button[] optionButtons;
    public TMP_Text statusText;

    [Header("Jugador FPS")]
    public FirstPersonController firstPersonController;

    private List<MotivationQuestionOut> _cachedQuestions = new List<MotivationQuestionOut>();
    private bool _isSurveyLoaded = false;
    private bool _isBusy = false;
    private int _currentQuestionIdToShow = -1;
    
    // Callback para reanudar lógica después de responder
    private Action _onQuestionAnswered;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (questionnairePanel != null)
        {
            questionnairePanel.SetActive(false); // Ocultar por defecto
        }

        // Buscar el FPSController si no está asignado
        if (firstPersonController == null)
        {
            firstPersonController = FindFirstObjectByType<FirstPersonController>();
        }
    }

    private void Start()
    {
        // Cargar las preguntas en segundo plano al iniciar el juego
        StartCoroutine(LoadSurveyInBackground());
    }

    private IEnumerator LoadSurveyInBackground()
    {
        string url = $"{baseUrl.TrimEnd('/')}/motivation/survey";
        using UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            MotivationSurveyOut survey = JsonUtility.FromJson<MotivationSurveyOut>(request.downloadHandler.text);
            if (survey != null && survey.questions != null)
            {
                _cachedQuestions = survey.questions.ToList();
                _isSurveyLoaded = true;
            }
        }
        else
        {
            Debug.LogError($"[Motivation API] Error cargando survey: {request.error}");
        }
    }

    /// <summary>
    /// Método simplificado para poder llamarlo desde UnityEvents en el Inspector (ej. OnAccessGranted del Keypad)
    /// </summary>
    public void ShowQuestionSimple(int questionId)
    {
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }
        
        // Esperamos 1.5 segundos para que el Keypad cambie a verde y la cámara salga del zoom
        StartCoroutine(WaitAndShowSimple(questionId));
    }

    private IEnumerator WaitAndShowSimple(int questionId)
    {
        yield return new WaitForSeconds(1.5f);
        ShowQuestion(questionId, null);
    }

    /// <summary>
    /// Muestra una pregunta específica por su ID de Base de Datos.
    /// Congela el tiempo y el FPS Controller.
    /// </summary>
    public void ShowQuestion(int questionId, Action onComplete = null)
    {
        _onQuestionAnswered = onComplete;
        
        if (!_isSurveyLoaded)
        {
            Debug.LogWarning("[Motivation API] Survey no cargado aún. Intentando mostrar de nuevo en 1 segundo...");
            StartCoroutine(WaitAndShowQuestion(questionId));
            return;
        }

        MotivationQuestionOut q = _cachedQuestions.FirstOrDefault(x => x.id == questionId);
        if (q == null)
        {
            Debug.LogError($"[Motivation API] No se encontró la pregunta con ID {questionId}.");
            _onQuestionAnswered?.Invoke();
            return;
        }

        _currentQuestionIdToShow = questionId;

        // Mostrar Panel y pausar juego
        SetGamePaused(true);
        RenderQuestion(q);
    }

    private IEnumerator WaitAndShowQuestion(int questionId)
    {
        yield return new WaitForSecondsRealtime(1f);
        ShowQuestion(questionId, _onQuestionAnswered);
    }

    private void RenderQuestion(MotivationQuestionOut q)
    {
        if (questionnairePanel != null) questionnairePanel.SetActive(true);

        if (promptText != null) promptText.text = q.prompt;
        if (statusText != null) statusText.text = "";

        MotivationOptionOut[] opts = q.options ?? Array.Empty<MotivationOptionOut>();
        
        for (int i = 0; i < optionButtons.Length; i++)
        {
            bool show = i < opts.Length;
            if (optionButtons[i] != null)
            {
                optionButtons[i].gameObject.SetActive(show);
                if (show)
                {
                    var label = optionButtons[i].GetComponentInChildren<TMP_Text>(true);
                    if (label != null) label.text = opts[i].label;

                    int capturedIndex = i;
                    int optionId = opts[i].id;
                    
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => OnOptionSelected(q.id, optionId));
                }
            }
        }
    }

    private void OnOptionSelected(int questionId, int optionId)
    {
        if (_isBusy) return;

        // Comprobamos si hay un UserSession global (igual que en Bartle)
        // Usamos reflection o llamadas si no existe explícitamente, pero asumimos que tienes UserSession.TryGetUserId
        int userId = 1; // Fallback
        if (UserSession.TryGetUserId(out int savedId))
        {
            userId = savedId;
        }

        StartCoroutine(PostAnswer(userId, questionId, optionId));
    }

    private IEnumerator PostAnswer(int userId, int questionId, int optionId)
    {
        _isBusy = true;
        if (statusText != null) statusText.text = "Enviando respuesta...";

        string url = $"{baseUrl.TrimEnd('/')}/motivation/answers";

        var body = new MotivationAnswerSubmit
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

        // Usamos SendWebRequest de forma normal porque Time.timeScale = 0 no afecta a UnityWebRequest
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            if (statusText != null) statusText.text = "Error al enviar. Intenta de nuevo.";
            Debug.LogError($"[Motivation API] Error POST: {request.error}\n{request.downloadHandler.text}");
            _isBusy = false;
            yield break;
        }

        // Éxito
        _isBusy = false;
        
        // Ocultar panel y reanudar juego
        if (questionnairePanel != null) questionnairePanel.SetActive(false);
        SetGamePaused(false);

        _onQuestionAnswered?.Invoke();
    }

    private void SetGamePaused(bool pause)
    {
        if (firstPersonController != null)
        {
            firstPersonController.enabled = !pause;
            if (pause)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                if (firstPersonController.m_MouseLook != null)
                    firstPersonController.m_MouseLook.SetCursorLock(false);
            }
            else
            {
                if (firstPersonController.m_MouseLook != null)
                    firstPersonController.m_MouseLook.SetCursorLock(true);
            }
        }

        // Pausar el tiempo del juego para que no avancen animaciones o temporizadores (como la caja cerrándose)
        Time.timeScale = pause ? 0f : 1f;
    }
}
