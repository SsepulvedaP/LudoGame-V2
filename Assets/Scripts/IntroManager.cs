using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroManager : MonoBehaviour
{
    [Header("Paneles de Introducción")]
    public GameObject panel1;
    public GameObject panel2;

    [Header("Menú Principal / Registro")]
    public GameObject menuPrincipal; // Panel_RegisterUser

    [Header("Escena a cargar al finalizar la intro")]
    public string nextSceneName = "Level 1";

    void Start()
    {
        // Flujo: Registro → Panel1 → Panel2 → Juego
        if (menuPrincipal != null) menuPrincipal.SetActive(true);
        if (panel1 != null) panel1.SetActive(false);
        if (panel2 != null) panel2.SetActive(false);
    }

    /// <summary>
    /// Llamar este método desde el botón "Registrarse" del Panel_RegisterUser
    /// DESPUÉS de que el registro en la API haya sido exitoso.
    /// </summary>
    public void OnRegistroExitoso()
    {
        if (menuPrincipal != null) menuPrincipal.SetActive(false);
        if (panel1 != null) panel1.SetActive(true);
        if (panel2 != null) panel2.SetActive(false);
    }

    /// <summary>
    /// Llamar desde el botón "Siguiente" del Panel1
    /// </summary>
    public void MostrarPanel2()
    {
        if (panel1 != null) panel1.SetActive(false);
        if (panel2 != null) panel2.SetActive(true);
    }

    /// <summary>
    /// Llamar desde el botón "Empezar" / "Continuar" del Panel2
    /// </summary>
    public void IrAlJuego()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}
