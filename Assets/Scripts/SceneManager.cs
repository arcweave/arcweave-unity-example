using UnityEngine;
using Arcweave;
using Arcweave.Project;

public class SceneInitializer : MonoBehaviour
{
    [Header("Arcweave Settings")]
    public ArcweavePlayer arcweavePlayer;
    
    [Header("Component Settings")]
    public string componentName = "SceneManager";
    public string attributeName = "Time"; // Usiamo direttamente l'attributo "Time"
    
    [Header("Particle System Settings")]
    public ParticleSystem targetParticleSystem;
    public string particleControlAttribute = "ParticleState"; // Nome dell'attributo che controlla il Particle System

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

        FindComponentAndAttribute();
        ControlParticleSystem();
    }

    private void FindComponentAndAttribute()
    {
        // Verifica se l'oggetto ArcweavePlayer è valido
        if (arcweavePlayer == null || arcweavePlayer.aw == null || arcweavePlayer.aw.Project == null)
        {
            Debug.LogError("Progetto Arcweave non inizializzato o oggetto non valido!");
            return;
        }

        // Trova il componente
        var component = FindComponentByName(componentName);
        if (component == null)
        {
            Debug.LogWarning($"Component '{componentName}' non trovato!");
            return;
        }

        // Trova l'attributo "Time"
        var timeAttribute = FindAttributeByName(component, attributeName);
        if (timeAttribute == null)
        {
            Debug.LogWarning($"Attribute '{attributeName}' non trovato!");
            return;
        }

        // Usa il valore dell'attributo
        string attributeValue = timeAttribute.data?.ToString();
        if (string.IsNullOrEmpty(attributeValue))
        {
            Debug.LogError($"Valore dell'attributo '{attributeName}' vuoto o non valido!");
            return;
        }

        // Stampa il valore dell'attributo
        Debug.Log($"Valore dell'attributo '{attributeName}': {attributeValue}");
    }

    /// <summary>
    /// Trova un componente Arcweave per nome
    /// </summary>
    private Arcweave.Project.Component FindComponentByName(string name)
    {
        if (arcweavePlayer?.aw?.Project == null)
        {
            Debug.LogError("Progetto Arcweave non inizializzato!");
            return null;
        }

        // Itera attraverso tutti i componenti del progetto
        foreach (var component in arcweavePlayer.aw.Project.components)
        {
            if (component != null && component.Name == name)
            {
                return component;
            }
        }

        return null;
    }

    /// <summary>
    /// Trova un attributo per nome
    /// </summary>
    private Arcweave.Project.Attribute FindAttributeByName(Arcweave.Project.Component component, string attributeName)
    {
        if (component == null || component.Attributes == null)
        {
            return null;
        }

        // Itera attraverso tutti gli attributi del componente
        foreach (var attribute in component.Attributes)
        {
            if (attribute != null && attribute.Name == attributeName)
            {
                return attribute;
            }
        }

        return null;
    }

    private void ControlParticleSystem()
    {
        if (targetParticleSystem == null)
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

        var particleAttribute = FindAttributeByName(component, particleControlAttribute);
        if (particleAttribute == null)
        {
            Debug.LogWarning($"Attribute '{particleControlAttribute}' non trovato!");
            return;
        }

        string particleState = particleAttribute.data?.ToString();
        if (string.IsNullOrEmpty(particleState))
        {
            Debug.LogError($"Valore dell'attributo '{particleControlAttribute}' vuoto o non valido!");
            return;
        }

        // Controlla il Particle System in base al valore dell'attributo
        if (particleState.ToLower() == "true" || particleState == "1")
        {
            if (!targetParticleSystem.isPlaying)
            {
                targetParticleSystem.Play();
            }
        }
        else
        {
            if (targetParticleSystem.isPlaying)
            {
                targetParticleSystem.Stop();
            }
        }

        Debug.Log($"Particle System stato: {targetParticleSystem.isPlaying}");
    }
} 