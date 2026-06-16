using NavKeypad;
using UnityEngine;

/// <summary>
/// Enlaza el keypad con la puerta del locker al iniciar la escena.
/// Colócalo en locker_0001; asigna referencias o déjalas vacías para buscarlas solas.
/// </summary>
public class LockerKeypadSetup : MonoBehaviour
{
    [SerializeField] private Keypad keypad;
    [SerializeField] private LockerDoor door;

    private void Awake()
    {
        if (door == null)
        {
            door = GetComponentInChildren<LockerDoor>(true);
        }

        if (keypad == null)
        {
            keypad = FindAnyObjectByType<Keypad>();
        }

        if (keypad == null || door == null)
        {
            // Debug.LogWarning("LockerKeypadSetup: falta Keypad o LockerDoor en la escena.", this);
            return;
        }

        keypad.OnAccessGranted.AddListener(door.Open);
    }

    private void OnDestroy()
    {
        if (keypad != null && door != null)
        {
            keypad.OnAccessGranted.RemoveListener(door.Open);
        }
    }
}
