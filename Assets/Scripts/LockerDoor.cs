using System.Collections;
using UnityEngine;

/// <summary>
/// Abre la puerta del locker rotándola en espacio local (eje Y por defecto).
/// Conéctalo al evento On Access Granted del componente Keypad.
/// </summary>
public class LockerDoor : MonoBehaviour
{
    [SerializeField] private float openAngle = 105f;
    [SerializeField] private float openDuration = 0.85f;
    [SerializeField] private Vector3 localRotationAxis = Vector3.up;

    private Quaternion _closedRotation;
    private Quaternion _openRotation;
    private bool _isOpen;
    private Coroutine _animation;

    private void Awake()
    {
        _closedRotation = transform.localRotation;
        _openRotation = _closedRotation * Quaternion.AngleAxis(openAngle, localRotationAxis.normalized);
    }

    public void Open()
    {
        if (_isOpen)
        {
            return;
        }

        _isOpen = true;
        if (_animation != null)
        {
            StopCoroutine(_animation);
        }

        _animation = StartCoroutine(RotateRoutine(_openRotation));
    }

    public void Close()
    {
        if (!_isOpen)
        {
            return;
        }

        _isOpen = false;
        if (_animation != null)
        {
            StopCoroutine(_animation);
        }

        _animation = StartCoroutine(RotateRoutine(_closedRotation));
    }

    private IEnumerator RotateRoutine(Quaternion target)
    {
        var start = transform.localRotation;
        var elapsed = 0f;
        var duration = Mathf.Max(0.01f, openDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.localRotation = Quaternion.Slerp(start, target, t);
            yield return null;
        }

        transform.localRotation = target;
        _animation = null;
    }
}
