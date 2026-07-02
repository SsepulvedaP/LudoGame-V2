using UnityEngine;

public class GameControl : MonoBehaviour
{
    [SerializeField]
    private Transform[] pictures;

    [SerializeField]
    private GameObject winText, camara, character, foto;
    [SerializeField] private UnityStandardAssets.Characters.FirstPerson.FirstPersonController mouseLocking;

    [Header("Tras el puzzle: cuestionario Ludo (Bartle)")]
    [Tooltip("Panel raíz del cuestionario (normalmente empieza desactivado). Se activa al completar el puzzle.")]
    [SerializeField] private GameObject bartlePanelRoot;
    [SerializeField] private BartleQuestionnaireUI bartleQuestionnaire;

    public static bool youWin;

    private const int ExpectedPieceCount = 4;

    void Start()
    {
        if (winText != null)
        {
            winText.SetActive(false);
        }

        youWin = false;

        if (pictures == null || pictures.Length < ExpectedPieceCount)
        {
            Debug.LogWarning(
                $"[GameControl] Asigna en el inspector un array 'pictures' con al menos {ExpectedPieceCount} elementos. Ahora: {(pictures == null ? 0 : pictures.Length)}.",
                this);
        }

        if (mouseLocking != null && mouseLocking.m_MouseLook == null)
        {
            Debug.LogWarning(
                "[GameControl] FirstPersonController sin MouseLook asignado; al ganar no se podrá desbloquear el cursor desde aquí.",
                this);
        }
    }

    void Update()
    {
        if (youWin)
        {
            return;
        }

        if (!AreAllPiecesAligned())
        {
            return;
        }

        youWin = true;
        
        // Completar la tarea del cuadro en el Checklist
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.CompletarCuadro();
        }

        bool showBartle = true; // Forzamos a que muestre el quiz global
        if (showBartle)
        {
            if (winText != null)
            {
                winText.SetActive(false);
            }

            if (GlobalQuizManager.Instance != null)
            {
                GlobalQuizManager.Instance.ShowNextChunk();
            }
        }
        else if (winText != null)
        {
            winText.SetActive(true);
           
        }

        if (character != null)
        {
            character.SetActive(true);
        }

        if (camara != null)
        {
            Destroy(camara);
        }
    }

    private bool AreAllPiecesAligned()
    {
        if (pictures == null || pictures.Length < ExpectedPieceCount)
        {
            return false;
        }

        for (int i = 0; i < ExpectedPieceCount; i++)
        {
            Transform t = pictures[i];
            if (t == null)
            {
                return false;
            }

            if (t.rotation.z != 0f)
            {
                return false;
            }
        }
        foto.SetActive(false);
        return true;
    }
}
