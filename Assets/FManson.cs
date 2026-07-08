using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FManson : MonoBehaviour
{
    [SerializeField] private GameObject PressE, canvasPuzzle, camara;
    [SerializeField] private UnityStandardAssets.Characters.FirstPerson.FirstPersonController mouseLocking;

    private bool isPlayerInTrigger = false;

    void Awake()
    {
        if (PressE != null) PressE.SetActive(false);
        if (canvasPuzzle != null) canvasPuzzle.SetActive(false);
    }

    private void Update()
    {
        if (isPlayerInTrigger)
        {
            PlayerInteraction.GlobalHoverText = "[ Presiona 'E' para interactuar ]";
        }

        bool keyEPressed = false;
        if (Keyboard.current != null)
        {
            keyEPressed = Keyboard.current.eKey.wasPressedThisFrame; // Cambiado a wasPressedThisFrame para mejor respuesta
        }

        if (keyEPressed && isPlayerInTrigger)
        {
            Debug.Log("Puzzle");
            if (canvasPuzzle != null) canvasPuzzle.SetActive(true);
            if (camara != null) camara.SetActive(true);
            if (mouseLocking != null) mouseLocking.m_MouseLook.SetCursorLock(false);
            
            isPlayerInTrigger = false;
            
            if (PressE != null) PressE.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("FAMILYM");
        isPlayerInTrigger = true;
    }

    private void OnTriggerExit(Collider other)
    {
        isPlayerInTrigger = false;
    }

    public void DisablePermanently()
    {
        enabled = false;
        isPlayerInTrigger = false;

        if (PressE != null)
        {
            Destroy(PressE);
            PressE = null;
        }
    }
}
