using UnityEngine;

public class KeyItem : MonoBehaviour
{
    public static bool isKeyCollected = false;
    [SerializeField] GameObject Llave;

    [Tooltip("IDs de las preguntas de motivación en la base de datos")]
    public int[] motivationQuestionIds = new int[] { 26 };

    // Ahora la lógica de presionar 'E' se maneja desde el jugador usando Raycast
    public void RecogerLlave()
    {
        if (!isKeyCollected)
        {
            isKeyCollected = true;
            // Debug.Log("¡Llave recogida!");
            
            // Nueva misión: Tomar la llave
            if (TaskManager.Instance != null)
            {
                TaskManager.Instance.CompletarTomarLlave();
            }
            
            // Mostrar preguntas del GlobalQuizManager al recoger la llave
            if (GlobalQuizManager.Instance != null)
            {
                GlobalQuizManager.Instance.ShowNextChunk();
            }
            Llave.SetActive(true);
            gameObject.SetActive(false); // Ocultar llave directamente
        }
    }
}
