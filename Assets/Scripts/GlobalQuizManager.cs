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

public class GlobalQuizManager : MonoBehaviour
{
    public static GlobalQuizManager Instance { get; private set; }

    [Header("API")]
    public string baseUrl = "https://ludo-api-48780a3730ba.herokuapp.com/api";

    [Header("Images")]
    public Sprite coronaSprite;
    public Sprite mazmorraSprite;

    // Enums y Clases
    public enum QuestionType { Motivation, Bartle, Memory }

    public class UnifiedQuestion
    {
        public QuestionType Type;
        public int Id; // ID de la API o interno
        public string Prompt;
        public List<UnifiedOption> Options;
        public Action<int> OnAnswered; // Callback al responder
        public Sprite Image; // Opcional
    }

    public class UnifiedOption
    {
        public int Id; // ID de la API o interno
        public string Label;
        public string Letter; // Útil para motivación A, B, C
        public bool IsCorrect; // Útil para memoria
    }

    private List<UnifiedQuestion> _allQuestions = new List<UnifiedQuestion>();
    private List<List<UnifiedQuestion>> _chunks = new List<List<UnifiedQuestion>>();
    private List<UnifiedQuestion> _memoryChunk = new List<UnifiedQuestion>();
    private bool _isMemoryRound = false;
    private Action _onChunkComplete;

    // Puntuaciones locales
    private int _scoreE = 0;
    private int _scoreL = 0;
    private int _scoreA = 0;
    private int _memoryScore = 0;

    // Estado
    private bool _isDataLoaded = false;
    private int _currentChunkIndex = 0;
    private int _currentQuestionInChunk = 0;
    private bool _isBusy = false;

    // UI
    private GameObject _canvasGo;
    private GameObject _mainPanel;
    private TMP_Text _promptText;
    private TMP_Text _progressText;
    private TMP_Text _statusText;
    private Image _questionImage;
    private List<Button> _optionButtons = new List<Button>();
    
    // Summary UI
    private GameObject _summaryPanel;
    private TMP_Text _summaryTitleText;
    private TMP_Text _summaryDetailsText;
    private Button _continueButton;
    private int _summaryState = 0; // 0=Bartle, 1=Motivation, 2=Memory
    private BartleSummaryOut _bartleSummaryData;
    private string _finalDomBartle = "(Sin respuestas)";
    private string _finalDomMot = "Indefinido";
    private string _finalMemLevel = "Estandar";
    private bool _resultsSaved = false;

    private FirstPersonController _fpsController;

    [Serializable]
    public class GeneralResponseSubmit
    {
        public int userId;
        public string response;
        public float averageResponse;
    }

    [Serializable]
    public class VAKResponseSubmit
    {
        public int userId;
        public string vakresponse;
    }

    [Serializable]
    public class MemoryResponseSubmit
    {
        public int userId;
        public string memoryresponse;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _fpsController = FindAnyObjectByType<FirstPersonController>();

        // Crear la UI Unificada en runtime
        BuildUnifiedUI();
    }

    private void Start()
    {
        StartCoroutine(LoadAllData());
    }

    private IEnumerator LoadAllData()
    {
        _statusText.text = "Cargando datos...";
        
        List<UnifiedQuestion> motivationQs = new List<UnifiedQuestion>();
        List<UnifiedQuestion> bartleQs = new List<UnifiedQuestion>();
        List<UnifiedQuestion> memoryQs = GetMemoryQuestions();

        // 1. Fetch Motivation
        string motUrl = $"{baseUrl.TrimEnd('/')}/motivation/survey";
        using (UnityWebRequest req = UnityWebRequest.Get(motUrl))
        {
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                MotivationSurveyOut survey = JsonUtility.FromJson<MotivationSurveyOut>(req.downloadHandler.text);
                if (survey != null && survey.questions != null)
                {
                    foreach (var q in survey.questions.OrderBy(x => x.sortOrder))
                    {
                        var uq = new UnifiedQuestion
                        {
                            Type = QuestionType.Motivation,
                            Id = q.id,
                            Prompt = q.prompt,
                            Options = q.options.Select(o => new UnifiedOption { Id = o.id, Label = o.label, Letter = o.optionLetter }).ToList()
                        };
                        motivationQs.Add(uq);
                    }
                }
            }
            else
            {
                Debug.LogError("[GlobalQuizManager] Error cargando Motivación: " + req.error);
            }
        }

        // 2. Fetch Bartle
        string bartleUrl = $"{baseUrl.TrimEnd('/')}/questions/bartle/survey";
        using (UnityWebRequest req = UnityWebRequest.Get(bartleUrl))
        {
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                BartleSurveyOut survey = JsonUtility.FromJson<BartleSurveyOut>(req.downloadHandler.text);
                if (survey != null && survey.questions != null)
                {
                    foreach (var q in survey.questions.OrderBy(x => x.sort_order))
                    {
                        var uq = new UnifiedQuestion
                        {
                            Type = QuestionType.Bartle,
                            Id = q.id,
                            Prompt = q.prompt,
                            Options = q.options.Select(o => new UnifiedOption { Id = o.id, Label = o.label }).ToList()
                        };
                        
                        // Asignar imágenes
                        if (q.id == 8 || q.prompt.Contains("¿Qué te produce esta imagen?"))
                            uq.Image = mazmorraSprite;
                        if (q.prompt.ToLower().Contains("asocias") && q.prompt.ToLower().Contains("icono"))
                            uq.Image = coronaSprite;

                        bartleQs.Add(uq);
                    }
                }
            }
            else
            {
                Debug.LogError("[GlobalQuizManager] Error cargando Bartle: " + req.error);
            }
        }

        _statusText.text = "";
        DistributeQuestions(motivationQs, bartleQs, memoryQs);
        _isDataLoaded = true;
    }

    private List<UnifiedQuestion> GetMemoryQuestions()
    {
        return new List<UnifiedQuestion>
        {
            new UnifiedQuestion { Type = QuestionType.Memory, Id = 1, Prompt = "Cual fue el codigo que te dieron cuando cuadraste la fotografía?", Options = new List<UnifiedOption> {
                new UnifiedOption { Id=1, Label="1925", IsCorrect=false },
                new UnifiedOption { Id=2, Label="1923", IsCorrect=false },
                new UnifiedOption { Id=3, Label="1922", IsCorrect=true },
                new UnifiedOption { Id=4, Label="1932", IsCorrect=false }
            }},
            new UnifiedQuestion { Type = QuestionType.Memory, Id = 2, Prompt = "Como se llama el juego?", Options = new List<UnifiedOption> {
                new UnifiedOption { Id=1, Label="El taller", IsCorrect=false },
                new UnifiedOption { Id=2, Label="The garage", IsCorrect=true },
                new UnifiedOption { Id=3, Label="El mecánico", IsCorrect=false },
                new UnifiedOption { Id=4, Label="El garaje", IsCorrect=false }
            }},
            new UnifiedQuestion { Type = QuestionType.Memory, Id = 3, Prompt = "Cuántos libros recogiste?", Options = new List<UnifiedOption> {
                new UnifiedOption { Id=1, Label="2", IsCorrect=true },
                new UnifiedOption { Id=2, Label="3", IsCorrect=false },
                new UnifiedOption { Id=3, Label="4", IsCorrect=false },
                new UnifiedOption { Id=4, Label="1", IsCorrect=false }
            }},
            new UnifiedQuestion { Type = QuestionType.Memory, Id = 4, Prompt = "Que pieza faltante tenía la caja de switches?", Options = new List<UnifiedOption> {
                new UnifiedOption { Id=1, Label="Fusible", IsCorrect=false },
                new UnifiedOption { Id=2, Label="Switch", IsCorrect=false },
                new UnifiedOption { Id=3, Label="Medidor de voltaje", IsCorrect=false },
                new UnifiedOption { Id=4, Label="Palanca", IsCorrect=true }
            }},
            new UnifiedQuestion { Type = QuestionType.Memory, Id = 5, Prompt = "Con cuál tecla recoletabas objetos", Options = new List<UnifiedOption> {
                new UnifiedOption { Id=1, Label="Espacio", IsCorrect=false },
                new UnifiedOption { Id=2, Label="Tecla E", IsCorrect=true },
                new UnifiedOption { Id=3, Label="Tecla w", IsCorrect=false },
                new UnifiedOption { Id=4, Label="Con el mouse", IsCorrect=false }
            }},
            new UnifiedQuestion { Type = QuestionType.Memory, Id = 6, Prompt = "De que color es la puerta del garaje?", Options = new List<UnifiedOption> {
                new UnifiedOption { Id=1, Label="Blanca", IsCorrect=true },
                new UnifiedOption { Id=2, Label="Gris", IsCorrect=false },
                new UnifiedOption { Id=3, Label="Negra", IsCorrect=false },
                new UnifiedOption { Id=4, Label="Azul", IsCorrect=false }
            }}
        };
    }

    private void DistributeQuestions(List<UnifiedQuestion> motivation, List<UnifiedQuestion> bartle, List<UnifiedQuestion> memory)
    {
        // Mezclar Motivación y Bartle de forma "intercalada" o aleatoria controlada
        List<UnifiedQuestion> pool = new List<UnifiedQuestion>();
        // Evitar duplicados si la API devuelve dobles
        var motUnique = motivation.GroupBy(x => x.Id).Select(g => g.First());
        var barUnique = bartle.GroupBy(x => x.Id).Select(g => g.First());
        pool.AddRange(motUnique);
        pool.AddRange(barUnique);
        
        // Fisher-Yates shuffle para la mezcla base
        System.Random rnd = new System.Random(); // Seed dinámica para aleatoriedad real
        int n = pool.Count;
        while (n > 1) {  
            n--;  
            int k = rnd.Next(n + 1);  
            var value = pool[k];  
            pool[k] = pool[n];  
            pool[n] = value;  
        }

        // Dividimos en 5 chunks (para los 5 puzzles del juego: cuadro, locker, caja, libros, palanca)
        _chunks.Clear();
        int numChunks = 5;
        for(int i=0; i<numChunks; i++) _chunks.Add(new List<UnifiedQuestion>());

        // Repartir base pool
        int chunkIdx = 0;
        foreach (var q in pool)
        {
            _chunks[chunkIdx].Add(q);
            chunkIdx = (chunkIdx + 1) % numChunks;
        }

        // Las preguntas de memoria van separadas
        _memoryChunk = memory;

        // Mezclar cada chunk internamente
        foreach (var chunk in _chunks)
        {
            int cn = chunk.Count;
            while (cn > 1) {  
                cn--;  
                int k = rnd.Next(cn + 1);  
                var value = chunk[k];  
                chunk[k] = chunk[cn];  
                chunk[cn] = value;  
            }
        }
    }

    public void ShowNextChunk(Action onComplete = null)
    {
        _onChunkComplete = onComplete;
        if (!_isDataLoaded)
        {
            StartCoroutine(WaitAndShowNextChunk());
            return;
        }

        if (_currentChunkIndex >= _chunks.Count)
        {
            _onChunkComplete?.Invoke();
            _onChunkComplete = null;
            return;
        }

        StartCoroutine(ShowChunkWithDelay());
    }

    public void ShowMemoryQuestions(Action onComplete = null)
    {
        _onChunkComplete = onComplete;
        if (!_isDataLoaded)
        {
            StartCoroutine(WaitAndShowMemoryQuestions());
            return;
        }

        _isMemoryRound = true;
        StartCoroutine(ShowChunkWithDelay());
    }

    private IEnumerator WaitAndShowMemoryQuestions()
    {
        yield return new WaitUntil(() => _isDataLoaded);
        ShowMemoryQuestions(_onChunkComplete);
    }

    private IEnumerator ShowChunkWithDelay()
    {
        yield return new WaitForSeconds(1.5f); // Pequeña pausa antes de mostrar el panel

        _currentQuestionInChunk = 0;
        SetGamePaused(true);
        _mainPanel.SetActive(true);
        _canvasGo.SetActive(true);
        ShowCurrentQuestion();
    }

    private IEnumerator WaitAndShowNextChunk()
    {
        yield return new WaitUntil(() => _isDataLoaded);
        ShowNextChunk();
    }

    private void ShowCurrentQuestion()
    {
        var activeList = _isMemoryRound ? _memoryChunk : _chunks[_currentChunkIndex];

        if (_currentQuestionInChunk >= activeList.Count)
        {
            // Bloque terminado
            _mainPanel.SetActive(false);
            _canvasGo.SetActive(false);
            SetGamePaused(false);

            if (!_isMemoryRound)
            {
                _currentChunkIndex++;
            }
            else
            {
                // Fin del juego -> Mostrar Resumen
                StartCoroutine(ShowSummarySequence());
            }

            _onChunkComplete?.Invoke();
            _onChunkComplete = null;
            return;
        }

        var q = activeList[_currentQuestionInChunk];
        _promptText.text = q.Prompt;
        
        string blockName = _isMemoryRound ? "Final" : $"{_currentChunkIndex+1}/5";
        _progressText.text = $"Bloque {blockName} - Pregunta {_currentQuestionInChunk+1}/{activeList.Count}";
        _statusText.text = "";

        // Manejo de Imagen
        if (q.Image != null)
        {
            _questionImage.gameObject.SetActive(true);
            _questionImage.sprite = q.Image;
        }
        else
        {
            _questionImage.gameObject.SetActive(false);
        }

        // Manejo de botones
        for (int i = 0; i < _optionButtons.Count; i++)
        {
            if (i < q.Options.Count)
            {
                _optionButtons[i].gameObject.SetActive(true);
                _optionButtons[i].GetComponentInChildren<TMP_Text>().text = q.Options[i].Label;
                
                int optionIndex = i;
                _optionButtons[i].onClick.RemoveAllListeners();
                _optionButtons[i].onClick.AddListener(() => OnOptionClicked(q, q.Options[optionIndex]));
            }
            else
            {
                _optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnOptionClicked(UnifiedQuestion q, UnifiedOption opt)
    {
        if (_isBusy) return;

        int userId = 1;
        if (UserSession.TryGetUserId(out int savedId)) userId = savedId;

        if (q.Type == QuestionType.Motivation)
        {
            // Evaluar local
            if (opt.Letter == "A") _scoreE++;
            else if (opt.Letter == "B") _scoreL++;
            else if (opt.Letter == "C") _scoreA++;
            
            StartCoroutine(PostMotivationAnswer(userId, q.Id, opt.Id));
        }
        else if (q.Type == QuestionType.Bartle)
        {
            StartCoroutine(PostBartleAnswer(userId, q.Id, opt.Id));
        }
        else if (q.Type == QuestionType.Memory)
        {
            if (opt.IsCorrect) _memoryScore++;
            AdvanceQuestion();
        }
    }

    private IEnumerator PostMotivationAnswer(int userId, int qId, int optId)
    {
        _isBusy = true;
        _statusText.text = "Guardando...";

        string url = $"{baseUrl.TrimEnd('/')}/motivation/answers";
        var body = new MotivationAnswerSubmit { user_id = userId, question_id = qId, option_id = optId };
        
        using (UnityWebRequest req = CreatePostRequest(url, body))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) Debug.LogError("Error POST Motivation: " + req.error);
        }
        
        AdvanceQuestion();
    }

    private IEnumerator PostBartleAnswer(int userId, int qId, int optId)
    {
        _isBusy = true;
        _statusText.text = "Guardando...";

        string url = $"{baseUrl.TrimEnd('/')}/questions/bartle/answers";
        var body = new UserBartleAnswerSubmit { user_id = userId, question_id = qId, option_id = optId };
        
        using (UnityWebRequest req = CreatePostRequest(url, body))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) Debug.LogError("Error POST Bartle: " + req.error);
        }
        
        AdvanceQuestion();
    }

    private void AdvanceQuestion()
    {
        _isBusy = false;
        _currentQuestionInChunk++;
        ShowCurrentQuestion();
    }

    private UnityWebRequest CreatePostRequest(string url, object body)
    {
        string json = JsonUtility.ToJson(body);
        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        return req;
    }

    private IEnumerator PostFinalResults(int userId, string domBartle, string domMot, string memLevel)
    {
        // 1. Guardar Memoria (MemoryResponse)
        string memUrl = $"{baseUrl.TrimEnd('/')}/responses/memory";
        var memBody = new MemoryResponseSubmit { userId = userId, memoryresponse = memLevel };
        using (UnityWebRequest reqMem = CreatePostRequest(memUrl, memBody))
        {
            yield return reqMem.SendWebRequest();
            if (reqMem.result != UnityWebRequest.Result.Success) 
                Debug.LogError("Error POST Memory: " + reqMem.error);
        }

        // 2. Guardar Motivación Ludo+ (VAKResponse)
        string vakUrl = $"{baseUrl.TrimEnd('/')}/responses/vak";
        var vakBody = new VAKResponseSubmit { userId = userId, vakresponse = domMot };
        using (UnityWebRequest reqVak = CreatePostRequest(vakUrl, vakBody))
        {
            yield return reqVak.SendWebRequest();
            if (reqVak.result != UnityWebRequest.Result.Success) 
                Debug.LogError("Error POST VAK (Motivacion): " + reqVak.error);
        }

        // 3. Guardar Perfil Bartle (General Response)
        string resUrl = $"{baseUrl.TrimEnd('/')}/responses/";
        var resBody = new GeneralResponseSubmit { userId = userId, response = domBartle, averageResponse = 0f };
        using (UnityWebRequest reqRes = CreatePostRequest(resUrl, resBody))
        {
            yield return reqRes.SendWebRequest();
            if (reqRes.result != UnityWebRequest.Result.Success) 
                Debug.LogError("Error POST General Response (Bartle): " + reqRes.error);
        }
    }

    // --- Secuencia de Resumen Final ---

    private IEnumerator ShowSummarySequence()
    {
        SetGamePaused(true);
        _canvasGo.SetActive(true);
        _mainPanel.SetActive(false);
        _summaryPanel.SetActive(true);
        _summaryState = 0;

        // Fetch Bartle Summary
        int userId = 1;
        if (UserSession.TryGetUserId(out int savedId)) userId = savedId;
        
        string url = $"{baseUrl.TrimEnd('/')}/questions/bartle/users/{userId}/summary";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                _bartleSummaryData = JsonUtility.FromJson<BartleSummaryOut>(req.downloadHandler.text);
            }
        }

        // Calcular resultados finales aquí antes de mostrar UI
        _finalDomBartle = _bartleSummaryData != null && !string.IsNullOrEmpty(_bartleSummaryData.dominant_type) ? _bartleSummaryData.dominant_type : "(Sin respuestas)";

        if (_scoreE >= _scoreL && _scoreE >= _scoreA) _finalDomMot = "Aprendiz Explorador (Autónomo)";
        else if (_scoreL > _scoreE && _scoreL >= _scoreA) _finalDomMot = "Aprendiz Orientado a Logro (Controlado)";
        else _finalDomMot = "Aprendiz por Activar (Desmotivado)";

        if (_memoryScore >= 5) _finalMemLevel = "Avanzada";
        else if (_memoryScore >= 3) _finalMemLevel = "Buena";
        else _finalMemLevel = "Estandar";

        // Lanzar el guardado de forma asíncrona sin bloquear la UI
        if (!_resultsSaved)
        {
            _resultsSaved = true;
            StartCoroutine(PostFinalResults(userId, _finalDomBartle, _finalDomMot, _finalMemLevel));
        }

        RenderSummaryState();
    }

    private void RenderSummaryState()
    {
        _continueButton.onClick.RemoveAllListeners();
        
        if (_summaryState == 0)
        {
            // Bartle
            string bartleDesc = "";
            string lowerDom = _finalDomBartle.ToLower();
            if (lowerDom.Contains("achiever"))
                bartleDesc = "¡Eres un Achiever! Un jugador motivado por la autosuperación, la acumulación de logros y el dominio total del juego. Te apasiona completar el 100% de los desafíos, coleccionar todas las recompensas posibles y subir de nivel de la forma más eficiente. Tu mayor satisfacción es ver el progreso tangible de tu esfuerzo, superar tus propias metas y demostrar que eres capaz de dominar mecánicas complejas para alcanzar el éxito absoluto.";
            else if (lowerDom.Contains("explorer"))
                bartleDesc = "¡Eres un Explorer! Tu perfil destaca por la curiosidad insaciable, el descubrimiento y el deseo de comprender cómo funciona el mundo que te rodea. Te fascina encontrar caminos ocultos, resolver misterios y experimentar con las mecánicas del juego solo para ver qué pasa. Para ti, el verdadero valor no está en competir ni en ganar rápido, sino en sumergirte en la experiencia, conocer cada detalle y desvelar los secretos que otros pasan por alto.";
            else if (lowerDom.Contains("socializer"))
                bartleDesc = "¡Eres un Socializer! Un tipo de jugador centrado en las personas, las relaciones y la construcción de comunidad dentro del entorno de juego. Tu principal motivación es interactuar con otros, colaborar en equipo, compartir historias y formar vínculos significativos a través de la partida. Para ti, el juego es un gran escenario social donde el verdadero valor no radica en los puntos o la victoria, sino en las conexiones y los momentos compartidos con los demás.";
            else if (lowerDom.Contains("killer"))
                bartleDesc = "¡Eres un Killer! Un perfil impulsado por la competencia directa, el desafío y la dominación estratégica. Te apasiona medir tus habilidades contra el entorno o contra otros jugadores, superar récords y dejar tu huella a través de la victoria. No te conformas con participar; buscas el reconocimiento que viene con el triunfo, convirtiéndote en una fuerza competitiva que define el ritmo de la partida y juega siempre para ganar.";

            _summaryTitleText.text = $"Tipo de jugador: {_finalDomBartle}";
            _summaryDetailsText.text = bartleDesc;
            
            // Adjust details font size for large text if needed
            _summaryDetailsText.fontSize = 26;

            _continueButton.GetComponentInChildren<TMP_Text>().text = "Siguiente";
            _continueButton.onClick.AddListener(() => { _summaryState++; RenderSummaryState(); });
        }
        else if (_summaryState == 1)
        {
            // Motivación Ludo+
            _summaryTitleText.text = $"Nivel de motivación: {_finalDomMot}";
            _summaryDetailsText.text = ""; // Sin detalles extras
            _summaryDetailsText.fontSize = 36;
            
            _continueButton.GetComponentInChildren<TMP_Text>().text = "Siguiente";
            _continueButton.onClick.AddListener(() => { _summaryState++; RenderSummaryState(); });
        }
        else if (_summaryState == 2)
        {
            // Memoria
            _summaryTitleText.text = $"Memoria a corto plazo + atención: {_finalMemLevel}";
            _summaryDetailsText.text = ""; // Sin detalles extras
            _summaryDetailsText.fontSize = 36;
            
            _continueButton.GetComponentInChildren<TMP_Text>().text = "Finalizar";
            _continueButton.onClick.AddListener(() => { 
                _canvasGo.SetActive(false); 
                SetGamePaused(false); 
                // Aquí podrías cargar otra escena o mostrar el cartel final del juego
                if (GameControl.youWin == false)
                {
                    // Forzar final de GameControl si no se activó
                    GameControl.youWin = true;
                }
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
        }
    }

    // --- UI Dinámica ---

    private void BuildUnifiedUI()
    {
        _canvasGo = new GameObject("GlobalQuizCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _canvasGo.transform.SetParent(transform, false);
        Canvas canvas = _canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = _canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Main Panel
        _mainPanel = CreateUiPanel("MainPanel", _canvasGo.transform, new Color(0.07f, 0.07f, 0.09f, 0.94f));

        _promptText = CreateTmp("Prompt", _mainPanel.transform, 32, TextAlignmentOptions.TopLeft, new Vector2(80, -140), new Vector2(-80, -320));
        _promptText.textWrappingMode = TextWrappingModes.Normal;

        _progressText = CreateTmp("Progress", _mainPanel.transform, 22, TextAlignmentOptions.TopRight, new Vector2(80, -80), new Vector2(-80, -130));
        _statusText = CreateTmp("Status", _mainPanel.transform, 18, TextAlignmentOptions.Bottom, new Vector2(80, 40), new Vector2(-80, 120));
        _statusText.color = new Color(1f, 0.55f, 0.45f, 1f);

        // Image
        GameObject imgGo = new GameObject("QuestionImage", typeof(RectTransform), typeof(Image));
        imgGo.transform.SetParent(_mainPanel.transform, false);
        RectTransform imgRt = imgGo.GetComponent<RectTransform>();
        imgRt.anchorMin = new Vector2(0.5f, 0.7f);
        imgRt.anchorMax = new Vector2(0.5f, 0.7f);
        imgRt.sizeDelta = new Vector2(300, 300);
        imgRt.anchoredPosition = new Vector2(0, -50);
        _questionImage = imgGo.GetComponent<Image>();
        _questionImage.preserveAspect = true;
        _questionImage.gameObject.SetActive(false);

        // Options host
        GameObject optsHost = new GameObject("Options", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        optsHost.transform.SetParent(_mainPanel.transform, false);
        RectTransform optRt = optsHost.GetComponent<RectTransform>();
        optRt.anchorMin = new Vector2(0.5f, 0.3f);
        optRt.anchorMax = new Vector2(0.5f, 0.3f);
        optRt.pivot = new Vector2(0.5f, 0.5f);
        optRt.sizeDelta = new Vector2(900, 400);

        VerticalLayoutGroup vlg = optsHost.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 15f;
        vlg.childControlWidth = true;
        vlg.childForceExpandWidth = true;

        for (int i = 0; i < 4; i++)
        {
            _optionButtons.Add(CreateOptionButton(optsHost.transform, $"Option {i}"));
        }

        // Summary Panel
        _summaryPanel = CreateUiPanel("SummaryPanel", _canvasGo.transform, new Color(0.02f, 0.02f, 0.04f, 0.98f));
        _summaryTitleText = CreateTmp("Title", _summaryPanel.transform, 48, TextAlignmentOptions.Center, new Vector2(0, -50), new Vector2(0, -150));
        _summaryDetailsText = CreateTmp("Details", _summaryPanel.transform, 34, TextAlignmentOptions.Center, new Vector2(0, -150), new Vector2(0, -450));
        
        _continueButton = CreateCenteredButton(_summaryPanel.transform, "Continuar", new Vector2(0, -150));
        
        _summaryPanel.SetActive(false);
        _canvasGo.SetActive(false);
    }

    private void SetGamePaused(bool pause)
    {
        if (_fpsController != null)
        {
            _fpsController.enabled = !pause;
            if (pause)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                if (_fpsController.m_MouseLook != null) _fpsController.m_MouseLook.SetCursorLock(false);
            }
            else
            {
                if (_fpsController.m_MouseLook != null) _fpsController.m_MouseLook.SetCursorLock(true);
            }
        }
        Time.timeScale = pause ? 0f : 1f;
    }

    // Builders (Simplified from existing scripts)
    private GameObject CreateUiPanel(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = color;
        return go;
    }

    private TMP_Text CreateTmp(string name, Transform parent, float fontSize, TextAlignmentOptions align, Vector2 min, Vector2 max)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = min; rt.offsetMax = max;
        
        TMP_Text tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/SpecialElite SDF");
        if (font != null) tmp.font = font;

        return tmp;
    }

    private Button CreateOptionButton(Transform parent, string label)
    {
        GameObject go = new GameObject("OptionBtn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        
        LayoutElement le = go.GetComponent<LayoutElement>();
        le.minHeight = 80f;

        Image img = go.GetComponent<Image>();
        img.color = new Color(0.22f, 0.24f, 0.32f, 1f);

        Button btn = go.GetComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(0.32f, 0.36f, 0.48f, 1f);
        colors.pressedColor = new Color(0.2f, 0.22f, 0.3f, 1f);
        btn.colors = colors;

        GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(go.transform, false);
        RectTransform rt = textGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        TMP_Text tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.margin = new Vector4(24, 0, 24, 0);
        
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/SpecialElite SDF");
        if (font != null) tmp.font = font;

        return btn;
    }

    private Button CreateCenteredButton(Transform parent, string label, Vector2 pos)
    {
        GameObject go = new GameObject("Btn", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1); rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(300, 68);

        go.GetComponent<Image>().color = new Color(0.28f, 0.5f, 0.85f, 1f);
        Button btn = go.GetComponent<Button>();

        GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(go.transform, false);
        RectTransform trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;

        TMP_Text tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/SpecialElite SDF");
        if (font != null) tmp.font = font;

        return btn;
    }
}
