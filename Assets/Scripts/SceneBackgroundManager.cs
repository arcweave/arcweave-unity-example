using UnityEngine;
using Arcweave;
using Arcweave.Project;

public class SceneBackgroundManager : MonoBehaviour
{
    [Header("Arcweave Settings")]
    public ArcweavePlayer arcweavePlayer;
    public RuntimeArcweaveImporter arcweaveImporter;

    [Header("Component Settings")]
    [Tooltip("The name of the Arcweave component to search for")]
    public string componentName = "SceneSettings";

    [Header("Attribute Settings")]
    [Tooltip("The name of the attribute for time (Day/Night)")]
    public string timeAttribute = "Time";

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            Debug.LogError("SceneBackgroundManager must be attached to a GameObject with a Camera component!");
            return;
        }

        InitializeBackground();

        if (arcweaveImporter != null)
        {
            arcweaveImporter.onImportSuccess.AddListener(OnImportSuccess);
        }

        if (arcweavePlayer != null)
        {
            arcweavePlayer.onProjectFinish += OnProjectFinish;
        }
    }

    private void OnImportSuccess()
    {
        UpdateBackgroundFromAttributes();
    }

    private void InitializeBackground()
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

        UpdateBackgroundFromAttributes();
    }

    private void OnProjectFinish(Arcweave.Project.Project project)
    {
        UpdateBackgroundFromAttributes();
    }

    private void UpdateBackgroundFromAttributes()
    {
        if (arcweavePlayer == null || arcweavePlayer.aw == null || arcweavePlayer.aw.Project == null)
        {
            Debug.LogError("Arcweave project not initialized or invalid object!");
            return;
        }

        // Find the component
        var component = FindComponentByName(componentName);
        if (component == null)
        {
            Debug.LogWarning($"Component '{componentName}' not found!");
            return;
        }

        // Update background based on time attribute
        var timeAttr = FindAttributeByName(component, timeAttribute);
        if (timeAttr != null)
        {
            string timeValue = timeAttr.data?.ToString();
            if (!string.IsNullOrEmpty(timeValue))
            {
                SetBackgroundBasedOnTime(timeValue);
            }
        }
    }

    private void SetBackgroundBasedOnTime(string timeValue)
    {
        switch (timeValue.ToLower())
        {
            case "day":
                mainCamera.clearFlags = CameraClearFlags.Skybox;
                Debug.Log("Setting background to Skybox (Day)");
                break;
            case "night":
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = Color.black;
                Debug.Log("Setting background to Solid Color Black (Night)");
                break;
            default:
                Debug.LogWarning($"Unknown time value: {timeValue}");
                break;
        }
    }

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