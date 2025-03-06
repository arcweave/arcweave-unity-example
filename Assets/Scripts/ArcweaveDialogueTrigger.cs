using UnityEngine;
using Arcweave;
using System.Linq;
using Arcweave.Project;
using TMPro;  // Add this to use TextMeshPro

public class DialogueTrigger : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float triggerDistance = 3f;
    public KeyCode interactionKey = KeyCode.E;
    
    [Header("UI Settings")]
    [Tooltip("Reference to the TextMeshPro component for interaction text")]
    public TextMeshPro interactionText;
    [Tooltip("Text that appears above the NPC when the player is nearby")]
    public string interactionMessage = "Press E to talk";
    
    [Header("Arcweave References")]
    public ArcweavePlayer arcweavePlayer;
    
    [Header("Arcweave Dialogue Settings")]
    [Tooltip("Specific board name to search for the NPC's dialogue. Leave blank to search all boards.")]
    public string specificBoardName;
    
    [Header("Arcweave Initialization")]
    [Tooltip("Should the project be initialized when the game starts?")]
    public bool initializeOnStart = false;
    
    private GameObject player;
    private bool canInteract = false;
    private bool isInDialogue = false;
    
    void Start()
    {
        // Find the player GameObject
        player = GameObject.FindGameObjectWithTag("Player");
        
        // Set up Arcweave event subscriptions if available
        if (arcweavePlayer != null)
        {
            arcweavePlayer.onProjectFinish += OnProjectFinish;
        }

        // Set initial text if TextMeshPro is assigned
        if (interactionText != null)
        {
            interactionText.text = interactionMessage;
            interactionText.enabled = false;
        }
    }
    
    void OnDisable()
    {
        // Clean up event subscriptions
        if (arcweavePlayer != null)
        {
            arcweavePlayer.onProjectFinish -= OnProjectFinish;
        }
    }
    
    void Update()
    {
        if (!player || !arcweavePlayer) return;
        
        // Skip interaction checks if already in dialogue
        if (isInDialogue) return;
        
        // Check if the player is close enough
        float distance = Vector3.Distance(transform.position, player.transform.position);
        canInteract = distance < triggerDistance;
        
        // Show or hide the interaction text
        if (interactionText != null)
        {
            interactionText.enabled = canInteract && !isInDialogue;
            if (interactionText.enabled)
            {
                // Make the text face the camera
                interactionText.transform.rotation = Camera.main.transform.rotation;
            }
        }
        
        // If the player can interact and presses the key
        if (canInteract && Input.GetKeyDown(interactionKey))
        {
            StartDialogue();
        }
    }
    
    // Start the dialogue with this NPC
    private void StartDialogue()
    {
        isInDialogue = true;
        
        // Tell GameManager to switch to dialogue state
        GameManager.Instance.StartDialogue(this);
        
        // Clear any existing UI elements before starting new dialogue
        if (arcweavePlayer != null && arcweavePlayer.GetComponent<ArcweavePlayerUI>() != null)
        {
            arcweavePlayer.GetComponent<ArcweavePlayerUI>().ClearTempButtons();
        }
        
        // Find and start the proper dialogue element
        FindAndStartNPCDialogue();
    }
    
    // End the dialogue and return to gameplay
    public void EndDialogue()
    {
        isInDialogue = false;
        GameManager.Instance.EndDialogue();
    }
    
    // Called when Arcweave project finishes
    private void OnProjectFinish(Arcweave.Project.Project project)
    {
        // End the dialogue when project finishes
        EndDialogue();
    }
    
    // Find and start the appropriate dialogue for this NPC
    private void FindAndStartNPCDialogue()
    {
        // If no board is specified, use the default starting element
        if (string.IsNullOrEmpty(specificBoardName))
        {
            Debug.LogWarning("No specific board name provided, using default starting element");
            arcweavePlayer.PlayProject();
            return;
        }

        // Search for the specified board
        var targetBoard = arcweavePlayer.aw.Project.boards.Find(board => board.Name == specificBoardName);
        if (targetBoard == null)
        {
            Debug.LogWarning($"Board '{specificBoardName}' not found, using default starting element");
            arcweavePlayer.PlayProject();
            return;
        }

        // Search for an element with the dialogue_start tag in the specified board
        Element startingElement = null;
        foreach (var node in targetBoard.Nodes)
        {
            if (node is Element element)
            {
                foreach (var attribute in element.Attributes)
                {
                    string data = attribute.data?.ToString();
                    if (data != null && data.Contains(GameManager.Instance.dialogueStartTag))
                    {
                        startingElement = element;
                        break;
                    }
                }
                if (startingElement != null) break;
            }
        }

        if (startingElement != null)
        {
            // Use the element found with the tag in the specified board
            arcweavePlayer.Next(startingElement);
        }
        else
        {
            Debug.LogWarning($"No element with tag '{GameManager.Instance.dialogueStartTag}' found in board '{specificBoardName}', using default starting element");
            arcweavePlayer.PlayProject();
        }
    }
    
    void OnDestroy()
    {
        if (interactionText != null)
            Destroy(interactionText.gameObject);
    }
    
    // Visualize interaction radius in the editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerDistance);
    }
}