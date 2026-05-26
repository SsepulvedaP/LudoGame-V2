using System.Collections;
using NavKeypad;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

/// <summary>
/// Acerca la cámara al keypad. Pulsa E mirando el keypad (o muy cerca) · Escape = salir.
/// </summary>
public class KeypadCameraFocus : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private FirstPersonController firstPersonController;
    [SerializeField] private CharacterController characterController;

    [Header("Vista del keypad")]
    [SerializeField] private Transform focusViewpoint;
    [SerializeField] private float focusFov = 28f;
    [SerializeField] private float focusTransitionSeconds = 0.45f;

    [Header("Detección")]
    [SerializeField] private float aimMaxDistance = 4f;
    [Tooltip("Si estás a esta distancia del punto de vista, puedes pulsar E aunque el rayo no pegue en una tecla.")]
    [SerializeField] private float approachDistance = 2.2f;
    [SerializeField] private KeyCode enterKey = KeyCode.E;
    [SerializeField] private KeyCode exitKey = KeyCode.Escape;

    [Header("Al desbloquear")]
    [SerializeField] private Keypad keypad;
    [Tooltip("Espera antes de quitar el zoom tras un PIN correcto (para ver 'Granted' en pantalla).")]
    [SerializeField] private float exitFocusDelayOnUnlock = 0.75f;
    [Tooltip("Si está activado, al acertar el PIN se destruye el objeto del punto de vista del keypad (para que no vuelva a salir el prompt).")]
    [SerializeField] private bool destroyFocusViewpointOnUnlock = true;
    [Tooltip("Si está activado, al acertar el PIN el prompt de interacción se destruye para que no vuelva a aparecer.")]
    [SerializeField] private bool destroyInteractPromptOnUnlock = true;

    [Header("UI opcional")]
    [SerializeField] private GameObject interactPrompt;
    [Tooltip("UI Selector del inventario. Se oculta en modo zoom (es un rectángulo grande, no un cursor).")]
    [SerializeField] private RectTransform aimReticle;

    private Coroutine _unlockRoutine;
    private bool _keypadUnlocked;
    private SelectManager _selectManager;

    private Vector2 _savedReticleAnchoredPosition;
    private bool _reticleWasActive = true;
    private Transform _cameraParent;
    private Vector3 _savedLocalPosition;
    private Quaternion _savedLocalRotation;
    private float _savedFov;
    private bool _focused;
    private float _blend;
    private Vector3 _blendStartPos;
    private Quaternion _blendStartRot;
    private float _blendStartFov;
    private Vector3 _blendEndPos;
    private Quaternion _blendEndRot;
    private float _blendEndFov;
    private bool _blending;

    public bool IsFocused => _focused || _blending;

    public Vector2 GetAimScreenPosition()
    {
        if (_focused && !_blending)
        {
            return Input.mousePosition;
        }

        return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    private void Awake()
    {
        if (firstPersonController == null)
        {
            firstPersonController = GetComponent<FirstPersonController>();
        }

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        ResolvePlayerCamera();

        _selectManager = GetComponent<SelectManager>();
    }

    private void Start()
    {
        ResolvePlayerCamera();
        ResolveKeypad();
        SetPromptVisible(false);

        if (keypad != null)
        {
            keypad.OnAccessGranted.AddListener(OnKeypadAccessGranted);
        }
    }

    public bool IsKeypadUnlocked => _keypadUnlocked;

    private void OnDestroy()
    {
        if (keypad != null)
        {
            keypad.OnAccessGranted.RemoveListener(OnKeypadAccessGranted);
        }
    }

    private void ResolveKeypad()
    {
        if (keypad != null)
        {
            return;
        }

        keypad = FindFirstObjectByType<Keypad>();
    }

    private void OnKeypadAccessGranted()
    {
        if (_keypadUnlocked)
        {
            return;
        }

        _keypadUnlocked = true;
        SetPromptVisible(false);
        _selectManager?.SetKeypadInteractionEnabled(false);

        if (_unlockRoutine != null)
        {
            StopCoroutine(_unlockRoutine);
        }

        _unlockRoutine = StartCoroutine(HandleUnlockRoutine());
    }

    private IEnumerator HandleUnlockRoutine()
    {
        if (exitFocusDelayOnUnlock > 0f)
        {
            yield return new WaitForSeconds(exitFocusDelayOnUnlock);
        }

        if (_focused)
        {
            ExitFocus();
        }
        else if (IsCameraDetachedFromPlayer())
        {
            ForceRestoreCameraImmediate();
        }

        while (_blending)
        {
            yield return null;
        }

        CleanupAfterUnlock();
        DisableLegacyKeypadPrompts();
        _unlockRoutine = null;
    }

    private void CleanupAfterUnlock()
    {
        SetPromptVisible(false);

        if (destroyInteractPromptOnUnlock && interactPrompt != null)
        {
            Destroy(interactPrompt);
            interactPrompt = null;
        }

        if (destroyFocusViewpointOnUnlock && focusViewpoint != null)
        {
            Destroy(focusViewpoint.gameObject);
            focusViewpoint = null;
        }
    }

    private static void DisableLegacyKeypadPrompts()
    {
        var legacyHandlers = FindObjectsByType<FManson>(FindObjectsSortMode.None);
        for (var i = 0; i < legacyHandlers.Length; i++)
        {
            legacyHandlers[i].DisablePermanently();
        }
    }

    private bool IsCameraDetachedFromPlayer()
    {
        return playerCamera != null
               && _cameraParent != null
               && playerCamera.transform.parent == null;
    }

    private void ForceRestoreCameraImmediate()
    {
        if (playerCamera == null || _cameraParent == null)
        {
            return;
        }

        playerCamera.transform.SetParent(_cameraParent, false);
        playerCamera.transform.localPosition = _savedLocalPosition;
        playerCamera.transform.localRotation = _savedLocalRotation;
        playerCamera.fieldOfView = _savedFov;
        _focused = false;
        _blending = false;
        RestoreReticlePosition();
        SetPlayerControlEnabled(true);
        RestoreFpsCursorAfterKeypad();
    }

    private void Update()
    {
        ResolvePlayerCamera();

        if (_blending)
        {
            StepBlend();
            SetPromptVisible(false);
            return;
        }

        if (_focused)
        {
            SetPromptVisible(false);
            if (!_keypadUnlocked
                && (Input.GetKeyDown(exitKey) || Input.GetKeyDown(KeyCode.Backspace)))
            {
                ExitFocus();
            }

            return;
        }

        if (_keypadUnlocked)
        {
            SetPromptVisible(false);
            return;
        }

        if (focusViewpoint == null || playerCamera == null)
        {
            SetPromptVisible(false);
            return;
        }

        var canInteract = CanInteractWithKeypad();
        SetPromptVisible(canInteract);

        if (canInteract && Input.GetKeyDown(enterKey))
        {
            EnterFocus();
        }
    }

    private void ResolvePlayerCamera()
    {
        if (playerCamera != null && playerCamera.isActiveAndEnabled)
        {
            return;
        }

        var cameras = GetComponentsInChildren<Camera>(false);
        for (var i = 0; i < cameras.Length; i++)
        {
            if (cameras[i].isActiveAndEnabled)
            {
                playerCamera = cameras[i];
                return;
            }
        }

        if (Camera.main != null && Camera.main.isActiveAndEnabled)
        {
            playerCamera = Camera.main;
        }
    }

    private bool CanInteractWithKeypad()
    {
        if (focusViewpoint == null || playerCamera == null)
        {
            return false;
        }

        var distToFocus = Vector3.Distance(transform.position, focusViewpoint.position);
        if (distToFocus > approachDistance)
        {
            return false;
        }

        if (RaycastHitsKeypad(playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))))
        {
            return true;
        }

        var toFocus = focusViewpoint.position - playerCamera.transform.position;
        if (toFocus.sqrMagnitude < 0.01f)
        {
            return true;
        }

        toFocus.Normalize();
        return Vector3.Dot(playerCamera.transform.forward, toFocus) > 0.45f;
    }

    private bool RaycastHitsKeypad(Ray ray)
    {
        if (!Physics.Raycast(ray, out var hit, aimMaxDistance))
        {
            return false;
        }

        return hit.collider.GetComponentInParent<Keypad>() != null
               || hit.collider.GetComponentInParent<KeypadButton>() != null;
    }

    private void SetPromptVisible(bool visible)
    {
        if (interactPrompt != null && interactPrompt.activeSelf != visible)
        {
            interactPrompt.SetActive(visible);
        }
    }

    public void EnterFocus()
    {
        if (_focused || focusViewpoint == null || playerCamera == null)
        {
            return;
        }

        _cameraParent = playerCamera.transform.parent;
        _savedLocalPosition = playerCamera.transform.localPosition;
        _savedLocalRotation = playerCamera.transform.localRotation;
        _savedFov = playerCamera.fieldOfView;

        SetPlayerControlEnabled(false);

        playerCamera.transform.SetParent(null, true);

        _blendStartPos = playerCamera.transform.position;
        _blendStartRot = playerCamera.transform.rotation;
        _blendStartFov = playerCamera.fieldOfView;
        _blendEndPos = focusViewpoint.position;
        _blendEndRot = focusViewpoint.rotation;
        _blendEndFov = focusFov;

        HideInventoryReticle();

        _focused = true;
        _blend = 0f;
        _blending = true;
        SetPromptVisible(false);
    }

    public void ExitFocus()
    {
        if (!_focused || playerCamera == null)
        {
            return;
        }

        _blendStartPos = playerCamera.transform.position;
        _blendStartRot = playerCamera.transform.rotation;
        _blendStartFov = playerCamera.fieldOfView;

        if (_cameraParent != null)
        {
            _blendEndPos = _cameraParent.TransformPoint(_savedLocalPosition);
            _blendEndRot = _cameraParent.rotation * _savedLocalRotation;
        }
        else
        {
            _blendEndPos = _blendStartPos;
            _blendEndRot = _blendStartRot;
        }

        _blendEndFov = _savedFov;
        _blend = 0f;
        _blending = true;
        _focused = false;
    }

    private void StepBlend()
    {
        _blend += Time.deltaTime / Mathf.Max(0.01f, focusTransitionSeconds);
        var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_blend));

        playerCamera.transform.position = Vector3.Lerp(_blendStartPos, _blendEndPos, t);
        playerCamera.transform.rotation = Quaternion.Slerp(_blendStartRot, _blendEndRot, t);
        playerCamera.fieldOfView = Mathf.Lerp(_blendStartFov, _blendEndFov, t);

        if (_blend < 1f)
        {
            return;
        }

        _blending = false;

        if (_focused)
        {
            playerCamera.transform.position = _blendEndPos;
            playerCamera.transform.rotation = _blendEndRot;
            playerCamera.fieldOfView = _blendEndFov;
            return;
        }

        playerCamera.transform.SetParent(_cameraParent, false);
        playerCamera.transform.localPosition = _savedLocalPosition;
        playerCamera.transform.localRotation = _savedLocalRotation;
        playerCamera.fieldOfView = _savedFov;
        RestoreReticlePosition();
        SetPlayerControlEnabled(true);
        RestoreFpsCursorAfterKeypad();
    }

    private void RestoreFpsCursorAfterKeypad()
    {
        if (firstPersonController == null || firstPersonController.m_MouseLook == null)
        {
            return;
        }

        firstPersonController.m_MouseLook.SuppressEscapeUnlock(0.35f);
        firstPersonController.m_MouseLook.ForceCursorLocked();
    }

    private void HideInventoryReticle()
    {
        if (aimReticle == null)
        {
            return;
        }

        _savedReticleAnchoredPosition = aimReticle.anchoredPosition;
        _reticleWasActive = aimReticle.gameObject.activeSelf;
        aimReticle.gameObject.SetActive(false);
    }

    private void RestoreReticlePosition()
    {
        if (aimReticle == null)
        {
            return;
        }

        aimReticle.gameObject.SetActive(_reticleWasActive);
        aimReticle.anchoredPosition = _savedReticleAnchoredPosition;
    }

    private void SetPlayerControlEnabled(bool enabled)
    {
        if (firstPersonController != null)
        {
            firstPersonController.enabled = enabled;
        }

        if (characterController != null)
        {
            characterController.enabled = enabled;
        }

        if (firstPersonController != null && firstPersonController.m_MouseLook != null)
        {
            firstPersonController.m_MouseLook.SetCursorLock(enabled);
        }

        if (enabled)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}
