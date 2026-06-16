using UnityEngine;

public class LeverCollectible : MonoBehaviour
{
    public static bool isLeverCollected = false;

    [Tooltip("IDs de las preguntas de motivación que salen al recoger la palanca")]
    public int[] motivationQuestionIds = new int[] { }; 

    [Tooltip("Referencia opcional a la caja de preguntas para validar que esté abierta")]
    public QuestionBox parentBox;

    private Collider myCollider;

    private void Start()
    {
        myCollider = GetComponent<Collider>();
        if (gameObject.GetComponent<InteractableHighlighter>() == null)
        {
            gameObject.AddComponent<InteractableHighlighter>();
        }
    }

    private void Update()
    {
        // Si hay una caja asignada, encendemos el collider SOLO si la caja está abierta
        if (myCollider != null && parentBox != null)
        {
            myCollider.enabled = parentBox.IsBoxOpen();
        }
    }

    public void RecogerPalanca()
    {
        if (!isLeverCollected)
        {
            if (parentBox != null && !parentBox.IsBoxOpen())
            {
                // Debug.LogWarning("No se puede recoger la palanca si la caja está cerrada.");
                return;
            }

            isLeverCollected = true;
            // Debug.Log("¡Palanca recogida!");

            // No completamos la tarea de la palanca aquí, sino en LeverPuzzle.cs
            
            // Aquí mostramos el siguiente bloque de preguntas, que corresponde al finalizar la caja
            if (GlobalQuizManager.Instance != null)
            {
                GlobalQuizManager.Instance.ShowNextChunk();
            }
            
            OcultarPalanca();
        }
    }

    private void OcultarPalanca()
    {
        // Ocultar el objeto padre completo (la palanca visual) y no solo la esfera
        if (transform.parent != null)
        {
            transform.parent.gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
