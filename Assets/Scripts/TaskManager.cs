using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance;

    [System.Serializable]
    public class TaskItem
    {
        public string id;
        public Transform checkmarkRoot;
        public TextMeshProUGUI textComponent;
        [HideInInspector] public bool isCompleted;
    }

    [Header("Lista de Tareas")]
    public TaskItem[] tasks;
    
    [Header("UI/UX Ajustes")]
    public Color completedTextColor = new Color(0.6f, 0.6f, 0.6f, 0.5f); // Gris semitransparente (Elegante)
    public Color checkmarkColor = new Color(0.2f, 0.9f, 0.4f, 1f); // Verde brillante
    
    [Header("Audio (Opcional)")]
    [Tooltip("Arrastra un efecto de sonido de 'Ding' o 'Check' aquí")]
    public AudioClip completeSound;
    private AudioSource audioSource;

    private void Awake()
    {
        Instance = this; // Sobrescribir la instancia para evitar bugs de destrucción en el editor

        // Preparamos el emisor de audio de forma dinámica
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    private void Start()
    {
        // Estado inicial de la UI
        foreach (var task in tasks)
        {
            if (task.checkmarkRoot != null)
            {
                task.checkmarkRoot.localScale = Vector3.zero; // Oculto al inicio
            }
            
            if (task.textComponent != null)
            {
                task.textComponent.fontStyle = FontStyles.Normal;
                task.textComponent.color = Color.white;
            }
        }
    }

    public void CompleteTask(string taskId)
    {
        // Debug.Log($"[TaskManager] Intentando completar tarea: {taskId}");
        foreach (var task in tasks)
        {
            if (task.id == taskId && !task.isCompleted)
            {
                task.isCompleted = true;
                // Debug.Log($"[TaskManager] ¡Tarea '{taskId}' completada con éxito!");
                
                // Dispara la animación de completar
                StartCoroutine(AnimateTaskCompletion(task));
                
                if (completeSound != null)
                {
                    audioSource.PlayOneShot(completeSound);
                }
                break;
            }
        }
    }

    private IEnumerator AnimateTaskCompletion(TaskItem task)
    {
        // 1. Efecto visual en el texto (Tachado y difuminado)
        if (task.textComponent != null)
        {
            task.textComponent.color = completedTextColor;
            task.textComponent.fontStyle = FontStyles.Strikethrough;
        }

        // 2. Animación "Pop" (Bouncy / Overshoot) del Checkmark
        if (task.checkmarkRoot != null)
        {
            float time = 0;
            float duration = 0.45f;
            Transform checkTransform = task.checkmarkRoot;

            while (time < duration)
            {
                time += Time.deltaTime;
                float t = time / duration;
                
                // Fórmula matemática "Ease Out Back" para un efecto elástico muy satisfactorio
                float c1 = 1.70158f;
                float c3 = c1 + 1f;
                float ease = 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);

                checkTransform.localScale = Vector3.one * ease;
                yield return null;
            }
            
            // Asegurar que termina exactamente en escala 1
            checkTransform.localScale = Vector3.one;
        }
    }

    // Funciones de ayuda
    public void CompletarCuadro() => CompleteTask("cuadro");
    public void CompletarLlave() => CompleteTask("llave");
    public void CompletarTomarLlave() => CompleteTask("tomar_llave");
    public void CompletarCaja() => CompleteTask("caja");
    public void CompletarLibros() => CompleteTask("libros");
    public void CompletarPalanca() => CompleteTask("palanca");

    // Mostrar/Ocultar todo el panel de objetivos (útil para el keypad)
    public void SetVisible(bool isVisible)
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = gameObject.AddComponent<CanvasGroup>();
        }
        
        cg.alpha = isVisible ? 1f : 0f;
        cg.interactable = isVisible;
        cg.blocksRaycasts = isVisible;
    }
}
