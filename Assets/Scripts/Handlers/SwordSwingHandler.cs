using UnityEngine;
using Arcweave.Project;
using System.Linq;

/// <summary>
/// Permanently enables sword swing when the dialogue reaches an element with the Attack component.
/// </summary>
public class SwordSwingHandler : ArcweaveElementComponentHandler
{
    [Header("References")]
    [Tooltip("The player's PlayerController — will have sword swing enabled")]
    public PlayerController playerController;

    protected override void Start()
    {
        base.Start();
        if (arcweavePlayer != null)
        {
            arcweavePlayer.onProjectUpdated += RestoreAbility;
            RestoreAbility(arcweavePlayer.aw.Project);
        }
    }

    private void RestoreAbility(Arcweave.Project.Project project)
    {
        if (playerController != null && project != null)
            playerController.canSwing = project.Boards.SelectMany(board => board.Nodes).OfType<Element>()
                .Any(element => element.Visits > 0 && element.HasComponent(componentName));
    }

    protected override void OnDestroy()
    {
        if (arcweavePlayer != null) arcweavePlayer.onProjectUpdated -= RestoreAbility;
        base.OnDestroy();
    }

    protected override void OnComponentDetected(Element element, Arcweave.Project.Component component)
    {
        if (playerController != null)
            playerController.EnableSwordSwing();
        else
            Debug.LogWarning("[SwordSwingHandler] PlayerController not assigned.", this);
    }

    // OnComponentAbsent not overridden — once unlocked, the ability is permanent.
}
