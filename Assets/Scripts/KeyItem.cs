using UnityEngine;

public class KeyItem : MonoBehaviour
{
    public static bool isKeyCollected = false;

    [Tooltip("IDs de las preguntas de motivación en la base de datos")]
    public int[] motivationQuestionIds = new int[] { 26 };

    // Ahora la lógica de presionar 'E' se maneja desde el jugador usando Raycast
    public void RecogerLlave()
    {
        if (!isKeyCollected)
        {
            isKeyCollected = true;
            Debug.Log("¡Llave recogida!");
            
            // Ya no completamos la tarea aquí, porque la tarea es abrir el locker, no tomar la llave.
            
            // Mostrar preguntas del GlobalQuizManager al recoger la llave
            if (GlobalQuizManager.Instance != null)
            {
                GlobalQuizManager.Instance.ShowNextChunk();
            }

            gameObject.SetActive(false); // Ocultar llave directamente
        }
    }
}
