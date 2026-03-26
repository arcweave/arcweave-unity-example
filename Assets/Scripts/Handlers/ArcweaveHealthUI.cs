using UnityEngine;
using Arcweave;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Connects an Arcweave health variable to a Unity Slider, text display, and animator.
/// Also reads a color attribute from an Arcweave component to style the slider fill.
/// </summary>
public class ArcweaveHealthUI : MonoBehaviour
{
    [Header("Arcweave References")]
    public ArcweavePlayer arcweavePlayer;

    [Header("Health Settings")]
    public Slider healthBar;
    public TextMeshProUGUI healthText;
    public string healthVariableName = "health";
    public float maxHealth = 100f;
    public bool faceCamera = true;

    [Header("Animator")]
    [Tooltip("Animator bool parameter for healthy state")]
    public string healthyAnimatorParam = "Healthy";
    [Tooltip("Health percentage threshold below which the character is 'unhealthy'")]
    [Range(0f, 1f)]
    public float healthyThreshold = 0.4f;

    [Header("Slider Color")]
    [Tooltip("The name of the Arcweave component to search for")]
    public string sliderColorComponentName = "UI Settings";
    [Tooltip("The name of the attribute for slider color")]
    public string sliderColorAttribute = "SliderColor";

    private Animator animator;

    private void Start()
    {
        if (arcweavePlayer == null)
        {
            arcweavePlayer = FindAnyObjectByType<ArcweavePlayer>();
            if (arcweavePlayer == null)
            {
                Debug.LogWarning("ArcweavePlayer not found in scene!");
            }
        }

        animator = GetComponent<Animator>();

        if (arcweavePlayer != null)
        {
            arcweavePlayer.onProjectFinish += OnProjectFinish;
        }

        var importer = FindAnyObjectByType<RuntimeArcweaveImporter>();
        if (importer != null)
        {
            importer.onImportSuccess.AddListener(OnImportSuccess);
        }

        SetupHealthBar();
        UpdateSliderColor();
    }

    private void SetupHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = maxHealth;
        }
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

        var importer = FindAnyObjectByType<RuntimeArcweaveImporter>();
        if (importer != null)
        {
            importer.onImportSuccess.RemoveListener(OnImportSuccess);
        }
    }

    private void Update()
    {
        if (arcweavePlayer?.aw?.Project == null) return;

        UpdateHealthBarRotation();
        UpdateHealthFromVariable();
    }

    private void UpdateHealthBarRotation()
    {
        if (!faceCamera || healthBar == null || Camera.main == null) return;

        healthBar.transform.rotation = Camera.main.transform.rotation;
    }

    public void UpdateHealthFromVariable()
    {
        if (arcweavePlayer?.aw?.Project == null) return;

        try
        {
            var healthVar = arcweavePlayer.aw.Project.GetVariable(healthVariableName);
            if (healthVar == null) return;

            float currentHealth = ConvertToFloat(healthVar.Value, healthVar.Type);
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            UpdateHealthUI(currentHealth);
            UpdateHealthAnimator(currentHealth);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error updating health: {e.Message}");
        }
    }

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

    private void UpdateHealthUI(float currentHealth)
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.RoundToInt(currentHealth)}";

            if (healthBar != null)
            {
                healthText.transform.rotation = healthBar.transform.rotation;
            }
        }
    }

    private void UpdateHealthAnimator(float currentHealth)
    {
        if (animator == null) return;

        bool hasHealthyParameter = false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == healthyAnimatorParam && param.type == AnimatorControllerParameterType.Bool)
            {
                hasHealthyParameter = true;
                break;
            }
        }

        if (hasHealthyParameter)
        {
            animator.SetBool(healthyAnimatorParam, currentHealth >= maxHealth * healthyThreshold);
        }
    }

    public void UpdateSliderColor()
    {
        if (arcweavePlayer?.aw?.Project == null || healthBar == null || healthBar.fillRect == null) return;

        var component = FindComponentByName(sliderColorComponentName);
        if (component == null) return;

        var colorAttribute = FindAttributeByName(component, sliderColorAttribute);
        if (colorAttribute == null) return;

        string colorHex = colorAttribute.data?.ToString();
        if (!string.IsNullOrEmpty(colorHex) && ColorUtility.TryParseHtmlString(colorHex, out Color color))
        {
            healthBar.fillRect.GetComponent<Image>().color = color;
            Debug.Log($"Health bar color set to: {colorHex}");
        }
    }

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
