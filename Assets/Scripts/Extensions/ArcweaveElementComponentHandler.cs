using UnityEngine;
using Arcweave;
using Arcweave.Project;

/// <summary>
/// Abstract base class for reacting to Arcweave components attached to dialogue elements.
/// Subscribes to ArcweavePlayer.onElementEnter and fires OnComponentDetected / OnComponentAbsent
/// based on whether the current element carries a component matching componentName.
/// </summary>
[AddComponentMenu("")]
public abstract class ArcweaveElementComponentHandler : MonoBehaviour
{
    [Header("Arcweave References")]
    public ArcweavePlayer arcweavePlayer;

    [Header("Component Settings")]
    [Tooltip("Name of the Arcweave component to watch for (case-sensitive).")]
    public string componentName = "";

    protected virtual void Start()
    {
        if (arcweavePlayer == null)
            arcweavePlayer = FindObjectOfType<ArcweavePlayer>();

        if (arcweavePlayer != null)
            arcweavePlayer.onElementEnter += HandleElementEnter;
        else
            Debug.LogError($"[{GetType().Name}] ArcweavePlayer not found in scene!");
    }

    private void HandleElementEnter(Element element)
    {
        if (element == null) return;

        if (element.TryGetComponent(componentName, out Arcweave.Project.Component component))
            OnComponentDetected(element, component);
        else
            OnComponentAbsent(element);
    }

    /// <summary>
    /// Called when the player enters an element that has the watched component.
    /// Use component.Attributes to read config data set in Arcweave.
    /// </summary>
    protected abstract void OnComponentDetected(Element element, Arcweave.Project.Component component);

    /// <summary>
    /// Called when the player enters an element that does NOT have the watched component.
    /// Override to undo or reset effects. Default does nothing.
    /// </summary>
    protected virtual void OnComponentAbsent(Element element) { }

    /// <summary>
    /// Reads a named attribute value from a component as a string. Returns null if not found.
    /// </summary>
    protected string GetAttributeValue(Arcweave.Project.Component component, string attributeName)
    {
        if (component?.Attributes == null) return null;

        foreach (var attr in component.Attributes)
        {
            if (attr != null && attr.Name == attributeName)
                return attr.data?.ToString();
        }
        return null;
    }

    protected virtual void OnDestroy()
    {
        if (arcweavePlayer != null)
            arcweavePlayer.onElementEnter -= HandleElementEnter;
    }
}
