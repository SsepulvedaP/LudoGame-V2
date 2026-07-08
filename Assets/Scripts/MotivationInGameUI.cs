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
            firstPersonController = FindAnyObjectByType<FirstPersonController>();
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
    /// Proxy a GlobalQuizManager
    /// </summary>
    public void ShowQuestionSimple(int questionId)
    {
        if (GlobalQuizManager.Instance != null) GlobalQuizManager.Instance.ShowNextChunk();
    }

    public void ShowQuestionsSimpleString(string commaSeparatedIds)
    {
        if (GlobalQuizManager.Instance != null) GlobalQuizManager.Instance.ShowNextChunk();
    }

    public void ShowQuestions(int[] questionIds, Action onComplete = null)
    {
        if (GlobalQuizManager.Instance != null) 
        {
            GlobalQuizManager.Instance.ShowNextChunk(onComplete);
        }
        else 
        {
            onComplete?.Invoke();
        }
    }

    public void ShowQuestion(int questionId, Action onComplete = null)
    {
        if (GlobalQuizManager.Instance != null) 
        {
            GlobalQuizManager.Instance.ShowNextChunk(onComplete);
        }
        else 
        {
            onComplete?.Invoke();
        }
    }

    private void RenderQuestion(MotivationQuestionOut q)
    {
        // Ya no se usa
    }
    // Ya no se necesitan los métodos de renderizado y envío de datos
    // porque todo se maneja desde GlobalQuizManager.


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
