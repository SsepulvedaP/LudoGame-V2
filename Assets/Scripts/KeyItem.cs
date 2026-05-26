using UnityEngine;

public class KeyItem : MonoBehaviour
{
    public static bool isKeyCollected = false;

    [Tooltip("ID de la pregunta de motivación en la base de datos (ej. 26)")]
    public int motivationQuestionId = 26;

    // Ahora la lógica de presionar 'E' se maneja desde el jugador usando Raycast
    public void RecogerLlave()
    {
        if (!isKeyCollected)
        {
            isKeyCollected = true;
            Debug.Log("¡Llave recogida!");
            
            if (MotivationInGameUI.Instance != null)
            {
                // Muestra la pregunta y cuando termina oculta la llave
                MotivationInGameUI.Instance.ShowQuestion(motivationQuestionId, () => {
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
