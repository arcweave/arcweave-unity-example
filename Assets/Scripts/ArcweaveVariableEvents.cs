using UnityEngine;
using Arcweave;
using UnityEngine.UI;
using TMPro;

public class ArcweaveVariableEvents : MonoBehaviour
{
    [Header("References")]
    private ArcweavePlayer arcweavePlayer;
    private Animator animator;

    [Header("Health UI")]
    public Slider healthBar;                     // Reference to UI Slider
    public TextMeshProUGUI healthText;          // Reference to health text
    public string healthVariableName = "health"; // Name of the health variable in Arcweave
    public float maxHealth = 100f;              // Maximum health value
    public bool faceCamera = true;              // Whether the UI should face the camera

    [Header("Object Activation")]
    public GameObject targetObject;           // GameObject to activate/deactivate based on variable
    public string activationVariableName = "activateObject"; // Name of the boolean variable in Arcweave
    private bool objectPermanentlyDeactivated = false; // Aggiungi questa variabile

    [Header("Slider Color Settings")]
    [Tooltip("The name of the Arcweave component to search for")]
    public string sliderColorComponentName = "UI Settings";
    [Tooltip("The name of the attribute for slider color")]
    public string sliderColorAttribute = "SliderColor";

    private void Start()
    {
        arcweavePlayer = FindAnyObjectByType<ArcweavePlayer>();
        animator = GetComponent<Animator>();

        if (arcweavePlayer != null)
        {
            arcweavePlayer.onProjectFinish += OnProjectFinish;
        }

        var importer = FindObjectOfType<RuntimeArcweaveImporter>();
        if (importer != null)
        {
            importer.onImportSuccess.AddListener(OnImportSuccess);
        }

        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = maxHealth;
        }
        else
        {
            Debug.LogWarning("Health bar not assigned! Please assign a UI Slider in the inspector.");
        }

        if (targetObject == null)
        {
            Debug.LogWarning("Target object not assigned! Please assign a GameObject in the inspector.");
        }

        // Initialize slider color
        UpdateSliderColor();
    }

    private void OnImportSuccess()
    {
        UpdateSliderColor();
        UpdateHealthFromVariable();
    }

    private void OnProjectFinish(Arcweave.Project.Project project)
    {
        UpdateSliderColor();
        UpdateHealthFromVariable();
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

    private void Update()
    {
        if (arcweavePlayer?.aw?.Project == null) return;

        // Update UI rotation to face camera
        if (faceCamera && healthBar != null && Camera.main != null)
        {
            healthBar.transform.rotation = Camera.main.transform.rotation;
        }

        UpdateHealthFromVariable();
        UpdateObjectActivation();
    }

    public void UpdateHealthFromVariable()
    {
        try
        {
            var healthVar = arcweavePlayer.aw.Project.GetVariable(healthVariableName);
            if (healthVar != null)
            {
                float currentHealth = 0f;

                // Convert variable to float based on its type
                if (healthVar.Type == typeof(int))
                {
                    currentHealth = (int)healthVar.Value;
                }
                else if (healthVar.Type == typeof(float))
                {
                    currentHealth = (float)healthVar.Value;
                }
                else if (healthVar.Type == typeof(string))
                {
                    if (float.TryParse(healthVar.Value.ToString(), out float parsedHealth))
                    {
                        currentHealth = parsedHealth;
                    }
                }

                // Update health bar with smooth transition
                currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
                if (healthBar != null)
                {
                    healthBar.value = currentHealth; // Rimuovi l'interpolazione per aggiornamento immediato
                }

                // Update health text
                if (healthText != null)
                {
                    healthText.text = $"{Mathf.RoundToInt(currentHealth)}";
                    healthText.transform.rotation = healthBar.transform.rotation;
                }

                // Update animator parameter if needed
                if (animator != null)
                {
                    // Verifica se il parametro "Healthy" esiste
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
                        animator.SetBool("Healthy", currentHealth >= maxHealth * 0.4f);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error updating health: {e.Message}");
        }
    }

    public void UpdateObjectActivation()
    {
        // Skip if object not assigned
        if (targetObject == null) return;

        try
        {
            var activationVar = arcweavePlayer.aw.Project.GetVariable(activationVariableName);
            if (activationVar != null && activationVar.Type == typeof(bool))
            {
                bool shouldActivate = !(bool)activationVar.Value; // Invert logic: if variable is true, deactivate object
                
                // Se la condizione è stata resettata, forziamo l'aggiornamento
                if (objectPermanentlyDeactivated && shouldActivate)
                {
                    objectPermanentlyDeactivated = false;
                }

                // Only update if state changed
                if (targetObject.activeSelf != shouldActivate && !objectPermanentlyDeactivated)
                {
                    targetObject.SetActive(shouldActivate);
                    Debug.Log($"Object '{targetObject.name}' {(shouldActivate ? "activated" : "deactivated")} based on variable '{activationVariableName}'");

                    // Se l'oggetto viene disattivato, impostiamo il flag per mantenerlo disattivato
                    if (!shouldActivate)
                    {
                        objectPermanentlyDeactivated = true;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error updating object activation: {e.Message}");
        }
    }

    public void UpdateSliderColor()
    {
        if (arcweavePlayer == null || arcweavePlayer.aw == null || arcweavePlayer.aw.Project == null)
        {
            Debug.LogError("Arcweave project not initialized or invalid object!");
            return;
        }

        // Find the component
        var component = FindComponentByName(sliderColorComponentName);
        if (component == null)
        {
            Debug.LogWarning($"Component '{sliderColorComponentName}' not found!");
            return;
        }

        // Find the attribute
        var colorAttribute = FindAttributeByName(component, sliderColorAttribute);
        if (colorAttribute != null)
        {
            string colorHex = colorAttribute.data?.ToString();
            if (!string.IsNullOrEmpty(colorHex) && ColorUtility.TryParseHtmlString(colorHex, out Color color))
            {
                if (healthBar != null && healthBar.fillRect != null)
                {
                    healthBar.fillRect.GetComponent<Image>().color = color;
                    Debug.Log($"Health bar color set to: {colorHex}");
                }
            }
            else
            {
                Debug.LogWarning($"Invalid color value: {colorHex}");
            }
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

    public void ResetObjectActivation()
    {
        objectPermanentlyDeactivated = false;
        Debug.Log("Object activation condition reset.");
    }
}