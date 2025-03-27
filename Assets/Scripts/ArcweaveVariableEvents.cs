using UnityEngine;
using Arcweave;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Connects Arcweave variables to Unity game objects and UI elements
/// </summary>
public class ArcweaveVariableEvents : MonoBehaviour
{
    [Header("References")]
    public ArcweavePlayer arcweavePlayer;
    private Animator animator;

    [Header("Health UI")]
    public Slider healthBar;
    public TextMeshProUGUI healthText;
    public string healthVariableName = "health";
    public float maxHealth = 100f;
    public bool faceCamera = true;

    [Header("Object Activation")]
    public GameObject targetObject;
    public string activationVariableName = "activateObject";
    private bool objectPermanentlyDeactivated = false;

    [Header("Slider Color Settings")]
    [Tooltip("The name of the Arcweave component to search for")]
    public string sliderColorComponentName = "UI Settings";
    [Tooltip("The name of the attribute for slider color")]
    public string sliderColorAttribute = "SliderColor";

    private void Start()
    {
        // Find references if not assigned
        if (arcweavePlayer == null)
        {
            arcweavePlayer = FindObjectOfType<ArcweavePlayer>();
            if (arcweavePlayer == null)
            {
                Debug.LogWarning("ArcweavePlayer not found in scene!");
            }
        }
        
        animator = GetComponent<Animator>();

        // Set up event listeners
        if (arcweavePlayer != null)
        {
            arcweavePlayer.onProjectFinish += OnProjectFinish;
        }

        var importer = FindObjectOfType<RuntimeArcweaveImporter>();
        if (importer != null)
        {
            importer.onImportSuccess.AddListener(OnImportSuccess);
        }

        // Initialize UI elements
        SetupHealthBar();
        
        // Log warnings for missing references
        if (targetObject == null)
        {
            Debug.LogWarning("Target object not assigned! Please assign a GameObject in the inspector.");
        }

        // Initialize slider color
        UpdateSliderColor();
    }

    /// <summary>
    /// Sets up the health bar with initial values
    /// </summary>
    private void SetupHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = maxHealth;
        }
        else
        {
            Debug.LogWarning("Health bar not assigned! Please assign a UI Slider in the inspector.");
        }
    }

    /// <summary>
    /// Called when a new project is imported successfully
    /// </summary>
    private void OnImportSuccess()
    {
        // Reset the permanent deactivation flag when project is reimported
        objectPermanentlyDeactivated = false;
        
        UpdateSliderColor();
        UpdateHealthFromVariable();
        UpdateObjectActivation();
    }

    /// <summary>
    /// Called when an Arcweave project finishes
    /// </summary>
    private void OnProjectFinish(Arcweave.Project.Project project)
    {
        // Reset the permanent deactivation flag when project finishes
        objectPermanentlyDeactivated = false;
        
        UpdateSliderColor();
        UpdateHealthFromVariable();
        UpdateObjectActivation();
    }

    private void OnDestroy()
    {
        // Clean up event listeners
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

    private void Update()
    {
        if (arcweavePlayer?.aw?.Project == null) return;

        // Update UI rotation to face camera
        UpdateHealthBarRotation();
        
        // Update gameplay elements from variables
        UpdateHealthFromVariable();
        UpdateObjectActivation();
    }

    /// <summary>
    /// Updates health bar rotation to face camera
    /// </summary>
    private void UpdateHealthBarRotation()
    {
        if (!faceCamera || healthBar == null || Camera.main == null) return;
        
        healthBar.transform.rotation = Camera.main.transform.rotation;
    }

    /// <summary>
    /// Updates health bar and text from Arcweave variable
    /// </summary>
    public void UpdateHealthFromVariable()
    {
        if (arcweavePlayer?.aw?.Project == null) return;
        
        try
        {
            var healthVar = arcweavePlayer.aw.Project.GetVariable(healthVariableName);
            if (healthVar == null) return;
            
            float currentHealth = ConvertToFloat(healthVar.Value, healthVar.Type);
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            
            // Update health bar UI
            UpdateHealthUI(currentHealth);
            
            // Update animator parameter if needed
            UpdateHealthAnimator(currentHealth);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error updating health: {e.Message}");
        }
    }
    
    /// <summary>
    /// Converts a variable's value to float based on its type
    /// </summary>
    private float ConvertToFloat(object value, System.Type type)
    {
        if (type == typeof(int))
            return (int)value;
        else if (type == typeof(float))
            return (float)value;
        else if (type == typeof(string) && float.TryParse(value.ToString(), out float parsedValue))
            return parsedValue;
            
        return 0f;
    }
    
    /// <summary>
    /// Updates health UI elements with current health value
    /// </summary>
    private void UpdateHealthUI(float currentHealth)
    {
        // Update health bar
        if (healthBar != null)
        {
            healthBar.value = currentHealth;
        }

        // Update health text
        if (healthText != null)
        {
            healthText.text = $"{Mathf.RoundToInt(currentHealth)}";
            
            if (healthBar != null)
            {
                healthText.transform.rotation = healthBar.transform.rotation;
            }
        }
    }
    
    /// <summary>
    /// Updates animator parameters based on health value
    /// </summary>
    private void UpdateHealthAnimator(float currentHealth)
    {
        if (animator == null) return;
        
        // Check if the "Healthy" parameter exists
        bool hasHealthyParameter = false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == "Healthy" && param.type == AnimatorControllerParameterType.Bool)
            {
                hasHealthyParameter = true;
                break;
            }
        }

        if (hasHealthyParameter)
        {
            // Set Healthy to true if health is above 40% of max
            animator.SetBool("Healthy", currentHealth >= maxHealth * 0.4f);
        }
    }

    /// <summary>
    /// Updates game object activation based on Arcweave variable
    /// </summary>
    public void UpdateObjectActivation()
    {
        // Skip if object not assigned
        if (targetObject == null || arcweavePlayer?.aw?.Project == null) return;

        try
        {
            // If the object has been permanently deactivated, don't do anything
            if (objectPermanentlyDeactivated)
            {
                // Ensure the object stays deactivated
                if (targetObject.activeSelf)
                {
                    targetObject.SetActive(false);
                }
                return;
            }
            
            var activationVar = arcweavePlayer.aw.Project.GetVariable(activationVariableName);
            if (activationVar == null || activationVar.Type != typeof(bool)) return;
            
            // Invert logic: if variable is true, deactivate object
            bool shouldActivate = !(bool)activationVar.Value;
            
            // If object is getting deactivated, set the permanent flag
            if (targetObject.activeSelf && !shouldActivate)
            {
                objectPermanentlyDeactivated = true;
                targetObject.SetActive(false);
                Debug.Log($"Object '{targetObject.name}' permanently deactivated based on variable '{activationVariableName}'");
            }
            // Only activate if it wasn't permanently deactivated before
            else if (!targetObject.activeSelf && shouldActivate && !objectPermanentlyDeactivated)
            {
                targetObject.SetActive(true);
                Debug.Log($"Object '{targetObject.name}' activated based on variable '{activationVariableName}'");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error updating object activation: {e.Message}");
        }
    }

    /// <summary>
    /// Reset any permanent deactivation flags
    /// </summary>
    public void ResetObjectActivation()
    {
        objectPermanentlyDeactivated = false;
    }

    /// <summary>
    /// Updates slider color based on Arcweave component attribute
    /// </summary>
    public void UpdateSliderColor()
    {
        if (arcweavePlayer?.aw?.Project == null || healthBar == null || healthBar.fillRect == null) return;

        // Find the component
        var component = FindComponentByName(sliderColorComponentName);
        if (component == null)
        {
            Debug.LogWarning($"Component '{sliderColorComponentName}' not found!");
            return;
        }

        // Find the attribute
        var colorAttribute = FindAttributeByName(component, sliderColorAttribute);
        if (colorAttribute == null) return;
        
        string colorHex = colorAttribute.data?.ToString();
        if (!string.IsNullOrEmpty(colorHex) && ColorUtility.TryParseHtmlString(colorHex, out Color color))
        {
            healthBar.fillRect.GetComponent<Image>().color = color;
            Debug.Log($"Health bar color set to: {colorHex}");
        }
        else
        {
            Debug.LogWarning($"Invalid color value: {colorHex}");
        }
    }

    /// <summary>
    /// Finds an Arcweave component by name
    /// </summary>
    private Arcweave.Project.Component FindComponentByName(string name)
    {
        if (arcweavePlayer?.aw?.Project == null) return null;

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
    /// Finds an attribute in a component by name
    /// </summary>
    private Arcweave.Project.Attribute FindAttributeByName(Arcweave.Project.Component component, string attributeName)
    {
        if (component?.Attributes == null) return null;

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