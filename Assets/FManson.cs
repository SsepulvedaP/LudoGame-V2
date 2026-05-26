using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        if (Input.GetKey(KeyCode.E) && PressE.activeInHierarchy)
        {
            Debug.Log("Puzzle");
            canvasPuzzle.SetActive(true);
            camara.SetActive(true);
            mouseLocking.m_MouseLook.SetCursorLock(false);
            PressE.SetActive(false);
        }

    }

    // Update is called once per frame

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
