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
    [Tooltip("ID de la pregunta de motivación en la base de datos (ej. 27)")]
    public int motivationQuestionId = 27;

    void Start()
    {
        boxAnimator = GetComponent<Animator>();
    }

    public bool IsBoxOpen()
    {
        return isOpen;
    }

    // Ahora este método es público para que el Raycast del jugador lo pueda activar
    public void AbrirCaja()
    {
        if (!isOpen)
        {
            StartCoroutine(AbrirYCerrarCaja());
        }
    }

    IEnumerator AbrirYCerrarCaja()
    {
        isOpen = true; 
        
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

        // 2. Apenas termina de abrirse, mostramos la pregunta de motivación
        if (MotivationInGameUI.Instance != null)
        {
            bool questionAnswered = false;
            
            // Pausar el flujo hasta que el jugador responda
            MotivationInGameUI.Instance.ShowQuestion(motivationQuestionId, () => {
                questionAnswered = true;
            });
            
            yield return new WaitUntil(() => questionAnswered);
        }

        // 3. Permanece abierta un momento más
        yield return new WaitForSeconds(openTime);

        if (boxAnimator != null)
        {
            boxAnimator.speed = 1f; // Reanudar para cerrar
        }

        // 4. Terminar de cerrarse
        yield return new WaitForSeconds(timeToFullyOpen);
        
        isOpen = false; 
    }
}
