using UnityEngine;
using Arcweave;

/// <summary>
/// Activates or deactivates a GameObject based on an Arcweave boolean variable.
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

    private bool objectPermanentlyDeactivated = false;

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
        objectPermanentlyDeactivated = false;
        UpdateObjectActivation();
    }

    private void OnProjectFinish(Arcweave.Project.Project project)
    {
        objectPermanentlyDeactivated = false;
        UpdateObjectActivation();
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

        UpdateObjectActivation();
    }

    public void UpdateObjectActivation()
    {
        if (targetObject == null || arcweavePlayer?.aw?.Project == null) return;

        try
        {
            if (objectPermanentlyDeactivated)
            {
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

            if (targetObject.activeSelf && !shouldActivate)
            {
                objectPermanentlyDeactivated = true;
                targetObject.SetActive(false);
                Debug.Log($"Object '{targetObject.name}' permanently deactivated based on variable '{activationVariableName}'");
            }
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
    /// Reset the permanent deactivation flag (called on reimport/restart)
    /// </summary>
    public void ResetObjectActivation()
    {
        objectPermanentlyDeactivated = false;
    }
}
