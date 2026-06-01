using UnityEngine;

public class BreakerSwitch : MonoBehaviour
{
    [Tooltip("El ID o número de este interruptor (ej. 1, 2, 3...)")]
    public int switchId;

    [Tooltip("Rotación local cuando está APAGADO")]
    public Vector3 offRotation = Vector3.zero;

    [Tooltip("Rotación local cuando está ENCENDIDO")]
    public Vector3 onRotation = new Vector3(45f, 0f, 0f);

    private bool isOn = false;
    private BreakerBoxManager manager;

    void Start()
    {
        manager = GetComponentInParent<BreakerBoxManager>();
        UpdateVisuals();
    }

    public bool IsOn()
    {
        return isOn;
    }

    public void ToggleSwitch()
    {
        isOn = !isOn;
        UpdateVisuals();

        if (manager != null)
        {
            manager.CheckCombination();
        }
    }

    private void UpdateVisuals()
    {
        transform.localEulerAngles = isOn ? onRotation : offRotation;
    }
}
