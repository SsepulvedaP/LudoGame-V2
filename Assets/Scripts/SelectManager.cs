using System.Collections.Generic;
using NavKeypad;
using UnityEngine;
using UnityEngine.InputSystem;

public class SelectManager : MonoBehaviour
{
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private InventoryManager bagManager;

    [Header("Distancia del raycast (metros)")]
    [SerializeField] private float selectableDistance = 1.5f;
    [Tooltip("Teclas del keypad; suele necesitar un poco más de alcance que los objetos Selectable.")]
    [SerializeField] private float keypadDistance = 3f;

    [Header("Keypad")]
    [SerializeField] private bool interactWithKeypad = true;
    [SerializeField] private KeypadCameraFocus keypadFocus;

    public void SetKeypadInteractionEnabled(bool enabled)
    {
        interactWithKeypad = enabled;
    }

    private readonly List<GameObject> inventory = new List<GameObject>();
    private Transform _selection;
    private Material _savedSelectionMaterial;
    private Transform _lastHitTransform;

    private void Awake()
    {
        if (keypadFocus == null)
        {
            keypadFocus = GetComponent<KeypadCameraFocus>();
        }
    }

    private void Update()
    {
        ClearHighlight();

        var cam = Camera.main;
        if (cam == null || Time.timeScale == 0f)
        {
            return;
        }

        var aimScreen = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        if (keypadFocus != null && keypadFocus.IsFocused) {
            aimScreen = keypadFocus.GetAimScreenPosition();
        } else {
            var breakerFocus = FindAnyObjectByType<BreakerBoxCameraFocus>();
            if (breakerFocus != null && breakerFocus.IsFocused) {
                aimScreen = breakerFocus.GetAimScreenPosition();
            }
        }
        var ray = cam.ScreenPointToRay(aimScreen);

        var maxDistance = interactWithKeypad
            ? Mathf.Max(selectableDistance, keypadDistance)
            : selectableDistance;

        if (!Physics.Raycast(ray, out var hit, maxDistance))
        {
            _lastHitTransform = null;
            UpdateBagUi();
            return;
        }

        if (hit.transform != _lastHitTransform)
        {
            _lastHitTransform = hit.transform;
            // Debug.Log($"[SelectManager] Apuntando a: {hit.transform.name} (Distancia: {hit.distance:F2}m)");
        }

        // Detectar si el usuario presionó o mantiene presionado el botón de interacción (Clic izquierdo/Gamepad/Touch)
        bool clickDown = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
            || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            || (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame);

        bool clickHeld = (Mouse.current != null && Mouse.current.leftButton.isPressed)
            || (Gamepad.current != null && Gamepad.current.buttonSouth.isPressed)
            || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            || (Keyboard.current != null && Keyboard.current.eKey.isPressed);

        if (interactWithKeypad
            && hit.distance <= keypadDistance
            && hit.collider.TryGetComponent(out KeypadButton keypadButton))
        {
            if (clickDown)
            {
                keypadButton.PressButton();
            }

            UpdateBagUi();
            return;
        }

        if (hit.distance <= keypadDistance
            && hit.collider.TryGetComponent(out BreakerSwitch breakerSwitch))
        {
            if (clickDown)
            {
                breakerSwitch.ToggleSwitch();
            }

            UpdateBagUi();
            return;
        }

        if (hit.distance <= selectableDistance
            && hit.collider.GetComponentInParent<LeverCollectible>() is LeverCollectible collectible)
        {
            if (clickDown)
            {
                collectible.RecogerPalanca();
            }
            UpdateBagUi();
            return;
        }

        if (hit.distance <= selectableDistance
            && hit.collider.GetComponentInParent<LeverPuzzle>() is LeverPuzzle puzzle)
        {
            if (clickDown)
            {
                if (LeverCollectible.isLeverCollected)
                {
                    puzzle.ColocarPalanca();
                }
            }
            UpdateBagUi();
            return;
        }

        if (keypadFocus != null && keypadFocus.IsFocused)
        {
            UpdateBagUi();
            return;
        }
        
        var bFocus = FindAnyObjectByType<BreakerBoxCameraFocus>();
        if (bFocus != null && (bFocus.IsFocused || bFocus.IsBreakerBoxManagerUnlocked))
        {
            UpdateBagUi();
            return;
        }

        // Detectamos el objeto si tiene el componente PickableManager
        if (hit.transform.TryGetComponent(out PickableManager pickable))
        {
            if (hit.distance <= selectableDistance)
            {
                // Buscar el Renderer incluso en los hijos (por si el modelo está dentro del objeto)
                var selectionRenderer = hit.transform.GetComponentInChildren<Renderer>();
                if (selectionRenderer != null)
                {
                    ApplyHighlight(selectionRenderer.transform, isKeypad: false);
                }
                
                if (clickHeld)
                {
                    // Auto-asignar el selector por si BookSetup falló en asignarlo
                    if (pickable.Selector == null) pickable.Selector = this;
                    pickable.IsPickable();
                }
            }
            else
            {
                if (Time.frameCount % 30 == 0) // Loggear cada 30 frames para no spamear
                {
                    // Debug.LogWarning($"[SelectManager] Apuntando a '{hit.transform.name}' pero está muy lejos: {hit.distance:F2}m (Límite: {selectableDistance:F2}m). Acércate más o aumenta 'Selectable Distance' en el Inspector.");
                }
            }
        }

        UpdateBagUi();
    }

    private void ClearHighlight()
    {
        if (_selection == null)
        {
            return;
        }

        var selectionRenderer = _selection.GetComponent<Renderer>();
        if (selectionRenderer != null)
        {
            if (_savedSelectionMaterial != null)
            {
                selectionRenderer.material = _savedSelectionMaterial;
            }
            else if (defaultMaterial != null)
            {
                selectionRenderer.material = defaultMaterial;
            }
        }

        _selection = null;
        _savedSelectionMaterial = null;
    }

    private void ApplyHighlight(Transform target, bool isKeypad)
    {
        if (isKeypad)
        {
            return;
        }

        var selectionRenderer = target.GetComponent<Renderer>();
        if (selectionRenderer == null || highlightMaterial == null)
        {
            return;
        }

        _savedSelectionMaterial = selectionRenderer.material;
        selectionRenderer.material = highlightMaterial;
        _selection = target;
    }

    private void UpdateBagUi()
    {
        if (bagManager == null)
        {
            return;
        }

        if (inventory.Count != 0)
        {
            bagManager.BagUsed();
        }
        else
        {
            bagManager.BagEmpty();
        }
    }

    public void AddObject(GameObject gObject)
    {
        if (inventory.Count < 3)
        {
            inventory.Add(gObject);

            // Comprobar tareas automáticamente
            if (TaskManager.Instance != null)
            {
                string itemName = gObject.name.ToLower();
                
                // La tarea 'llave' en realidad es 'Abrir el locker', así que ya no se completa al tomar la llave.

                // Tarea: Recoger los libros (Verificar si ya tiene los 3)
                int bookCount = 0;
                foreach (var item in inventory)
                {
                    string invName = item.name.ToLower();
                    if (invName.Contains("buch") || invName.Contains("book") || invName.Contains("libro"))
                    {
                        bookCount++;
                    }
                }

                if (bookCount >= 3)
                {
                    TaskManager.Instance.CompletarLibros();
                    if (GlobalQuizManager.Instance != null)
                    {
                        GlobalQuizManager.Instance.ShowNextChunk();
                    }
                }
            }
        }
    }

    public GameObject[] TemporaryInventory()
    {
        return inventory.ToArray();
    }
}
