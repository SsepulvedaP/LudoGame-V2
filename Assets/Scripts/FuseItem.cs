using UnityEngine;

public class FuseItem : MonoBehaviour
{
    public static bool isFuseCollected = false;

    [Tooltip("IDs de las preguntas de motivación que salen al recoger el fusible")]
    public int[] motivationQuestionIds = new int[] { 3 }; 

    public void CollectFuse()
    {
        if (!isFuseCollected)
        {
            isFuseCollected = true;
            Debug.Log("¡Fusible recogido!");
            
            if (MotivationInGameUI.Instance != null && motivationQuestionIds != null && motivationQuestionIds.Length > 0)
            {
                MotivationInGameUI.Instance.ShowQuestions(motivationQuestionIds, () => {
                    gameObject.SetActive(false); 
                });
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
