using UnityEngine;
using Arcweave;
using Arcweave.Project;
using TMPro;
using System.Collections;
using UnityEngine.UI;

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
    public GameObject characterWithAnimator;  // Reference to the GameObject with the Animator
    public Button quitButton;            // Reference to the quit game button

    private Animator animator;  // Reference to the character's Animator

    [Header("Message UI")]
    public TextMeshProUGUI messageText;  // Text component for displaying temporary messages
    public float messageDisplayTime = 2f; // How long messages stay on screen
    
    [Header("Dialogue Tags")]
    [Tooltip("Tag that indicates the end of dialogue")]
    public string dialogueEndTag = "dialogue_end";
    [Tooltip("Tag that indicates the start of dialogue")]
    public string dialogueStartTag = "dialogue_start";
    
    // Event for state changes that other scripts can subscribe to
    public delegate void GameStateChangedDelegate(GameState newState);
    public event GameStateChangedDelegate OnGameStateChanged;
    
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
        Application.targetFrameRate = 60;
        
        // Initialize animator
        if (characterWithAnimator != null)
        {
            animator = characterWithAnimator.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("No Animator component found on characterWithAnimator!");
            }
            else
            {
                Debug.Log("Animator initialized successfully");
                // Verify the parameter exists
                foreach (AnimatorControllerParameter param in animator.parameters)
                {
                    if (param.name == "isInDialogue")
                    {
                        Debug.Log("Found isInDialogue parameter in Animator");
                        break;
                    }
                }
            }
        }
        else
        {
            Debug.LogError("characterWithAnimator is not assigned in GameManager!");
        }

        // Initialize UI states
        if (arcweaveUI != null) arcweaveUI.SetActive(false);
        
        // Setup quit button
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }
        
        SetGameState(GameState.Gameplay);
    }
    
    private void Update()
    {
        // Handle pause menu toggle with Escape or Backspace
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
        {
            TogglePause();
        }
    }
    
    /// <summary>
    /// Toggles between pause and the previous game state
    /// </summary>
    private void TogglePause()
    {
        if (currentState == GameState.Paused)
        {
            SetGameState(previousState);
        }
        else
        {
            previousState = currentState;
            SetGameState(GameState.Paused);
        }
    }
    
    /// <summary>
    /// Changes the game state and handles all related state changes
    /// </summary>
    public void SetGameState(GameState newState)
    {
        if (currentState == newState) return;
        
        currentState = newState;
        
        switch (newState)
        {
            case GameState.Gameplay:
                EnableGameplayControls();
                if (importerUI != null) importerUI.SetActive(false);
                if (arcweaveUI != null) arcweaveUI.SetActive(false);
                Time.timeScale = 1f;
                break;
            
            case GameState.Dialogue:
                DisableGameplayControls();
                if (importerUI != null) importerUI.SetActive(false);
                if (arcweaveUI != null) arcweaveUI.SetActive(true);
                Time.timeScale = 1f;
                break;
            
            case GameState.Paused:
                DisableGameplayControls();
                if (importerUI != null) importerUI.SetActive(true);
                // We don't pause time to allow for importing
                break;
        }
        
        OnGameStateChanged?.Invoke(newState);
    }
    
    /// <summary>
    /// Enables player controls and hides cursor
    /// </summary>
    private void EnableGameplayControls()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        if (playerController != null) playerController.enabled = true;
        if (cameraController != null) cameraController.enabled = true;
        
        // Set animator state for gameplay
        if (animator != null)
        {
            animator.SetBool("isInDialogue", false);
            Debug.Log($"Setting isInDialogue to false. Current value: {animator.GetBool("isInDialogue")}");
        }
        else
        {
            Debug.LogError("Animator is null in EnableGameplayControls!");
        }
        
        activeDialogueTrigger = null;
    }
    
    /// <summary>
    /// Disables player controls and shows cursor
    /// </summary>
    private void DisableGameplayControls()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        if (playerController != null) playerController.enabled = false;
        if (cameraController != null) cameraController.enabled = false;
        
        // Set animator state for dialogue
        if (animator != null)
        {
            animator.SetBool("isInDialogue", true);
            Debug.Log($"Setting isInDialogue to true. Current value: {animator.GetBool("isInDialogue")}");
        }
        else
        {
            Debug.LogError("Animator is null in DisableGameplayControls!");
        }
    }
    
    #region Dialogue Management
    
    /// <summary>
    /// Starts dialogue with a specific trigger
    /// </summary>
    public void StartDialogue(DialogueTrigger trigger)
    {
        activeDialogueTrigger = trigger;
        SetGameState(GameState.Dialogue);
    }
    
    /// <summary>
    /// Ends current dialogue and returns to gameplay
    /// </summary>
    public void EndDialogue()
    {
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

    /// <summary>
    /// Starts an Arcweave project from a specific tag
    /// </summary>
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
                if (node is Element element)
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
    
    #endregion

    #region Game State Control
    
    /// <summary>
    /// Pauses the game if not already paused
    /// </summary>
    public void PauseGame()
    {
        if (currentState != GameState.Paused)
        {
            previousState = currentState;
            SetGameState(GameState.Paused);
        }
    }

    /// <summary>
    /// Resumes the game to the previous state
    /// </summary>
    public void ResumeGame()
    {
        if (currentState == GameState.Paused)
        {
            SetGameState(previousState);
        }
    }
    
    #endregion

    #region Message System
    
    /// <summary>
    /// Shows a temporary message on screen
    /// </summary>
    public void ShowMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
            messageText.gameObject.SetActive(true);
            StartCoroutine(HideMessageAfterDelay());
        }
    }

    private IEnumerator HideMessageAfterDelay()
    {
        yield return new WaitForSeconds(messageDisplayTime);
        if (messageText != null)
        {
            messageText.gameObject.SetActive(false);
        }
    }
    
    #endregion

    private void OnDestroy()
    {
        // Clean up button listener
        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
        }
    }

    /// <summary>
    /// Quits the game
    /// </summary>
    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
        Debug.Log("Quitting game...");
    }
}