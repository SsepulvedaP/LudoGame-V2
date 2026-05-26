using System.Collections.Generic;
using NavKeypad;
using UnityEngine;

public class SelectManager : MonoBehaviour
{
    [SerializeField] private string selectableTag = "Selectable";
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
        if (cam == null)
        {
            return;
        }

        var aimScreen = keypadFocus != null
            ? keypadFocus.GetAimScreenPosition()
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        var ray = cam.ScreenPointToRay(aimScreen);

        var maxDistance = interactWithKeypad
            ? Mathf.Max(selectableDistance, keypadDistance)
            : selectableDistance;

        if (!Physics.Raycast(ray, out var hit, maxDistance))
        {
            UpdateBagUi();
            return;
        }

        if (interactWithKeypad
            && hit.distance <= keypadDistance
            && hit.collider.TryGetComponent(out KeypadButton keypadButton))
        {
            if (Input.GetButtonDown("Fire1") || Input.GetMouseButtonDown(0))
            {
                keypadButton.PressButton();
            }

            UpdateBagUi();
            return;
        }

        if (keypadFocus != null && (keypadFocus.IsFocused || keypadFocus.IsKeypadUnlocked))
        {
            UpdateBagUi();
            return;
        }

        if (hit.distance <= selectableDistance
            && hit.transform.CompareTag(selectableTag)
            && hit.transform.TryGetComponent(out PickableManager pickable))
        {
            var selectionRenderer = hit.transform.GetComponent<Renderer>();
            if (selectionRenderer != null)
            {
                ApplyHighlight(hit.transform, isKeypad: false);
                if (Input.GetButton("Fire1"))
                {
                    pickable.IsPickable();
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
        }
    }

    public GameObject[] TemporaryInventory()
    {
        return inventory.ToArray();
    }
}
