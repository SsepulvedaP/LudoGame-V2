using UnityEngine;
using System.Collections;

public class FuseBox : MonoBehaviour
{
    private bool isRepaired = false;

    [Header("Preguntas")]
    [Tooltip("IDs de las preguntas de motivación que salen al colocar el fusible")]
    public int[] motivationQuestionIds = new int[] { 4 };

    [Header("Visuales")]
    [Tooltip("Opcional: Arrastra aquí un objeto (como un cilindro) que represente el fusible ya colocado dentro de la caja")]
    public GameObject placedFuseVisual;

    void Start()
    {
        if (placedFuseVisual != null)
        {
            placedFuseVisual.SetActive(false); // Ocultar el fusible al principio
        }
    }

    public bool IsRepaired()
    {
        return isRepaired;
    }

    public void RepairBox()
    {
        if (!isRepaired)
        {
            StartCoroutine(RepairRoutine());
        }
    }

    IEnumerator RepairRoutine()
    {
        isRepaired = true;
        Debug.Log("Caja reparada");

        if (placedFuseVisual != null)
        {
            placedFuseVisual.SetActive(true); // Mostrar el fusible dentro
        }

        if (MotivationInGameUI.Instance != null && motivationQuestionIds != null && motivationQuestionIds.Length > 0)
        {
            bool questionsAnswered = false;
            
            MotivationInGameUI.Instance.ShowQuestions(motivationQuestionIds, () => {
                questionsAnswered = true;
            });
            
            yield return new WaitUntil(() => questionsAnswered);
        }

        // Aquí podrías encender luces o reproducir un sonido
    }
}
