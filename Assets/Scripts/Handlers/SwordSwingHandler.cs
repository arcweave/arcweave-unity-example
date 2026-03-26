using UnityEngine;
using Arcweave.Project;

/// <summary>
/// Permanently enables sword swing when the dialogue reaches an element with the Attack component.
/// Reads the optional SwingSpeed attribute to set animation speed (default 1.0).
/// </summary>
public class SwordSwingHandler : ArcweaveElementComponentHandler
{
    [Header("References")]
    [Tooltip("The player's PlayerController — will have sword swing enabled")]
    public PlayerController playerController;

    [Header("Attribute Settings")]
    [Tooltip("Name of the optional attribute on the Attack component that sets animation speed")]
    public string swingSpeedAttribute = "SwingSpeed";

    protected override void OnComponentDetected(Element element, Arcweave.Project.Component component)
    {
        float speed = 1f;

        string speedValue = GetAttributeValue(component, swingSpeedAttribute);
        if (!string.IsNullOrEmpty(speedValue))
            float.TryParse(speedValue,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out speed);

        if (playerController != null)
            playerController.EnableSwordSwing(speed);
        else
            Debug.LogWarning("[SwordSwingHandler] PlayerController not assigned.", this);
    }

    // OnComponentAbsent not overridden — once unlocked, the ability is permanent.
}
