using UnityEngine;

public class IntroManager : MonoBehaviour
{
    [Header("Paneles de Introducción")]
    public GameObject panel1;
    public GameObject panel2;

    [Header("Menú Principal / Registro")]
    public GameObject menuPrincipal; // Aquí arrastrarás tu Panel_RegisterUser

    void Start()
    {
        // Al iniciar la escena, mostramos el primer panel y ocultamos el resto
        if (panel1 != null) panel1.SetActive(true);
        if (panel2 != null) panel2.SetActive(false);
        if (menuPrincipal != null) menuPrincipal.SetActive(false);
    }

    public void MostrarPanel2()
    {
        if (panel1 != null) panel1.SetActive(false);
        if (panel2 != null) panel2.SetActive(true);
    }

    public void MostrarMenuPrincipal()
    {
        if (panel2 != null) panel2.SetActive(false);
        if (menuPrincipal != null) menuPrincipal.SetActive(true);
    }
}
