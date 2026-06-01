using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FManson : MonoBehaviour
{
    [SerializeField] private GameObject PressE, canvasPuzzle, camara;
    [SerializeField] private UnityStandardAssets.Characters.FirstPerson.FirstPersonController mouseLocking;

    void Awake()
    {
        PressE.SetActive(false);
        canvasPuzzle.SetActive(false);
    }

    private void Update()
    {
        bool keyEPressed = false;
        if (Keyboard.current != null)
        {
            keyEPressed = Keyboard.current.eKey.isPressed;
        }

        if (keyEPressed && PressE != null && PressE.activeInHierarchy)
        {
            Debug.Log("Puzzle");
            canvasPuzzle.SetActive(true);
            camara.SetActive(true);
            mouseLocking.m_MouseLook.SetCursorLock(false);
            PressE.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("FAMILYM");
        PressE.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        PressE.SetActive(false);
    }

    public void DisablePermanently()
    {
        enabled = false;

        if (PressE != null)
        {
            Destroy(PressE);
            PressE = null;
        }
    }
}
