using UnityEngine;
using Arcweave;
using Arcweave.Project;
using TMPro;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Main game manager that handles game states, UI, and player controls
/// </summary>
public class GameManager : MonoBehaviour
{
    // Singleton instance
    public static GameManager Instance { get; private set; }
    
    public enum GameState
    {
        Gameplay,   // Normal gameplay state
        Dialogue,   // When player is in dialogue with NPCs
        Paused      // When game is paused (menu, settings, etc.)
    }
    
    [Header("Game State")]
    public GameState currentState = GameState.Gameplay;
    private GameState previousState;
    
    [Header("References")]
    public GameObject arcweaveUI;        // UI for dialogue system
    public GameObject importerUI;        // UI for importing Arcweave projects
    public PlayerController playerController;
    public ThirdPersonCamera cameraController;
    public GameObject characterWithAnimator;
    public Button quitButton;


    [Header("Message UI")]
    public TextMeshProUGUI messageText;  
    public float messageDisplayTime = 2f;
    
    [Header("Dialogue Tags")]
    public string dialogueEndTag = "dialogue_end";
    public string dialogueStartTag = "dialogue_start";
    
    [Header("Debug Settings")]
    public bool debugMode = false;
    
    // Event for state changes that other scripts can subscribe to
    public delegate void GameStateChangedDelegate(GameState newState);
    public event GameStateChangedDelegate OnGameStateChanged;
    
    private DialogueTrigger activeDialogueTrigger;
    private Animator characterAnimator;
    private Coroutine messageCoroutine;
    private bool isInitialized = false;
    
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
            return;
        }
        
        Initialize();
    }
    
    /// <summary>
    /// Initialize the game manager components
    /// </summary>
    private void Initialize()
    {
        if (isInitialized) return;
        
        // Find references if not assigned
        if (playerController == null)
        {
            playerController = FindAnyObjectByType<PlayerController>();
        }
        
        if (cameraController == null)
        {
            cameraController = FindAnyObjectByType<ThirdPersonCamera>();
        }
        
        if (characterWithAnimator != null)
        {
            characterAnimator = characterWithAnimator.GetComponent<Animator>();
        }
        
        // Set up UI
        if (arcweaveUI != null)
        {
            arcweaveUI.SetActive(false);
        }
        
        if (importerUI != null)
        {
            importerUI.SetActive(false);
        }
        
        // Set up quit button
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
        }
        
        // Set initial game state
        SetGameState(currentState);
        
        isInitialized = true;
        
        if (debugMode)
        {
            Debug.Log("GameManager initialized");
        }
    }
    
    private void Update()
    {
        // Handle pause menu toggle with Escape or Backspace
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }
    
    /// <summary>
    /// Set the game state and handle state transitions
    /// </summary>
    public void SetGameState(GameState newState)
    {
        if (newState == currentState) return;
        
        previousState = currentState;
        currentState = newState;
        
        if (debugMode)
        {
            Debug.Log($"Game state changed from {previousState} to {currentState}");
        }
        
        // Handle state-specific setup
        switch (currentState)
        {
            case GameState.Gameplay:
                EnterGameplayState();
                break;
                
            case GameState.Dialogue:
                EnterDialogueState();
                break;
                
            case GameState.Paused:
                EnterPausedState();
                break;
        }
        
        // Notify listeners of state change
        if (OnGameStateChanged != null)
        {
            OnGameStateChanged(currentState);
        }
    }
    
    /// <summary>
    /// Return to the previous game state
    /// </summary>
    public void ReturnToPreviousState()
    {
        SetGameState(previousState);
    }
    
    /// <summary>
    /// Toggles between pause and the previous game state
    /// </summary>
    public void TogglePause()
    {
        if (currentState == GameState.Paused)
        {
            ReturnToPreviousState();
        }
        else
        {
            // Don't pause if in dialogue
            if (currentState == GameState.Dialogue)
            {
                if (debugMode)
                {
                    Debug.Log("Cannot pause during dialogue");
                }
                return;
            }
            
            SetGameState(GameState.Paused);
        }
        
        if (debugMode)
        {
            Debug.Log($"Toggled pause. Current state: {currentState}");
        }
        
        // Show/hide importer UI based on pause state
        if (importerUI != null)
        {
            importerUI.SetActive(currentState == GameState.Paused);
        }
    }
    
    /// <summary>
    /// Resumes the game from paused state (for backward compatibility)
    /// </summary>
    public void ResumeGame()
    {
        if (currentState == GameState.Paused)
        {
            ReturnToPreviousState();
        }
        else
        {
            // If not paused, ensure we're in gameplay state
            SetGameState(GameState.Gameplay);
        }
        
        // Hide importer UI when resuming
        if (importerUI != null)
        {
            importerUI.SetActive(false);
        }
        
        if (debugMode)
        {
            Debug.Log("Game resumed");
        }
    }
    
    /// <summary>
    /// Enter gameplay state
    /// </summary>
    private void EnterGameplayState()
    {
        // Enable player controls
        if (playerController != null) playerController.enabled = true;
        if (cameraController != null) cameraController.enabled = true;
        
        // Hide dialogue UI
        if (arcweaveUI != null) arcweaveUI.SetActive(false);
        
        // Set animator state for gameplay
        if (characterAnimator != null)
        {
            characterAnimator.SetBool("isInDialogue", false);
        }
        
        // Lock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    /// <summary>
    /// Enter dialogue state
    /// </summary>
    private void EnterDialogueState()
    {
        // Show dialogue UI
        if (arcweaveUI != null) arcweaveUI.SetActive(true);
        
        // Disable player controls
        if (playerController != null) playerController.enabled = false;
        if (cameraController != null) cameraController.enabled = false;
        
        // Set animator state for dialogue
        if (characterAnimator != null)
        {
            characterAnimator.SetBool("isInDialogue", true);
        }
        
        // Unlock cursor for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    /// <summary>
    /// Enter paused state
    /// </summary>
    private void EnterPausedState()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        if (playerController != null) playerController.enabled = false;
        if (cameraController != null) cameraController.enabled = false;
        
        // Set animator state for dialogue
        if (characterAnimator != null)
        {
            characterAnimator.SetBool("isInDialogue", true);
        }
    }
    
    #region Dialogue Management
    
    /// <summary>
    /// Starts dialogue with a specific trigger
    /// </summary>
    public void StartDialogue(DialogueTrigger trigger)
    {
        if (trigger == null)
        {
            Debug.LogError("Cannot start dialogue with null trigger!");
            return;
        }
        
        activeDialogueTrigger = trigger;
        SetGameState(GameState.Dialogue);
        
        if (debugMode)
        {
            Debug.Log($"Started dialogue with {trigger.gameObject.name}");
        }
    }
    
    /// <summary>
    /// Ends current dialogue and returns to gameplay
    /// </summary>
    public void EndDialogue()
    {
        if (debugMode && activeDialogueTrigger != null)
        {
            Debug.Log($"Ending dialogue with {activeDialogueTrigger.gameObject.name}");
        }
        
        activeDialogueTrigger = null;
        SetGameState(GameState.Gameplay);
    }
    
    /// <summary>
    /// Returns the currently active dialogue trigger
    /// </summary>
    public DialogueTrigger GetActiveDialogueTrigger()
    {
        return activeDialogueTrigger;
    }

    /// <summary>
    /// Checks if an Arcweave element has the dialogue end tag
    /// </summary>
    public bool HasDialogueEndTag(Element element)
    {
        if (element == null || element.Attributes == null) return false;
        if (string.IsNullOrEmpty(dialogueEndTag)) return false;
        
        foreach (var attribute in element.Attributes)
        {
            if (attribute == null || attribute.data == null) continue;
            
            string data = attribute.data.ToString();
            if (data.Contains(dialogueEndTag))
            {
                if (debugMode)
                {
                    Debug.Log($"Element '{element.Title}' has dialogue end tag: {data}");
                }
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Checks if an Arcweave element has the dialogue start tag
    /// </summary>
    public bool HasDialogueStartTag(Element element)
    {
        if (element == null || element.Attributes == null) return false;
        if (string.IsNullOrEmpty(dialogueStartTag)) return false;
        
        foreach (var attribute in element.Attributes)
        {
            if (attribute == null || attribute.data == null) continue;
            
            string data = attribute.data.ToString();
            if (data.Contains(dialogueStartTag))
            {
                if (debugMode)
                {
                    Debug.Log($"Element '{element.Title}' has dialogue start tag: {data}");
                }
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Starts a project from a specific tag
    /// </summary>
    public void StartProjectFromTag(string tag, ArcweavePlayer player)
    {
        if (string.IsNullOrEmpty(tag))
        {
            Debug.LogError("Cannot start project with empty tag!");
            return;
        }
        
        if (player == null || player.aw == null || player.aw.Project == null)
        {
            Debug.LogError("ArcweavePlayer or project not initialized!");
            return;
        }
        
        // Ensure project is initialized
        player.EnsureInitialized();
        
        // Find an element with the specified tag
        foreach (var board in player.aw.Project.boards)
        {
            if (board == null || board.Nodes == null) continue;
            
            foreach (var node in board.Nodes)
            {
                if (node is Element element)
                {
                    if (element.Attributes == null) continue;
                    
                    foreach (var attribute in element.Attributes)
                    {
                        if (attribute == null || attribute.data == null) continue;
                        
                        string data = attribute.data.ToString();
                        if (data.Contains(tag))
                        {
                            Debug.Log($"Starting project from element with tag: {tag}");
                            player.Next(element);
                            return;
                        }
                    }
                }
            }
        }
        
        Debug.LogWarning($"No element found with tag: {tag}");
        player.PlayProject(); // Fallback to default start
    }
    
    #endregion
    
    #region UI Management
    
    /// <summary>
    /// Shows a temporary message on screen
    /// </summary>
    public void ShowMessage(string message, float duration = 0)
    {
        if (messageText == null) return;
        
        // Use default duration if none specified
        if (duration <= 0) duration = messageDisplayTime;
        
        // Cancel any existing message display
        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
        }
        
        // Start new message display
        messageCoroutine = StartCoroutine(ShowMessageCoroutine(message, duration));
    }
    
    /// <summary>
    /// Coroutine for displaying a message for a set duration
    /// </summary>
    private IEnumerator ShowMessageCoroutine(string message, float duration)
    {
        messageText.text = message;
        messageText.gameObject.SetActive(true);
        
        yield return new WaitForSeconds(duration);
        
        messageText.gameObject.SetActive(false);
        messageCoroutine = null;
    }
    
    #endregion
    
    #region Game Control
    
    /// <summary>
    /// Quit the game
    /// </summary>
    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    #endregion
}