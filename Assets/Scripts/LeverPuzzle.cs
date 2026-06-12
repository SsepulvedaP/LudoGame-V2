using UnityEngine;
using UnityEngine.Events;

public class LeverPuzzle : MonoBehaviour
{
    [Header("Estado del Puzzle")]
    private bool isPlaced = false;

    public bool IsPlaced => isPlaced;

    [Header("Objetos de la Palanca")]
    [Tooltip("El tubo/barra de la palanca (RMain Knob)")]
    public GameObject leverStick;
    
    [Tooltip("La bolita o punta de la palanca (Sphere001)")]
    public GameObject leverBall;

    [Tooltip("El objeto de anillos o base (Rigns)")]
    public GameObject rings;

    [Header("Configuración de Posiciones y Rotaciones (Objetivo)")]
    public Vector3 targetStickPos = new Vector3(-0.5151f, 0.0649f, 0.2216f);
    public Vector3 targetStickRot = new Vector3(-70f, 0f, 0f);
    
    public Vector3 targetBallPos = new Vector3(-0.52f, 0.36f, -0.05f);
    public Vector3 targetBallRot = new Vector3(-80f, 0f, 0f);

    [Tooltip("La posición local que tomarán los anillos al activarse")]
    public Vector3 targetRingsPos = new Vector3(-0.574f, 0.369f, -0.05f);

    [Tooltip("La rotación local que tomarán los anillos al activarse")]
    public Vector3 targetRingsRot = new Vector3(0f, -90f, -100f);

    [Header("Preguntas a mostrar al colocar la palanca")]
    [Tooltip("IDs de las preguntas de motivación que salen al colocar la palanca")]
    public int[] motivationQuestionIds = new int[] { };

    [Header("Objetos a Activar/Desactivar al Colocar")]
    [Tooltip("Objetos que se activarán automáticamente al colocar la palanca")]
    public GameObject[] objectsToActivate;

    [Tooltip("Objetos que se desactivarán automáticamente al colocar la palanca")]
    public GameObject[] objectsToDeactivate;

    [Header("Eventos")]
    public UnityEvent onLeverPlaced;

    void Start()
    {
        // Al iniciar el juego, si la palanca no se ha colocado, ocultamos la palanca y su bolita
        if (!isPlaced)
        {
            if (leverStick != null) leverStick.SetActive(false);
            if (leverBall != null) leverBall.SetActive(false);
        }
    }

    public void ColocarPalanca()
    {
        if (isPlaced) return;

        isPlaced = true;
        Debug.Log("¡Palanca colocada en el mecanismo!");

        StartCoroutine(AnimateLeverPlacement());
    }

    private System.Collections.IEnumerator AnimateLeverPlacement()
    {
        // Activamos los objetos
        if (leverStick != null) leverStick.SetActive(true);
        if (leverBall != null) leverBall.SetActive(true);

        if (rings != null)
        {
            rings.transform.localPosition = targetRingsPos;
            rings.transform.localEulerAngles = targetRingsRot;
        }

        // Posicionamos la palanca y la bolita, pero con rotación en 0 (hacia arriba)
        if (leverStick != null)
        {
            leverStick.transform.localPosition = targetStickPos;
            leverStick.transform.localEulerAngles = Vector3.zero;
        }
        if (leverBall != null)
        {
            leverBall.transform.localPosition = targetBallPos;
            leverBall.transform.localEulerAngles = Vector3.zero;
        }

        // Activar objetos adicionales configurados
        if (objectsToActivate != null)
        {
            foreach (GameObject obj in objectsToActivate)
            {
                if (obj != null) obj.SetActive(true);
            }
        }

        // Desactivar objetos adicionales configurados
        if (objectsToDeactivate != null)
        {
            foreach (GameObject obj in objectsToDeactivate)
            {
                if (obj != null) obj.SetActive(false);
            }
        }

        // Animación suave de 1.2 segundos
        float duration = 1.2f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            // Usamos SmoothStep para un movimiento más natural
            float t = Mathf.SmoothStep(0f, 1f, progress);

            if (leverStick != null) leverStick.transform.localEulerAngles = Vector3.Lerp(Vector3.zero, targetStickRot, t);
            if (leverBall != null) leverBall.transform.localEulerAngles = Vector3.Lerp(Vector3.zero, targetBallRot, t);

            yield return null;
        }

        // Aseguramos los valores finales
        if (leverStick != null) leverStick.transform.localEulerAngles = targetStickRot;
        if (leverBall != null) leverBall.transform.localEulerAngles = targetBallRot;

        // Completamos la tarea de la palanca en el TaskManager
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.CompletarPalanca();
        }

        // Ejecutar evento opcional
        onLeverPlaced?.Invoke();

        // Mostrar preguntas de motivación (ahora a través de GlobalQuizManager)
        if (GlobalQuizManager.Instance != null)
        {
            GlobalQuizManager.Instance.ShowNextChunk();
        }
    }
}
