using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Tooltip("Distancia máxima a la que el jugador puede interactuar con objetos.")]
    public float interactionDistance = 3.0f;

    private Camera cam;
    private string hoverText = "";

    void Start()
    {
        // Buscamos la cámara principal en el jugador
        cam = Camera.main;
        if (cam == null)
        {
            cam = GetComponentInChildren<Camera>();
        }
    }

    void Update()
    {
        hoverText = ""; // Reseteamos el texto en cada frame

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
    }

    // Muestra el letrero en el centro de la pantalla
    void OnGUI()
    {
        if (!string.IsNullOrEmpty(hoverText))
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 25;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;

            // Dibujar en el centro inferior de la pantalla
            GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height / 2 + 50, 300, 50), hoverText, style);
        }
    }
}
