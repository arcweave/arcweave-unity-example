using UnityEngine;
using Arcweave;
using System.Linq;
using Arcweave.Project;
using TMPro;

/// <summary>
/// Handles triggering and interaction with Arcweave dialogue system
/// </summary>
public class DialogueTrigger : MonoBehaviour
{
    // Constants
    private const string PLAYER_TAG = "Player";
    private const string DEFAULT_DIALOGUE_START_TAG = "dialogue_start";

    [Header("Interaction Settings")]
    public float triggerDistance = 3f;
    public KeyCode interactionKey = KeyCode.E;
    
    [Header("UI Settings")]
    public TextMeshPro interactionText;
    public string interactionMessage = "Press E to talk";
    
    [Header("Arcweave References")]
    public ArcweavePlayer arcweavePlayer;
    
    [Header("Arcweave Dialogue Settings")]
    public string specificBoardName;
    public string fallbackBoardName;
    
    [Header("Debug Settings")]
    public bool debugMode = false;
    
    [Header("Character Animation")]
    public bool controlPlayerAnimator = true;
    public string inDialogueParameterName = "IsInDialogue";
    
    // Private variables
    private GameObject player;
    private bool canInteract = false;
    private bool isInDialogue = false;
    private bool isInitialized = false;
    private Animator playerAnimator;
    
    void Start()
    {
        Initialize();
    }
    
    /// <summary>
    /// Initialize the dialogue trigger component
    /// </summary>
    private void Initialize()
    {
        if (isInitialized) return;
        
        // Find references if not assigned
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag(PLAYER_TAG);
            if (player == null)
            {
                Debug.LogWarning($"Player not found! Make sure a GameObject with the '{PLAYER_TAG}' tag exists.");
            }
            else if (controlPlayerAnimator)
            {
                // Find player animator if we need to control it
                playerAnimator = player.GetComponent<Animator>();
                if (playerAnimator == null)
                {
                    Debug.LogWarning("Player Animator not found on Player GameObject!");
                }
            }
        }
        
        if (arcweavePlayer == null)
        {
            arcweavePlayer = FindAnyObjectByType<ArcweavePlayer>();
            if (arcweavePlayer == null)
            {
                Debug.LogWarning("ArcweavePlayer not found in scene!");
            }
        }
        
        // Set up Arcweave event subscriptions
        if (arcweavePlayer != null)
        {
            // Unsubscribe first to prevent duplicate subscriptions
            arcweavePlayer.onProjectFinish -= OnProjectFinish;
            arcweavePlayer.onProjectFinish += OnProjectFinish;
            
            // Make sure the Arcweave project is initialized
            arcweavePlayer.EnsureInitialized();
        }

        // Set initial text visibility
        if (interactionText != null)
        {
            interactionText.text = interactionMessage;
            interactionText.enabled = false;
        }
        
        isInitialized = true;
        
        if (debugMode)
        {
            Debug.Log($"DialogueTrigger initialized for {gameObject.name}");
        }
    }
    
    void OnEnable()
    {
        // Re-initialize if needed
        if (!isInitialized)
        {
            Initialize();
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
        UpdateInteractionText();
        
        // Start dialogue when player presses the interaction key
        if (canInteract && Input.GetKeyDown(interactionKey))
        {
            StartDialogue();
        }
    }
    
    /// <summary>
    /// Updates the interaction text visibility and rotation
    /// </summary>
    private void UpdateInteractionText()
    {
        if (interactionText == null) return;
        
        interactionText.enabled = canInteract && !isInDialogue;
        
        if (interactionText.enabled && Camera.main != null)
        {
            // Make the text face the camera
            interactionText.transform.rotation = Camera.main.transform.rotation;
        }
    }
    
    /// <summary>
    /// Start the dialogue with this NPC
    /// </summary>
    public void StartDialogue()
    {
        if (isInDialogue) return;
        
        if (arcweavePlayer == null)
        {
            Debug.LogError("Cannot start dialogue - ArcweavePlayer not found");
            return;
        }
        
        // Ensure Arcweave project is initialized
        arcweavePlayer.EnsureInitialized();
        
        isInDialogue = true;
        
        // Update player animator parameter
        if (controlPlayerAnimator && playerAnimator != null)
        {
            playerAnimator.SetBool(inDialogueParameterName, true);
            
            if (debugMode)
            {
                Debug.Log($"Set player animator parameter '{inDialogueParameterName}' to true");
            }
        }
        
        if (debugMode)
        {
            Debug.Log($"Starting dialogue with {gameObject.name}");
        }
        
        // Tell GameManager to switch to dialogue state
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartDialogue(this);
        }
        
        // Clear any existing UI elements
        var playerUI = arcweavePlayer?.GetComponent<ArcweavePlayerUI>();
        if (playerUI != null)
        {
            playerUI.ClearTempButtons();
        }
        
        // Find and start the proper dialogue element
        FindAndStartNPCDialogue();
    }
    
    /// <summary>
    /// End the dialogue and return to gameplay
    /// </summary>
    public void EndDialogue()
    {
        if (!isInDialogue) return;
        
        isInDialogue = false;
        
        // Update player animator parameter
        if (controlPlayerAnimator && playerAnimator != null)
        {
            playerAnimator.SetBool(inDialogueParameterName, false);
            
            if (debugMode)
            {
                Debug.Log($"Set player animator parameter '{inDialogueParameterName}' to false");
            }
        }
        
        if (debugMode)
        {
            Debug.Log($"Ending dialogue with {gameObject.name}");
        }
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EndDialogue();
        }
    }
    
    /// <summary>
    /// Called when Arcweave project finishes
    /// </summary>
    private void OnProjectFinish(Arcweave.Project.Project project)
    {
        if (debugMode)
        {
            Debug.Log($"Project finished event received by {gameObject.name}");
        }
        
        EndDialogue();
    }
    
    /// <summary>
    /// Find and start the appropriate dialogue for this NPC
    /// </summary>
    private void FindAndStartNPCDialogue()
    {
        if (arcweavePlayer == null || arcweavePlayer.aw == null || arcweavePlayer.aw.Project == null)
        {
            Debug.LogError("Arcweave Player or Project not assigned");
            EndDialogue();
            return;
        }
        
        // If no board is specified, try to find a board with this NPC's name
        if (string.IsNullOrEmpty(specificBoardName))
        {
            specificBoardName = gameObject.name;
            Debug.Log($"No board specified, trying to find board named: {specificBoardName}");
        }

        // Search for the specified board
        var targetBoard = arcweavePlayer.aw.Project.boards.Find(board => board != null && board.Name == specificBoardName);
        
        // Try fallback board if specified and primary board not found
        if (targetBoard == null && !string.IsNullOrEmpty(fallbackBoardName))
        {
            Debug.LogWarning($"Board '{specificBoardName}' not found, trying fallback board '{fallbackBoardName}'");
            targetBoard = arcweavePlayer.aw.Project.boards.Find(board => board != null && board.Name == fallbackBoardName);
        }
        
        if (targetBoard == null)
        {
            Debug.LogError($"Could not find board '{specificBoardName}' or fallback board '{fallbackBoardName}'");
            EndDialogue();
            return;
        }

        // Find element with dialogue_start tag in the specified board
        Debug.Log($"Starting dialogue from board: {targetBoard.Name}");
        
        Element startingElement = FindDialogueStartElement(targetBoard);

        if (startingElement != null)
        {
            // Use the element found with the tag
            Debug.Log($"Starting dialogue from element: {startingElement.Title}");
            
            // Check if element has content
            if (!startingElement.HasContent())
            {
                Debug.LogWarning($"Starting element '{startingElement.Title}' has no content!");
            }
            
            arcweavePlayer.Next(startingElement);
        }
        else
        {
            // Try to find any element in the board as a fallback
            if (targetBoard.Nodes != null && targetBoard.Nodes.Count > 0)
            {
                var fallbackElement = targetBoard.Nodes.OfType<Element>().FirstOrDefault();
                if (fallbackElement != null)
                {
                    Debug.LogWarning($"No element with dialogue_start tag found. Using first element in board as fallback: {fallbackElement.Title}");
                    arcweavePlayer.Next(fallbackElement);
                    return;
                }
            }
            
            // No starting element found, end dialogue
            Debug.LogError($"No starting element found in board '{targetBoard.Name}'");
            EndDialogue();
        }
    }
    
    /// <summary>
    /// Find an element with the dialogue_start tag in a board
    /// </summary>
    private Element FindDialogueStartElement(Board targetBoard)
    {
        if (targetBoard == null || targetBoard.Nodes == null) return null;
        
        string tagToFind = string.Empty;
        
        // Get the dialogue start tag from GameManager
        if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.dialogueStartTag))
        {
            tagToFind = GameManager.Instance.dialogueStartTag;
        }
        else
        {
            // Fallback to default tag if GameManager is not available
            tagToFind = DEFAULT_DIALOGUE_START_TAG;
            Debug.LogWarning($"GameManager not found or dialogueStartTag not set. Using default tag: '{DEFAULT_DIALOGUE_START_TAG}'");
        }
        
        if (debugMode)
        {
            Debug.Log($"Searching for elements with tag: {tagToFind}");
        }
        
        // First pass: look for exact tag match
        foreach (var node in targetBoard.Nodes)
        {
            if (node is Element element)
            {
                if (element.Attributes == null) continue;
                
                foreach (var attribute in element.Attributes)
                {
                    if (attribute == null || attribute.data == null) continue;
                    
                    string data = attribute.data.ToString();
                    if (data == tagToFind)
                    {
                        if (debugMode)
                        {
                            Debug.Log($"Found element with exact tag match: {element.Title}");
                        }
                        return element;
                    }
                }
            }
        }
        
        // Second pass: look for partial tag match
        foreach (var node in targetBoard.Nodes)
        {
            if (node is Element element)
            {
                if (element.Attributes == null) continue;
                
                foreach (var attribute in element.Attributes)
                {
                    if (attribute == null || attribute.data == null) continue;
                    
                    string data = attribute.data.ToString();
                    if (data.Contains(tagToFind))
                    {
                        if (debugMode)
                        {
                            Debug.Log($"Found element with partial tag match: {element.Title}");
                        }
                        return element;
                    }
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Force cleanup on destruction
    /// </summary>
    void OnDestroy()
    {
        if (interactionText != null)
            Destroy(interactionText.gameObject);
    }
    
    /// <summary>
    /// Visualize interaction radius in the editor
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerDistance);
    }
    
    /// <summary>
    /// Refreshes the board reference when a new project is loaded
    /// </summary>
    public void RefreshBoardReference()
    {
        if (arcweavePlayer == null || arcweavePlayer.aw == null || arcweavePlayer.aw.Project == null)
        {
            Debug.LogWarning("Cannot refresh board reference: ArcweavePlayer or Project is null");
            return;
        }
        
        // Try to find the specified board by name in the new project
        Board targetBoard = null;
        
        if (!string.IsNullOrEmpty(specificBoardName))
        {
            targetBoard = FindBoardByName(specificBoardName);
            
            if (targetBoard != null)
            {
                Debug.Log($"Found specific board '{specificBoardName}' for dialogue trigger {gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"Specific board '{specificBoardName}' not found for dialogue trigger {gameObject.name}");
            }
        }
        
        // If specific board not found, try fallback board
        if (targetBoard == null && !string.IsNullOrEmpty(fallbackBoardName))
        {
            targetBoard = FindBoardByName(fallbackBoardName);
            
            if (targetBoard != null)
            {
                Debug.Log($"Using fallback board '{fallbackBoardName}' for dialogue trigger {gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"Fallback board '{fallbackBoardName}' not found for dialogue trigger {gameObject.name}");
            }
        }
        
        // If no specific or fallback board, try to use the first board in the project
        if (targetBoard == null && arcweavePlayer.aw.Project.boards != null && arcweavePlayer.aw.Project.boards.Count > 0)
        {
            targetBoard = arcweavePlayer.aw.Project.boards.First();
            Debug.LogWarning($"Using first available board for dialogue trigger {gameObject.name}");
        }
        
        // If we still don't have a board, log an error
        if (targetBoard == null)
        {
            Debug.LogError($"No valid board found for dialogue trigger {gameObject.name}. Dialogue won't work.");
        }
        else if (debugMode)
        {
            Debug.Log($"Board reference refreshed for dialogue trigger {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Finds a board by name in the current project
    /// </summary>
    private Board FindBoardByName(string boardName)
    {
        if (string.IsNullOrEmpty(boardName) || arcweavePlayer == null || arcweavePlayer.aw == null || arcweavePlayer.aw.Project == null)
        {
            return null;
        }
        
        if (arcweavePlayer.aw.Project.boards == null)
        {
            return null;
        }
        
        foreach (var board in arcweavePlayer.aw.Project.boards)
        {
            if (board != null && board.Name == boardName)
            {
                return board;
            }
        }
        
        return null;
    }
}