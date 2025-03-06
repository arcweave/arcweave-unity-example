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

    private void Start()
    {
        arcweavePlayer = FindObjectOfType<ArcweavePlayer>();
        animator = GetComponent<Animator>();

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

    private void Update()
    {
        if (arcweavePlayer?.aw?.Project == null) return;

        // Update UI rotation to face camera
        if (faceCamera && healthBar != null && Camera.main != null)
        {
            healthBar.transform.rotation = Camera.main.transform.rotation;
        }

        UpdateHealthFromVariable();
    }

    private void UpdateHealthFromVariable()
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
                    healthBar.value = Mathf.Lerp(healthBar.value, currentHealth, Time.deltaTime * 5f);
                }

                // Update health text
                if (healthText != null)
                {
                    healthText.text = $"{Mathf.RoundToInt(currentHealth)}";
                    healthText.transform.rotation = healthBar.transform.rotation; // Make text face camera too
                }

                // Update animator parameter if needed
                if (animator != null)
                {
                    animator.SetBool("Healthy", currentHealth >= maxHealth * 0.4f);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error updating health: {e.Message}");
        }
    }
}