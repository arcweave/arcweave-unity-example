using UnityEngine;
using Arcweave;

/// <summary>
/// Activates or deactivates a GameObject based on an Arcweave boolean variable or element visit.
/// Uses inverted logic: variable true = object inactive, variable false = object active.
/// Name variables for the locked/closed state (e.g. chest_locked, sword_locked).
/// </summary>
public class ArcweaveObjectActivation : MonoBehaviour
{
    [Header("Arcweave References")]
    public ArcweavePlayer arcweavePlayer;

    [Header("Object Activation")]
    [Tooltip("The GameObject to activate/deactivate")]
    public GameObject targetObject;
    [Tooltip("Boolean variable name. true = object inactive, false = object active (inverted logic)")]
    public string activationVariableName = "activateObject";
    [Tooltip("Optional element ID. When set, hide the object after this element was visited instead of using the variable.")]
    public string deactivateAfterVisitingElementId;

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

        if (arcweavePlayer != null)
        {
            arcweavePlayer.onProjectFinish += OnProjectFinish;
            arcweavePlayer.onProjectUpdated += OnProjectFinish;
        }

        var importer = FindAnyObjectByType<RuntimeArcweaveImporter>();
        if (importer != null)
        {
            importer.onImportSuccess.AddListener(OnImportSuccess);
        }

        if (targetObject == null)
        {
            Debug.LogWarning("Target object not assigned! Please assign a GameObject in the inspector.");
        }
    }

    private void OnImportSuccess()
    {
        UpdateObjectActivation();
    }

    private void OnProjectFinish(Arcweave.Project.Project project)
    {
        UpdateObjectActivation();
    }

    private void OnDestroy()
    {
        if (arcweavePlayer != null)
        {
            arcweavePlayer.onProjectFinish -= OnProjectFinish;
            arcweavePlayer.onProjectUpdated -= OnProjectFinish;
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

        UpdateObjectActivation();
    }

    public void UpdateObjectActivation()
    {
        if (targetObject == null || arcweavePlayer?.aw?.Project == null) return;

        try
        {
            bool shouldActivate;
            if (!string.IsNullOrEmpty(deactivateAfterVisitingElementId))
            {
                var element = arcweavePlayer.aw.Project.ElementWithId(deactivateAfterVisitingElementId);
                if (element == null) return;
                shouldActivate = element.Visits == 0;
            }
            else
            {
                var activationVar = arcweavePlayer.aw.Project.GetVariable(activationVariableName);
                if (activationVar == null || activationVar.Type != typeof(bool)) return;
                shouldActivate = !(bool)activationVar.Value;
            }

            if (targetObject.activeSelf != shouldActivate) targetObject.SetActive(shouldActivate);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error updating object activation: {e.Message}");
        }
    }

    /// <summary>
    /// Refresh from the current project state (called on reimport/restart).
    /// </summary>
    public void ResetObjectActivation()
    {
        UpdateObjectActivation();
    }
}
