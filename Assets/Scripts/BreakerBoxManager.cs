using UnityEngine;
using UnityEngine.Events;

public class BreakerBoxManager : MonoBehaviour
{
    [Header("Configuración del Puzzle")]
    [Tooltip("IDs de los interruptores que DEBEN estar encendidos para ganar (ej. 1, 3, 6)")]
    public int[] correctSwitchIds = new int[] { 1, 3, 6 };
    
    [Header("Preguntas a mostrar al ganar")]
    public int[] motivationQuestionIds = new int[] { 5, 6, 7 };

    [Header("Eventos Opcionales")]
    public UnityEvent onPuzzleSolved;

    private BreakerSwitch[] allSwitches;
    private bool isSolved = false;

    public UnityEvent OnAccessGranted = new UnityEvent();

    void Start()
    {
        if (gameObject.GetComponent<InteractableHighlighter>() == null)
        {
            gameObject.AddComponent<InteractableHighlighter>();
        }

        // Encuentra todos los interruptores que sean hijos de este objeto
        allSwitches = GetComponentsInChildren<BreakerSwitch>();
    }

    public void CheckCombination()
    {
        if (isSolved) return;

        bool allCorrect = true;

        foreach (var sw in allSwitches)
        {
            // ¿Este interruptor DEBERÍA estar encendido?
            bool shouldBeOn = false;
            foreach (var id in correctSwitchIds)
            {
                if (sw.switchId == id)
                {
                    shouldBeOn = true;
                    break;
                }
            }

            // Si el estado actual no coincide con lo que debería ser, fallamos
            if (sw.IsOn() != shouldBeOn)
            {
                allCorrect = false;
                break;
            }
        }

        if (allCorrect)
        {
            SolvePuzzle();
        }
    }

    private void SolvePuzzle()
    {
        isSolved = true;
        // Debug.Log("¡Puzzle de la caja eléctrica resuelto!");

        OnAccessGranted?.Invoke();
        onPuzzleSolved?.Invoke();

        if (MotivationInGameUI.Instance != null && motivationQuestionIds != null && motivationQuestionIds.Length > 0)
        {
            MotivationInGameUI.Instance.ShowQuestions(motivationQuestionIds, null);
        }
    }
}
