using UnityEngine;
using Arcweave;
using Arcweave.Project;

public abstract class ArcweaveAttributeHandler : MonoBehaviour
{
    [Header("Arcweave Settings")]
    public ArcweavePlayer arcweavePlayer;
    public RuntimeArcweaveImporter arcweaveImporter;

    [Header("Component Settings")]
    [Tooltip("The name of the Arcweave component to search for")]
    public string componentName = "SceneSettings";

    [Header("Attribute Settings")]
    [Tooltip("The name of the attribute to search for")]
    public string attributeName = "Color";

    protected virtual void Start()
    {
        Initialize();
        if (arcweaveImporter != null)
        {
            arcweaveImporter.onImportSuccess.AddListener(OnImportSuccess);
        }
        if (arcweavePlayer != null)
        {
            arcweavePlayer.onProjectFinish += OnProjectFinish;
        }
    }

    private void Initialize()
    {
        if (arcweavePlayer == null)
        {
            arcweavePlayer = FindObjectOfType<ArcweavePlayer>();
            if (arcweavePlayer == null)
            {
                Debug.LogError("ArcweavePlayer not found!");
                return;
            }
        }

        UpdateFromAttributes();
    }

    private void OnImportSuccess()
    {
        UpdateFromAttributes();
    }

    private void OnProjectFinish(Arcweave.Project.Project project)
    {
        UpdateFromAttributes();
    }

    private void UpdateFromAttributes()
    {
        if (arcweavePlayer == null || arcweavePlayer.aw == null || arcweavePlayer.aw.Project == null)
        {
            Debug.LogError("Arcweave project not initialized or invalid object!");
            return;
        }

        var component = FindComponentByName(componentName);
        if (component == null)
        {
            Debug.LogWarning($"Component '{componentName}' not found!");
            return;
        }

        var attribute = FindAttributeByName(component, attributeName);
        if (attribute != null)
        {
            string attributeValue = attribute.data?.ToString();
            if (!string.IsNullOrEmpty(attributeValue))
            {
                ApplyAttributeValue(attributeValue);
            }
        }
    }

    protected abstract void ApplyAttributeValue(string value);

    private Arcweave.Project.Component FindComponentByName(string name)
    {
        if (arcweavePlayer?.aw?.Project == null)
        {
            Debug.LogError("Arcweave project not initialized!");
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

    private void OnDestroy()
    {
        if (arcweavePlayer != null)
        {
            arcweavePlayer.onProjectFinish -= OnProjectFinish;
        }
        if (arcweaveImporter != null)
        {
            arcweaveImporter.onImportSuccess.RemoveListener(OnImportSuccess);
        }
    }
} 