using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class UserRegistrationClient : MonoBehaviour
{
    [Header("API")]
    public string baseUrl = "https://ludo-api-48780a3730ba.herokuapp.com/api";

    [Header("Flujo post-registro")]
    [Tooltip("Referencia al IntroManager para mostrar los paneles de introducción tras registrarse. Si es null, carga la escena directamente.")]
    public IntroManager introManager;

    [Header("UI")]
    public TMP_InputField inputName;
    public TMP_InputField inputDocument;
    public TMP_Dropdown dropdownCountry;

    [Header("Area UI")]
    public TMP_Dropdown dropdownArea;
    [FormerlySerializedAs("inputOtherPosition")]
    public TMP_InputField inputOtherArea;
    [FormerlySerializedAs("otherPositionSection")]
    public GameObject otherAreaSection;

    [Header("Tras registrarse con éxito")]
    [Tooltip("Escena que se carga al terminar el registro (nombre exacto, como en Build Settings; p. ej. Level 1). El Bartle puedes montarlo dentro de esa escena.")]
    public string nextSceneName = "Level 1";

    private readonly List<string> countryValues = new List<string>();
    private readonly Dictionary<string, int> areaIdsByTitle = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> fallbackAreaOptions = new List<string>
    {
        "Ti",
        "Comunicaciones",
        "Operaciones",
        "Otro"
    };

    private readonly Dictionary<string, string> countryCodeToBackendValue = new Dictionary<string, string>
    {
        { "CO", "Colombia" },
        { "US", "USA" },
        { "MX", "Mexico" },
        { "PE", "Peru" },
        { "AR", "Argentina" },
        { "BR", "Brazil" },
        { "CA", "Canada" },
        { "CL", "Chile" },
        { "EC", "Ecuador" },
        { "VE", "Venezuela" },
        { "BO", "Bolivia" },
        { "PY", "Paraguay" },
        { "UY", "Uruguay" },
        { "PA", "Panama" },
        { "CR", "CostaRica" },
        { "CU", "Cuba" },
        { "GT", "Guatemala" },
        { "HN", "Honduras" },
        { "SV", "ElSalvador" },
        { "NI", "Nicaragua" },
        { "DO", "RepublicaDominicana" },
        { "HT", "Haiti" },
        { "PR", "PuertoRico" }
    };

    [Serializable]
    private class CreateUserRequest
    {
        public string name;
        public string token;
        public string document;
        public string country;
        public int positionId;
    }

    [Serializable]
    private class CreateUserResponse
    {
        public int id;
        public string name;
        public string token;
        public string country;
        public int positionId;
    }

    [Serializable]
    private class CreatePositionRequest
    {
        public string title;
    }

    [Serializable]
    private class CountryApiList
    {
        public CountryApiItem[] items;
    }

    [Serializable]
    private class CountryApiItem
    {
        public CountryApiName name;
        public string cca2;
    }

    [Serializable]
    private class CountryApiName
    {
        public string common;
    }

    [Serializable]
    private class PositionApiList
    {
        public PositionApiItem[] items;
    }

    [Serializable]
    private class PositionApiItem
    {
        public int id;
        public string title;
    }

    private void Start()
    {
        ConfigureAreaDropdown();
        StartCoroutine(LoadCountries());
        StartCoroutine(LoadPositions());
    }

    private void OnDestroy()
    {
        if (dropdownArea != null)
        {
            dropdownArea.onValueChanged.RemoveListener(OnAreaChanged);
        }
    }

    private IEnumerator LoadCountries()
    {
        string countriesUrl = "https://restcountries.com/v3.1/region/americas?fields=name,cca2";

        using UnityWebRequest request = UnityWebRequest.Get(countriesUrl);
        yield return request.SendWebRequest();


        if (request.result != UnityWebRequest.Result.Success)
        {
            LoadFallbackCountries();
            yield break;
        }

        string wrappedJson = "{\"items\":" + request.downloadHandler.text + "}";
        CountryApiList response = JsonUtility.FromJson<CountryApiList>(wrappedJson);

        if (response == null || response.items == null)
        {
            Debug.LogWarning("[UserRegistrationClient] La respuesta de países no es válida, cargando países por defecto.");
            LoadFallbackCountries();
            yield break;
        }

        dropdownCountry.ClearOptions();
        countryValues.Clear();

        List<string> options = new List<string>();

        foreach (CountryApiItem country in response.items)
        {
            if (!countryCodeToBackendValue.TryGetValue(country.cca2, out string backendValue))
            {
                continue;
            }

            countryValues.Add(backendValue);
            options.Add(country.name.common);
        }

        if (options.Count == 0)
        {
            LoadFallbackCountries();
            yield break;
        }

        dropdownCountry.AddOptions(options);
    }

    private void ConfigureAreaDropdown()
    {
        if (dropdownArea == null)
        {
            SetOtherPositionSectionActive(false);
            return;
        }

        dropdownArea.onValueChanged.RemoveListener(OnAreaChanged);
        dropdownArea.ClearOptions();
        dropdownArea.AddOptions(new List<string> { "Cargando áreas…" });
        dropdownArea.onValueChanged.AddListener(OnAreaChanged);
        OnAreaChanged(dropdownArea.value);
    }

    private void OnAreaChanged(int value)
    {
        SetOtherPositionSectionActive(IsOtherAreaSelected());
    }

    private bool IsOtherAreaSelected()
    {
        if (dropdownArea == null || dropdownArea.options.Count == 0)
        {
            return false;
        }

        return dropdownArea.options[dropdownArea.value].text.Equals("Otro", StringComparison.OrdinalIgnoreCase);
    }

    private void SetOtherPositionSectionActive(bool active)
    {
        if (otherAreaSection != null)
        {
            otherAreaSection.SetActive(active);
        }

        if (inputOtherArea != null)
        {
            inputOtherArea.interactable = active;
        }
    }

    private IEnumerator LoadPositions()
    {
        string positionsUrl = $"{baseUrl}/positions/";

        using UnityWebRequest request = UnityWebRequest.Get(positionsUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[UserRegistrationClient] Error cargando posiciones: " + request.error);
            ApplyAreaDropdownFromList(fallbackAreaOptions);
            yield break;
        }

        string raw = request.downloadHandler.text;
        if (string.IsNullOrWhiteSpace(raw))
        {
            ApplyAreaDropdownFromList(fallbackAreaOptions);
            yield break;
        }

        string wrappedJson = "{\"items\":" + raw + "}";
        PositionApiList response = JsonUtility.FromJson<PositionApiList>(wrappedJson);

        areaIdsByTitle.Clear();

        if (response?.items == null || response.items.Length == 0)
        {
            ApplyAreaDropdownFromList(fallbackAreaOptions);
            yield break;
        }

        List<PositionApiItem> sorted = new List<PositionApiItem>(response.items);
        sorted.Sort((a, b) => a.id.CompareTo(b.id));

        foreach (PositionApiItem position in sorted)
        {
            if (position == null || position.id <= 0 || string.IsNullOrWhiteSpace(position.title))
            {
                continue;
            }

            areaIdsByTitle[position.title] = position.id;
        }

        List<string> labels = new List<string>();
        foreach (PositionApiItem position in sorted)
        {
            if (position == null || position.id <= 0 || string.IsNullOrWhiteSpace(position.title))
            {
                continue;
            }

            labels.Add(position.title.Trim());
        }

        if (labels.Count == 0)
        {
            ApplyAreaDropdownFromList(fallbackAreaOptions);
            yield break;
        }

        labels.Add("Otro");
        ApplyAreaDropdownFromList(labels);
    }

    /// <summary>
    /// Puebla el dropdown de área y mantiene la lógica de "Otro".
    /// </summary>
    private void ApplyAreaDropdownFromList(List<string> optionLabels)
    {
        if (dropdownArea == null || optionLabels == null || optionLabels.Count == 0)
        {
            return;
        }

        dropdownArea.onValueChanged.RemoveListener(OnAreaChanged);
        dropdownArea.ClearOptions();
        dropdownArea.AddOptions(new List<string>(optionLabels));
        dropdownArea.value = 0;
        dropdownArea.RefreshShownValue();
        dropdownArea.onValueChanged.AddListener(OnAreaChanged);
        OnAreaChanged(dropdownArea.value);
    }

    private void LoadFallbackCountries()
    {

        dropdownCountry.ClearOptions();
        countryValues.Clear();

        dropdownCountry.AddOptions(new List<string>
        {
            "Colombia",
            "USA",
            "Mexico",
            "Peru",
            "Argentina",
            "Brazil",
            "Canada",
            "Chile",
            "Ecuador",
            "Venezuela",
            "Bolivia",
            "Paraguay",
            "Uruguay",
            "Panama",
            "CostaRica",
            "Cuba",
            "Guatemala",
            "Honduras",
            "ElSalvador",
            "Nicaragua",
            "RepublicaDominicana",
            "Haiti",
            "PuertoRico"
        });

        countryValues.AddRange(new List<string>
        {
            "Colombia",
            "USA",
            "Mexico",
            "Peru",
            "Argentina",
            "Brazil",
            "Canada",
            "Chile",
            "Ecuador",
            "Venezuela",
            "Bolivia",
            "Paraguay",
            "Uruguay",
            "Panama",
            "CostaRica",
            "Cuba",
            "Guatemala",
            "Honduras",
            "ElSalvador",
            "Nicaragua",
            "RepublicaDominicana",
            "Haiti",
            "PuertoRico"
        });

    }


    public void RegisterUserButton()
    {
        StartCoroutine(RegisterUser());
    }

    private string GetSelectedAreaTitle()
    {
        if (IsOtherAreaSelected())
        {
            return inputOtherArea != null ? inputOtherArea.text.Trim() : string.Empty;
        }

        if (dropdownArea == null || dropdownArea.options.Count == 0)
        {
            return string.Empty;
        }

        return dropdownArea.options[dropdownArea.value].text.Trim();
    }

    private int FindLoadedAreaId(string title)
    {
        if (areaIdsByTitle.TryGetValue(title, out int areaId))
        {
            return areaId;
        }

        return 0;
    }

    private IEnumerator CreateArea(string title, Action<int> onCreated)
    {
        int existingAreaId = FindLoadedAreaId(title);
        if (existingAreaId > 0)
        {
            onCreated?.Invoke(existingAreaId);
            yield break;
        }

        CreatePositionRequest body = new CreatePositionRequest
        {
            title = title
        };

        string json = JsonUtility.ToJson(body);
        string positionsUrl = $"{baseUrl}/positions/";

        // Debug.Log("[UserRegistrationClient] Creando área en: " + positionsUrl);
        // Debug.Log("[UserRegistrationClient] Payload área: " + json);

        using UnityWebRequest request = new UnityWebRequest(positionsUrl, "POST");
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(jsonBytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        // Debug.Log("[UserRegistrationClient] Crear área HTTP " + request.responseCode);
        // Debug.Log("[UserRegistrationClient] Crear área response body: " + request.downloadHandler.text);

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[UserRegistrationClient] Error creando área: " + request.error);
            onCreated?.Invoke(0);
            yield break;
        }

        PositionApiItem createdPosition = JsonUtility.FromJson<PositionApiItem>(request.downloadHandler.text);
        if (createdPosition == null || createdPosition.id <= 0)
        {
            Debug.LogError("[UserRegistrationClient] El backend no devolvió un ID de área válido.");
            onCreated?.Invoke(0);
            yield break;
        }

        areaIdsByTitle[createdPosition.title] = createdPosition.id;
        onCreated?.Invoke(createdPosition.id);
    }

    private IEnumerator RegisterUser()
    {
        if (string.IsNullOrWhiteSpace(inputName.text))
        {
            Debug.LogError("Escribe un nombre.");
            yield break;
        }

        if (inputDocument != null && string.IsNullOrWhiteSpace(inputDocument.text))
        {
            Debug.LogError("Escribe un documento.");
            yield break;
        }

        int areaId = 0;
        string selectedArea = GetSelectedAreaTitle();

        if (string.IsNullOrWhiteSpace(selectedArea))
        {
            if (IsOtherAreaSelected())
            {
                Debug.LogError("[UserRegistrationClient] Escribe la nueva área.");
                yield break;
            }

            Debug.LogError("[UserRegistrationClient] Selecciona un área válida.");
            yield break;
        }

        yield return CreateArea(selectedArea, createdAreaId => areaId = createdAreaId);

        if (areaId <= 0)
        {
            yield break;
        }

        string selectedCountry = countryValues[dropdownCountry.value];

        CreateUserRequest body = new CreateUserRequest
        {
            name = inputName.text,
            token = Guid.NewGuid().ToString(),
            document = inputDocument != null ? inputDocument.text : "",
            country = selectedCountry,
            positionId = areaId
        };

        string json = JsonUtility.ToJson(body);
        string usersUrl = $"{baseUrl}/users/";

        // Debug.Log("[UserRegistrationClient] Registrando usuario en: " + usersUrl);
        // Debug.Log("[UserRegistrationClient] Área enviada al backend: " + selectedArea);
        // Debug.Log("[UserRegistrationClient] País seleccionado UI: " + dropdownCountry.options[dropdownCountry.value].text);
        // Debug.Log("[UserRegistrationClient] País enviado al backend: " + selectedCountry);
        // Debug.Log("[UserRegistrationClient] AreaId enviado como positionId: " + areaId);
        // Debug.Log("[UserRegistrationClient] Payload: " + json);

        using UnityWebRequest request = new UnityWebRequest(usersUrl, "POST");
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(jsonBytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        // Debug.Log("[UserRegistrationClient] Registro HTTP " + request.responseCode);
        // Debug.Log("[UserRegistrationClient] Registro response body: " + request.downloadHandler.text);

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[UserRegistrationClient] Error registrando usuario: " + request.error);
            yield break;
        }

        // Debug.Log("[UserRegistrationClient] Usuario registrado correctamente.");

        CreateUserResponse created = JsonUtility.FromJson<CreateUserResponse>(request.downloadHandler.text);
        if (created != null && created.id > 0)
        {
            UserSession.Save(created.id, created.token, created.name);
            // Debug.Log("[UserRegistrationClient] user_id guardado en sesión: " + created.id);
        }
        else
        {
            Debug.LogWarning("[UserRegistrationClient] La respuesta no incluyó id de usuario; Bartle/API posteriores no tendrán user_id.");
        }

        // Si hay un IntroManager, mostrar los paneles de introducción antes de cargar la escena.
        // El IntroManager será quien llame a SceneManager.LoadScene al final del Panel2.
        if (introManager != null)
        {
            introManager.OnRegistroExitoso();
        }
        else
        {
            // Fallback: cargar la escena directamente si no hay IntroManager asignado
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
