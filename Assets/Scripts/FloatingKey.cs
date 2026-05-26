using UnityEngine;

public class FloatingKey : MonoBehaviour
{
    [Header("Configuración de Rotación")]
    [Tooltip("Velocidad a la que gira la llave en su propio eje.")]
    public float rotationSpeed = 100f;

    [Header("Configuración de Flotación")]
    [Tooltip("Qué tan alto y bajo sube la llave.")]
    public float floatAmplitude = 0.25f; 
    [Tooltip("Qué tan rápido hace el movimiento de subir y bajar.")]
    public float floatFrequency = 1f;   

    private Vector3 startPosition;

    void Start()
    {
        // Guardamos la posición inicial donde colocaste la llave en la escena
        startPosition = transform.position;
    }

    void Update()
    {
        // 1. ROTACIÓN: Gira la llave sobre el eje Y (Arriba) constantemente
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        // 2. FLOTACIÓN: Usamos una onda senoidal (Mathf.Sin) para un movimiento suave de arriba abajo
        float newY = startPosition.y + Mathf.Sin(Time.time * Mathf.PI * floatFrequency) * floatAmplitude;
        
        // Aplicamos la nueva posición manteniendo X y Z intactos
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}
