using UnityEngine;

public class Ejemplo : MonoBehaviour
{
    private Animator boxAnimator;
    private bool isOpen = false;

    public string animationName = "Open";

    void Start()
    {
        boxAnimator = GetComponent<Animator>();
    }

    void OnMouseDown()
    {
        if (KeyItem.isKeyCollected && !isOpen)
        {
            // Debug.Log("¡Abriendo la caja con la llave!");
            
            if (boxAnimator != null)
            {
                boxAnimator.Play(animationName);
            }
            
            isOpen = true;
        }
        else if (!KeyItem.isKeyCollected)
        {
            // Debug.Log("Está cerrada. Necesitas encontrar la llave primero.");
        }
    }
}
