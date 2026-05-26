using UnityEngine;

public class InventoryScroll : MonoBehaviour
{
    [SerializeField] private GameObject imgSelector;
    [SerializeField] private KeypadCameraFocus keypadFocus;

    private Vector3 scale;

    private void Awake()
    {
        if (keypadFocus == null)
        {
            keypadFocus = FindFirstObjectByType<KeypadCameraFocus>();
        }

        scale = new Vector3(225f, 0f, 0f);
    }

    private void Update()
    {
        if (imgSelector == null)
        {
            return;
        }

        if (keypadFocus != null && keypadFocus.IsFocused)
        {
            return;
        }

        var pos = imgSelector.transform.localPosition;
        pos += Mathf.Clamp(Input.mouseScrollDelta.y, -1f, 1f) * scale;
        if (pos.x > 225f)
        {
            pos.x = -225f;
        }

        if (pos.x < -225f)
        {
            pos.x = 225f;
        }

        imgSelector.transform.localPosition = pos;
    }
}
