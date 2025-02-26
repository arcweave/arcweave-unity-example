using UnityEngine;
using Arcweave;
using System.Linq;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float triggerDistance = 3f;
    public KeyCode interactionKey = KeyCode.E;
    
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
 

        // Find component matching this GameObject's name
        var component = arcweavePlayer.aw.Project.components.Find(c => c.Name == gameObject.name);
        
        if (component == null)
        {
            Debug.LogWarning($"No Arcweave component found with the name '{gameObject.name}'");
            arcweavePlayer.PlayProject(); // Fallback to default
            return;
        }

        // Find element with this component
        Arcweave.Project.Element startingElement = null;
        
        // Search in specific board if provided
        if (!string.IsNullOrEmpty(specificBoardName))
        {
            var targetBoard = arcweavePlayer.aw.Project.boards.Find(board => board.Name == specificBoardName);
            
            if (targetBoard == null)
            {
                Debug.LogWarning($"No board found with the name '{specificBoardName}'");
                arcweavePlayer.PlayProject();
                return;
            }

            foreach (var node in targetBoard.Nodes)
            {
                if (node is Arcweave.Project.Element element && 
                    element.Components.Any(c => c.Name == component.Name))
                {
                    startingElement = element;
                    break;
                }
            }
        }
        // Or search all boards
        else
        {
            foreach (var board in arcweavePlayer.aw.Project.boards)
            {
                foreach (var node in board.Nodes)
                {
                    if (node is Arcweave.Project.Element element && 
                        element.Components.Any(c => c.Name == component.Name))
                    {
                        startingElement = element;
                        break;
                    }
                }

                if (startingElement != null)
                    break;
            }
        }

        // If no element found, use default
        if (startingElement == null)
        {
            Debug.LogWarning($"No starting element found for component '{component.Name}'" + 
                (string.IsNullOrEmpty(specificBoardName) ? "" : $" in board '{specificBoardName}'"));
            arcweavePlayer.PlayProject();
            return;
        }

        // Set starting element and play
        arcweavePlayer.aw.Project.StartingElement = startingElement;
        arcweavePlayer.PlayProject();
    }
    
    void OnGUI()
    {
        if (canInteract && !isInDialogue)
        {
            // Show interaction prompt
            GUI.Label(new Rect(Screen.width/2 - 100, Screen.height - 50, 200, 30), 
                     "Press E to talk");
        }
    }
    
    // Visualize interaction radius in the editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerDistance);
    }
}