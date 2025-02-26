using UnityEngine;
using Arcweave;

public class GameManager : MonoBehaviour
{
    // Singleton instance
    public static GameManager Instance { get; private set; }
    
    // Game states
    public enum GameState
    {
        Gameplay,
        Dialogue
    }
    
    [Header("Game State")]
    public GameState currentState = GameState.Gameplay;
    
    [Header("References")]
    public GameObject arcweaveUI;
    public PlayerController playerController;
    public ThirdPersonCamera cameraController;
    
    [Header("Dialogue Tags")]
    [Tooltip("Tag that indicates the end of dialogue")]
    public string dialogueEndTag = "dialogue_end";

    [Tooltip("Tag that indicates the start of dialogue")]
    public string dialogueStartTag = "dialogue_start";
    
    // Event that other scripts can subscribe to
    public delegate void GameStateChangedDelegate(GameState newState);
    public event GameStateChangedDelegate OnGameStateChanged;
    
    // Currently active dialogue trigger
    private DialogueTrigger activeDialogueTrigger;
    
    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Ensure UI is disabled at start
        if (arcweaveUI != null)
        {
            arcweaveUI.SetActive(false);
        }
        
        // Set initial state
        SetGameState(GameState.Gameplay);
    }
    
    // Method to change game state
    public void SetGameState(GameState newState)
    {
        // Skip if no change
        if (currentState == newState) return;
        
        currentState = newState;
        
        // Handle state changes
        switch (newState)
        {
            case GameState.Gameplay:
                EnableGameplayControls();
                break;
            case GameState.Dialogue:
                DisableGameplayControls();
                break;
        }
        
        // Notify other scripts of state change
        OnGameStateChanged?.Invoke(newState);
    }
    
    // Enable gameplay controls
    private void EnableGameplayControls()
    {
        // Hide and lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Enable player and camera controls
        if (playerController != null)
            playerController.enabled = true;
            
        if (cameraController != null)
            cameraController.enabled = true;
            
        // Deactivate Arcweave UI immediately
        if (arcweaveUI != null)
            arcweaveUI.SetActive(false);
            
        // Clear current dialogue trigger reference
        activeDialogueTrigger = null;
    }
    
    // Disable gameplay controls and enable dialogue UI
    private void DisableGameplayControls()
    {
        // Show and unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Disable player and camera controls
        if (playerController != null)
            playerController.enabled = false;
            
        if (cameraController != null)
            cameraController.enabled = false;
            
        // Show Arcweave UI
        if (arcweaveUI != null)
            arcweaveUI.SetActive(true);
    }
    
    // Method to start dialogue
    public void StartDialogue(DialogueTrigger trigger)
    {
        if (trigger == null || trigger.arcweavePlayer == null)
        {
            Debug.LogError("Invalid dialogue trigger or Arcweave Player!");
            return;
        }

        // Initialize project if needed
  

        activeDialogueTrigger = trigger;
        SetGameState(GameState.Dialogue);
    }
    
    // Method to end dialogue
    public void EndDialogue()
    {
        SetGameState(GameState.Gameplay);
    }
    
    // Get the active dialogue trigger
    public DialogueTrigger GetActiveDialogueTrigger()
    {
        return activeDialogueTrigger;
    }

    // Modifica la funzione HasDialogueEndTag per usare la variabile serializzata
    public bool HasDialogueEndTag(Arcweave.Project.Element element)
    {
        foreach (var attribute in element.Attributes)
        {
            string data = attribute.data?.ToString();
            if (data != null && data.Contains(dialogueEndTag))
            {
                return true;
            }
        }
        return false;
    }

    // Aggiungi questa nuova funzione
    public void StartProjectFromTag(string tag, ArcweavePlayer player)
    {
        if (player == null || player.aw == null)
        {
            Debug.LogError("Arcweave Player is not assigned!");
            return;
        }

        foreach (var board in player.aw.Project.boards)
        {
            foreach (var node in board.Nodes)
            {
                if (node is Arcweave.Project.Element element)
                {
                    foreach (var attribute in element.Attributes)
                    {
                        string data = attribute.data?.ToString();
                        if (data != null && data.Contains(tag))
                        {
                            player.aw.Project.StartingElement = element;
                            player.PlayProject();
                            return;
                        }
                    }
                }
            }
        }

        Debug.LogWarning($"No element found with tag '{tag}'");
        player.PlayProject();
    }

    // Modifica la funzione StartDialogueFromTag per usare la variabile serializzata
    public void StartDialogueFromTag(ArcweavePlayer player)
    {
        if (player == null || player.aw == null)
        {
            Debug.LogError("Arcweave Player is not assigned!");
            return;
        }

        foreach (var board in player.aw.Project.boards)
        {
            foreach (var node in board.Nodes)
            {
                if (node is Arcweave.Project.Element element)
                {
                    foreach (var attribute in element.Attributes)
                    {
                        string data = attribute.data?.ToString();
                        if (data != null && data.Contains(dialogueStartTag))
                        {
                            player.aw.Project.StartingElement = element;
                            player.PlayProject();
                            return;
                        }
                    }
                }
            }
        }

        Debug.LogWarning($"No element found with tag '{dialogueStartTag}'");
        player.PlayProject();
    }

    
}