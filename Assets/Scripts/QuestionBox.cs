using UnityEngine;
using System.Collections;

public class QuestionBox : MonoBehaviour
{
    private Animator boxAnimator;
    private bool isOpen = false;

    [Header("Configuración de Animación")]
    public string animationName = "opened_closed";
    public string idleAnimationName = "Idle";
    
    [Tooltip("Tiempo en segundos hasta que la caja se abre por completo.")]
    public float timeToFullyOpen = 1.0f;

    [Tooltip("Tiempo que permanece abierta la caja (después de responder la pregunta).")]
    public float openTime = 3f;

    [Header("Motivación")]
    [Tooltip("IDs de las preguntas de motivación en la base de datos")]
    public int[] motivationQuestionIds = new int[] { 27 };

    private Collider boxCollider;

    void Start()
    {
        boxAnimator = GetComponent<Animator>();
        boxCollider = GetComponent<Collider>();
    }

    public bool IsBoxOpen()
    {
        return isOpen;
    }

    // Ahora este método es público para que el Raycast del jugador lo pueda activar
    private bool isOpening = false;

    public void AbrirCaja()
    {
        if (!isOpen && !isOpening)
        {
            if (TaskManager.Instance != null)
            {
                TaskManager.Instance.CompletarCaja(); // 'caja' es el ID para "Abrir la caja fuerte"
            }

            StartCoroutine(AbrirYCerrarCaja());
        }
    }

    IEnumerator AbrirYCerrarCaja()
    {
        isOpening = true; 
        if (boxCollider != null)
        {
            boxCollider.enabled = false; // Desactivar collider para permitir interactuar con la palanca dentro
        }
        
        if (boxAnimator != null)
        {
            boxAnimator.speed = 1f;
            boxAnimator.Play(animationName, 0, 0f);
        }

        // 1. Esperamos a que la caja se abra físicamente
        yield return new WaitForSeconds(timeToFullyOpen);

        if (boxAnimator != null)
        {
            boxAnimator.speed = 0f; // Pausar la animación cuando está abierta
        }

        // 2. Apenas termina de abrirse, quitamos la pausa (si la hubiese) o marcamos que ya está abierta.
        isOpening = false;
        isOpen = true; 
        // Las preguntas ya fueron llamadas al iniciar el método.


        // 3. ¡La caja se queda abierta permanentemente para que el jugador pueda tomar la palanca tranquilamente!
        // No reanudamos la animación para cerrar, ni reactivamos el collider.
    }
}
