using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class BookSetup : MonoBehaviour
{
    [Tooltip("Lista de sprites para cada libro. Se asignarán en orden a los libros hijos.")]
    [SerializeField] private Sprite[] bookSprites;

    private void Awake()
    {
        // Se ejecuta automáticamente al iniciar la partida en el juego
        SetupBooks();
    }

    [ContextMenu("Setup Books Now")]
    public void SetupBooks()
    {
        SelectManager selector = FindFirstObjectByType<SelectManager>();
        if (selector == null)
        {
            Debug.LogWarning("No se encontró ningún SelectManager en la escena.");
        }

        int index = 0;
        foreach (Transform child in transform)
        {
            // 1. Establecer la etiqueta a "Selectable"
            child.gameObject.tag = "Selectable";

            // 2. Obtener o agregar BoxCollider
            BoxCollider boxCollider = child.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = child.gameObject.AddComponent<BoxCollider>();
            }

            // 3. Ajustar el BoxCollider a los límites reales de la malla
            AdjustColliderToBounds(child, boxCollider);

            // 4. Obtener o agregar PickableManager
            PickableManager pickable = child.GetComponent<PickableManager>();
            if (pickable == null)
            {
                pickable = child.gameObject.AddComponent<PickableManager>();
            }

            // 5. Configurar propiedades de PickableManager
            if (pickable != null)
            {
                pickable.Pickable = true;
                if (selector != null)
                {
                    pickable.Selector = selector;
                }

                if (bookSprites != null && index < bookSprites.Length)
                {
                    pickable.Sprite = bookSprites[index];
                    Debug.Log($"Libro '{child.name}' configurado con sprite: {bookSprites[index].name}");
                }
                else
                {
                    Debug.LogWarning($"Libro '{child.name}' configurado SIN sprite (índice fuera de rango o array de sprites vacío).");
                }
            }

            Debug.Log($"Libro '{child.name}' configurado con éxito. Tag: {child.gameObject.tag}, BoxCollider: {boxCollider != null}, PickableManager: {pickable != null}");

            index++;
        }

        Debug.Log($"Se configuraron {index} libros correctamente bajo {gameObject.name}.");

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(gameObject);
            foreach (Transform child in transform)
            {
                EditorUtility.SetDirty(child.gameObject);
                var pm = child.GetComponent<PickableManager>();
                if (pm != null) EditorUtility.SetDirty(pm);
                var col = child.GetComponent<BoxCollider>();
                if (col != null) EditorUtility.SetDirty(col);
            }
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }

    private void AdjustColliderToBounds(Transform book, BoxCollider boxCollider)
    {
        Renderer[] renderers = book.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            // Valores por defecto si no hay malla
            boxCollider.center = Vector3.zero;
            boxCollider.size = new Vector3(0.3f, 0.4f, 0.1f);
            return;
        }

        Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);
        bool hasBounds = false;

        foreach (var renderer in renderers)
        {
            if (renderer != null && renderer.enabled)
            {
                Bounds worldBounds = renderer.bounds;
                Vector3 center = worldBounds.center;
                Vector3 extents = worldBounds.extents;

                Vector3[] corners = new Vector3[8];
                corners[0] = center + new Vector3(-extents.x, -extents.y, -extents.z);
                corners[1] = center + new Vector3(-extents.x, -extents.y, extents.z);
                corners[2] = center + new Vector3(-extents.x, extents.y, -extents.z);
                corners[3] = center + new Vector3(-extents.x, extents.y, extents.z);
                corners[4] = center + new Vector3(extents.x, -extents.y, -extents.z);
                corners[5] = center + new Vector3(extents.x, -extents.y, extents.z);
                corners[6] = center + new Vector3(extents.x, extents.y, -extents.z);
                corners[7] = center + new Vector3(extents.x, extents.y, extents.z);

                if (!hasBounds)
                {
                    localBounds = new Bounds(book.InverseTransformPoint(corners[0]), Vector3.zero);
                    hasBounds = true;
                }

                for (int i = 0; i < 8; i++)
                {
                    localBounds.Encapsulate(book.InverseTransformPoint(corners[i]));
                }
            }
        }

        if (hasBounds)
        {
            boxCollider.center = localBounds.center;
            boxCollider.size = localBounds.size;
        }
    }
}
