using UnityEngine;
using Arcweave;
using System.Linq;

public class DialogueTrigger : MonoBehaviour
{
    public float triggerDistance = 3f;
    public KeyCode interactionKey = KeyCode.E;
    public ArcweavePlayer arcweavePlayer;
    public GameObject dialoguePanel;
    
    [Header("Arcweave Dialogue Settings")]
    [Tooltip("Specific board name to search for the NPC's dialogue. Leave blank to search all boards.")]
    public string specificBoardName;

    private GameObject player;
    private bool canInteract = false;
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        
        // Hide dialogue panel at the start
        if (dialoguePanel)
            dialoguePanel.SetActive(false);
    }
    
    void Update()
    {
        if (!player || !arcweavePlayer) return;
        
        // Check if the player is close enough
        float distance = Vector3.Distance(transform.position, player.transform.position);
        canInteract = distance < triggerDistance;
        
        // If the player can interact and presses the key
        if (canInteract && Input.GetKeyDown(interactionKey))
        {
            if (dialoguePanel)
                dialoguePanel.SetActive(true);
            
            // Find the starting element based on the NPC's name
            StartDialogueForNPC();
        }
    }
    
    void StartDialogueForNPC()
    {
        // If the Arcweave project is not assigned, log an error
        if (arcweavePlayer.aw == null)
        {
            Debug.LogError("No Arcweave Project assigned in the inspector!");
            return;
        }

        // Get the component with the same name as this GameObject
        var component = arcweavePlayer.aw.Project.components.Find(c => c.Name == gameObject.name);
        
        if (component == null)
        {
            Debug.LogWarning($"No Arcweave component found with the name '{gameObject.name}'");
            arcweavePlayer.PlayProject(); // Fallback to default starting element
            return;
        }

        // Try to find an element in the project that has this component
        Arcweave.Project.Element startingElement = null;
        
        // If a specific board name is provided, search only that board
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
                if (node is Arcweave.Project.Element element)
                {
                    if (element.Components.Any(c => c.Name == component.Name))
                    {
                        startingElement = element;
                        break;
                    }
                }
            }
        }
        // If no specific board, search all boards
        else
        {
            foreach (var board in arcweavePlayer.aw.Project.boards)
            {
                foreach (var node in board.Nodes)
                {
                    if (node is Arcweave.Project.Element element)
                    {
                        if (element.Components.Any(c => c.Name == component.Name))
                        {
                            startingElement = element;
                            break;
                        }
                    }
                }

                if (startingElement != null)
                    break;
            }
        }

        if (startingElement == null)
        {
            Debug.LogWarning($"No starting element found for component '{component.Name}'" + 
                (string.IsNullOrEmpty(specificBoardName) ? "" : $" in board '{specificBoardName}'"));
            arcweavePlayer.PlayProject(); // Fallback to default starting element
            return;
        }

        // Override the starting element and play the project
        arcweavePlayer.aw.Project.StartingElement = startingElement;
        arcweavePlayer.PlayProject();
    }
    
    void OnGUI()
    {
        if (canInteract)
        {
            // Show an interaction prompt on screen
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