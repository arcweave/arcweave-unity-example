using UnityEngine;
using Arcweave.Project;

/// <summary>
/// Permanently enables sword swing when the dialogue reaches an element with the Attack component.
/// </summary>
public class SwordSwingHandler : ArcweaveElementComponentHandler
{
    [Header("References")]
    [Tooltip("The player's PlayerController — will have sword swing enabled")]
    public PlayerController playerController;

    protected override void OnComponentDetected(Element element, Arcweave.Project.Component component)
    {
        if (playerController != null)
            playerController.EnableSwordSwing();
        else
            Debug.LogWarning("[SwordSwingHandler] PlayerController not assigned.", this);
    }

    // OnComponentAbsent not overridden — once unlocked, the ability is permanent.
}
