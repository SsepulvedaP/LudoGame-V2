using UnityEngine;

public class InteractableHighlighter : MonoBehaviour
{
    [Header("Highlight Settings")]
    public bool enableLight = true;
    public Color lightColor = new Color(1f, 0.9f, 0.6f); // Suavizado
    public float pulseSpeed = 4f;
    
    [Header("Light Specifics")]
    public float minIntensity = 0f;
    public float maxIntensity = 5f;
    public float lightRange = 1.5f;
    public Vector3 lightLocalOffset = Vector3.zero;

    [Header("Material Emission (Optional)")]
    public bool enableMaterialEmission = true;
    public float minEmission = 0f;
    public float maxEmission = 0.15f;

    private Light pointLight;
    private Renderer[] renderers;
    private MaterialPropertyBlock propBlock;
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        if (enableLight)
        {
            GameObject lightObj = new GameObject("InteractableHighlightLight");
            lightObj.transform.SetParent(this.transform);
            
            Collider col = GetComponent<Collider>();
            Renderer[] localRenderers = GetComponentsInChildren<Renderer>();
            
            if (col != null)
            {
                lightObj.transform.position = col.bounds.center;
            }
            else if (localRenderers != null && localRenderers.Length > 0)
            {
                Bounds b = localRenderers[0].bounds;
                for (int i = 1; i < localRenderers.Length; i++)
                {
                    b.Encapsulate(localRenderers[i].bounds);
                }
                lightObj.transform.position = b.center;
            }
            else
            {
                lightObj.transform.localPosition = Vector3.zero;
            }

            pointLight = lightObj.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = lightColor;
            pointLight.range = lightRange;
            pointLight.intensity = minIntensity;
            pointLight.shadows = LightShadows.None;
            
            // Aplicar el offset manual que el usuario puede configurar en el Inspector
            lightObj.transform.localPosition += lightLocalOffset;
            
            // Un pequeño offset hacia atrás por si está pegado a la pared
            lightObj.transform.position -= transform.forward * 0.2f;
        }

        if (enableMaterialEmission)
        {
            renderers = GetComponentsInChildren<Renderer>();
            propBlock = new MaterialPropertyBlock();
            
            // Forzar la activación del keyword de emisión en los materiales
            if (renderers != null)
            {
                foreach (var r in renderers)
                {
                    if (r == null) continue;
                    foreach (var mat in r.materials)
                    {
                        if (mat != null)
                        {
                            mat.EnableKeyword("_EMISSION");
                            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                        }
                    }
                }
            }
        }
    }

    private void Update()
    {
        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;

        if (enableLight && pointLight != null)
        {
            pointLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
        }

        if (enableMaterialEmission && renderers != null && propBlock != null)
        {
            float emission = Mathf.Lerp(minEmission, maxEmission, t);
            Color finalEmissionColor = lightColor * emission;

            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(propBlock);
                propBlock.SetColor(EmissionColorID, finalEmissionColor);
                r.SetPropertyBlock(propBlock);
            }
        }
    }

    private void OnDisable()
    {
        if (enableMaterialEmission && renderers != null && propBlock != null)
        {
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(propBlock);
                propBlock.SetColor(EmissionColorID, Color.black);
                r.SetPropertyBlock(propBlock);
            }
        }
    }
}
