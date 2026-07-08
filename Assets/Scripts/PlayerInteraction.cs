using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Tooltip("Distancia máxima a la que el jugador puede interactuar con objetos.")]
    public float interactionDistance = 3.0f;

    private Camera cam;
    private string hoverText = "";
    
    public static string GlobalHoverText = "";
    
    // UI elements
    private GameObject uiCanvas;
    private TMPro.TextMeshProUGUI interactionText;

    void Start()
    {
        // Buscamos la cámara principal en el jugador
        cam = Camera.main;
        if (cam == null)
        {
            cam = GetComponentInChildren<Camera>();
        }
        
        SetupUI();
    }

    private void SetupUI()
    {
        // Crear un Canvas en runtime para los textos de interacción
        uiCanvas = new GameObject("PlayerInteractionCanvas");
        Canvas canvas = uiCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        UnityEngine.UI.CanvasScaler scaler = uiCanvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        uiCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // Crear el objeto de texto
        GameObject textGo = new GameObject("InteractionText");
        textGo.transform.SetParent(uiCanvas.transform, false);
        
        interactionText = textGo.AddComponent<TMPro.TextMeshProUGUI>();
        interactionText.alignment = TMPro.TextAlignmentOptions.Center;
        interactionText.fontSize = 32;
        interactionText.color = Color.white;
        
        // Centrar y posicionar un poco abajo
        RectTransform rt = interactionText.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(800, 100);
        rt.anchoredPosition = new Vector2(0, -50);
        
        // Cargar fuente del juego si existe
        TMPro.TMP_FontAsset font = Resources.Load<TMPro.TMP_FontAsset>("Fonts & Materials/SpecialElite SDF");
        if (font != null) interactionText.font = font;
        
        interactionText.gameObject.SetActive(false);
    }

    void Update()
    {
        hoverText = GlobalHoverText; // Toma el texto de otros scripts si existe
        GlobalHoverText = ""; // Reseteamos el texto global para el siguiente frame

        if (cam == null) return;
        
        // NO procesar interacciones si el juego está pausado (ej. cuestionario abierto)
        if (Time.timeScale == 0f) return;

        // Lanzamos un rayo hacia el frente desde el centro de la pantalla (mirada del jugador)
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        bool keyEPressedThisFrame = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

        // Si el rayo impacta con algo dentro de la distancia máxima
        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            // ¿Estamos mirando la llave?
            if (hit.collider.TryGetComponent(out KeyItem key))
            {
                if (!KeyItem.isKeyCollected)
                {
                    hoverText = "[ Presiona 'E' para tomar Llave ]";
                    if (keyEPressedThisFrame)
                    {
                        key.RecogerLlave();
                    }
                }
            }
            // ¿Estamos mirando la caja?
            else if (hit.collider.TryGetComponent(out QuestionBox box))
            {
                if (!box.IsBoxOpen())
                {
                    if (KeyItem.isKeyCollected)
                    {
                        hoverText = "[ Presiona 'E' para Abrir Caja ]";
                        if (keyEPressedThisFrame)
                        {
                            box.AbrirCaja();
                        }
                    }
                    else
                    {
                        hoverText = "[ Necesitas la Llave ]";
                    }
                }
            }
            // ¿Estamos mirando la palanca coleccionable?
            else if (hit.collider.GetComponentInParent<LeverCollectible>() is LeverCollectible collectible)
            {
                if (!LeverCollectible.isLeverCollected)
                {
                    hoverText = "[ Presiona 'E' para tomar Palanca ]";
                    if (keyEPressedThisFrame)
                    {
                        collectible.RecogerPalanca();
                    }
                }
            }
            // ¿Estamos mirando el mecanismo de la palanca?
            else if (hit.collider.GetComponentInParent<LeverPuzzle>() is LeverPuzzle puzzle)
            {
                if (!puzzle.IsPlaced)
                {
                    if (LeverCollectible.isLeverCollected)
                    {
                        hoverText = "[ Presiona 'E' para colocar la Palanca ]";
                        if (keyEPressedThisFrame)
                        {
                            puzzle.ColocarPalanca();
                        }
                    }
                    else
                    {
                        hoverText = "[ Necesitas la Palanca ]";
                    }
                }
            }
        }

        // Actualizar UI en Update en lugar de OnGUI
        if (!string.IsNullOrEmpty(hoverText))
        {
            if (!interactionText.gameObject.activeSelf) interactionText.gameObject.SetActive(true);
            interactionText.text = hoverText;
        }
        else
        {
            if (interactionText.gameObject.activeSelf) interactionText.gameObject.SetActive(false);
        }
    }
}
