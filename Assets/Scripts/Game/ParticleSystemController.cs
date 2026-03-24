using UnityEngine;
using Arcweave;
using Arcweave.Project;

public class ParticleSystemController : MonoBehaviour
{
    [Header("Arcweave Settings")]
    public ArcweavePlayer arcweavePlayer;
    
    [Header("Component Settings")]
    public string componentName = "Weather"; // Componente che contiene l'attributo
    public string weatherAttribute = "WeatherState"; // Attributo che controlla il tempo atmosferico
    
    [Header("Particle System")]
    public ParticleSystem rainParticleSystem; // Il Particle System per la pioggia

    private void Start()
    {
        if (arcweavePlayer == null)
        {
            arcweavePlayer = FindObjectOfType<ArcweavePlayer>();
            if (arcweavePlayer == null)
            {
                Debug.LogError("ArcweavePlayer non trovato!");
                return;
            }
        }

        arcweavePlayer.onProjectFinish += OnProjectFinish;

        var importer = FindObjectOfType<RuntimeArcweaveImporter>();
        if (importer != null)
        {
            importer.onImportSuccess.AddListener(OnImportSuccess);
        }

        UpdateParticleSystem();
    }

    private void OnImportSuccess()
    {
        UpdateParticleSystem();
    }

    private void OnProjectFinish(Arcweave.Project.Project project)
    {
        UpdateParticleSystem();
    }

    private void OnDestroy()
    {
        if (arcweavePlayer != null)
        {
            arcweavePlayer.onProjectFinish -= OnProjectFinish;
        }

        var importer = FindObjectOfType<RuntimeArcweaveImporter>();
        if (importer != null)
        {
            importer.onImportSuccess.RemoveListener(OnImportSuccess);
        }
    }

    public void UpdateParticleSystem()
    {
        if (rainParticleSystem == null)
        {
            Debug.LogWarning("Nessun Particle System assegnato!");
            return;
        }

        var component = FindComponentByName(componentName);
        if (component == null)
        {
            Debug.LogWarning($"Component '{componentName}' non trovato!");
            return;
        }

        var weatherStateAttribute = FindAttributeByName(component, weatherAttribute);
        if (weatherStateAttribute == null)
        {
            Debug.LogWarning($"Attribute '{weatherAttribute}' non trovato!");
            return;
        }

        string weatherState = weatherStateAttribute.data?.ToString();
        if (string.IsNullOrEmpty(weatherState))
        {
            Debug.LogError($"Valore dell'attributo '{weatherAttribute}' vuoto o non valido!");
            return;
        }

        // Controlla il Particle System in base allo stato del tempo
        if (weatherState.ToLower() == "rain")
        {
            if (!rainParticleSystem.isPlaying)
            {
                rainParticleSystem.Play();
                Debug.Log("Avviato Particle System (Pioggia)");
            }
        }
        else if (weatherState.ToLower() == "clear")
        {
            if (rainParticleSystem.isPlaying)
            {
                rainParticleSystem.Stop();
                Debug.Log("Fermato Particle System (Cielo sereno)");
            }
        }
    }

    private Arcweave.Project.Component FindComponentByName(string name)
    {
        if (arcweavePlayer?.aw?.Project == null)
        {
            Debug.LogError("Progetto Arcweave non inizializzato!");
            return null;
        }

        foreach (var component in arcweavePlayer.aw.Project.components)
        {
            if (component != null && component.Name == name)
            {
                return component;
            }
        }

        return null;
    }

    private Arcweave.Project.Attribute FindAttributeByName(Arcweave.Project.Component component, string attributeName)
    {
        if (component == null || component.Attributes == null)
        {
            return null;
        }

        foreach (var attribute in component.Attributes)
        {
            if (attribute != null && attribute.Name == attributeName)
            {
                return attribute;
            }
        }

        return null;
    }
} 