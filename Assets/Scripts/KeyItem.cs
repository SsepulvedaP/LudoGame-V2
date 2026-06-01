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
            
            if (TaskManager.Instance != null)
                TaskManager.Instance.CompletarLlave();
            
            if (MotivationInGameUI.Instance != null && motivationQuestionIds != null && motivationQuestionIds.Length > 0)
            {
                // Muestra la secuencia de preguntas y cuando termina oculta la llave
                MotivationInGameUI.Instance.ShowQuestions(motivationQuestionIds, () => {
                    gameObject.SetActive(false); 
                });
            }
            else
            {
                gameObject.SetActive(false); // Ocultar llave directamente si no hay UI
            }
        }
    }
}
