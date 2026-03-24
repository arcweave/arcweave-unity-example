using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.IO;

/// <summary>
/// Handles the UI for importing Arcweave projects at runtime
/// </summary>
public class ArcweaveImporterUI : MonoBehaviour
{
    public RuntimeArcweaveImporter importer;
    
    [Header("Web Import UI")]
    public TMP_InputField apiKeyInput;
    public TMP_InputField hashInput;
    public Button importWebButton;
    
    [Header("Local Import UI")]
    public TMP_InputField localPathInput;
    public Button importLocalButton;
    
    [Header("Other UI")]
    public Button resumeButton;
    public GameObject loadingIndicator;
    public TextMeshProUGUI messageText;
    public float messageDisplayTime = 2f;
    
    [Header("Settings")]
    public bool autoCloseOnSuccess = true;
    public float autoCloseDelay = 1.5f;
    public bool debugMode = false;

    // PlayerPrefs keys for saving credentials
    private const string API_KEY_PREF = "ArcweaveAPIKey";
    private const string HASH_PREF = "ArcweaveProjectHash";
    private const string LOCAL_PATH_PREF = "ArcweaveLocalPath";
    
    // Reference to the panel that contains this UI
    private GameObject uiPanel;
    private Coroutine autoCloseCoroutine;

    private void Start()
    {
        // Get reference to the panel (this gameObject or its parent)
        uiPanel = transform.gameObject;
        
        InitializeImporter();
        SetupUI();
        SetupCallbacks();
      
        
        if (debugMode)
        {
            Debug.Log("ArcweaveImporterUI initialized");
        }
    }

    private void InitializeImporter()
    {
        if (importer == null)
        {
            importer = FindAnyObjectByType<RuntimeArcweaveImporter>();
        }
    }

    private void SetupUI()
    {
        // Load saved credentials
        string savedApiKey = PlayerPrefs.GetString(API_KEY_PREF, importer.apiKey);
        string savedHash = PlayerPrefs.GetString(HASH_PREF, importer.projectHash);
        string savedLocalPath = PlayerPrefs.GetString(LOCAL_PATH_PREF, importer.localJsonFilePath);

        // Setup web import fields
        if (apiKeyInput != null)
        {
            apiKeyInput.text = savedApiKey;
            apiKeyInput.onValueChanged.AddListener((value) => {
                importer.SetApiKey(value);
                PlayerPrefs.SetString(API_KEY_PREF, value);
                PlayerPrefs.Save();
            });
            
            // Set normal placeholder text
            apiKeyInput.placeholder.GetComponent<TextMeshProUGUI>().text = "Enter your Arcweave API Key";
        }

        if (hashInput != null)
        {
            hashInput.text = savedHash;
            hashInput.onValueChanged.AddListener((value) => {
                importer.SetProjectHash(value);
                PlayerPrefs.SetString(HASH_PREF, value);
                PlayerPrefs.Save();
            });
            
            // Set normal placeholder text
            hashInput.placeholder.GetComponent<TextMeshProUGUI>().text = "Enter your Arcweave Project Hash";
        }
        
        // Setup local import fields
        if (localPathInput != null)
        {
            localPathInput.text = savedLocalPath;
            localPathInput.onValueChanged.AddListener((value) => {
                importer.SetLocalJsonFilePath(value);
                PlayerPrefs.SetString(LOCAL_PATH_PREF, value);
                PlayerPrefs.Save();
            });
        }

        // Setup buttons
        if (importWebButton != null)
        {
            // Enable web import button
            importWebButton.interactable = true;
            
            // Reset colors to default
            ColorBlock colors = importWebButton.colors;
            colors.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.5f); // Default Unity disabled color
            importWebButton.colors = colors;
            
            // Reset button text
            TextMeshProUGUI buttonText = importWebButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = "Web Import";
            }
            
            // Set proper listener for web import
            importWebButton.onClick.RemoveAllListeners();
            importWebButton.onClick.AddListener(() => {
                importer.ImportFromWeb();
            });
        }
        
        if (importLocalButton != null)
        {
            importLocalButton.onClick.RemoveAllListeners();
            importLocalButton.onClick.AddListener(() => {
                importer.ImportFromLocalFile();
            });
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(() => {
                CloseImporterUI();
                
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ResumeGame();
                }
                else
                {
                    Debug.LogWarning("GameManager.Instance is null. Cannot resume game.");
                }
            });
        }

        // Initialize UI elements
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
        if (messageText != null) messageText.gameObject.SetActive(false);
    }
    
   

    private void SetupCallbacks()
    {
        if (importer != null)
        {
            importer.onImportStarted.RemoveListener(OnImportStarted);
            importer.onImportSuccess.RemoveListener(OnImportSuccess);
            importer.onImportFailed.RemoveListener(OnImportFailed);
            
            importer.onImportStarted.AddListener(OnImportStarted);
            importer.onImportSuccess.AddListener(OnImportSuccess);
            importer.onImportFailed.AddListener(OnImportFailed);
        }
        else
        {
            Debug.LogError("Importer reference is null. Cannot setup callbacks.");
        }
    }

    private void OnImportStarted()
    {
        // Cancel any auto-close coroutine
        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = null;
        }
        
        if (importWebButton != null) importWebButton.interactable = false;
        if (importLocalButton != null) importLocalButton.interactable = false;
        if (loadingIndicator != null) loadingIndicator.SetActive(true);
        
        ShowMessage("Importing...");
        
        if (debugMode)
        {
            Debug.Log("Import started");
        }
    }

    private void OnImportSuccess()
    {
        if (importWebButton != null) importWebButton.interactable = true;
        if (importLocalButton != null) importLocalButton.interactable = true;
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
        
        ShowMessage("Import successful!");
        
        if (debugMode)
        {
            Debug.Log("Import successful");
        }
        
        // Auto-close the panel after successful import
        if (autoCloseOnSuccess)
        {
            autoCloseCoroutine = StartCoroutine(AutoCloseAfterDelay());
        }
    }

    private void OnImportFailed()
    {
        if (importWebButton != null) importWebButton.interactable = true;
        if (importLocalButton != null) importLocalButton.interactable = true;
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
        
        ShowMessage("Import failed!");
        
        if (debugMode)
        {
            Debug.Log("Import failed");
        }
    }
    
    /// <summary>
    /// Closes the importer UI panel and returns to the game
    /// </summary>
    public void CloseImporterUI()
    {
        if (uiPanel != null)
        {
            uiPanel.SetActive(false);
            
            if (debugMode)
            {
                Debug.Log("Importer UI closed");
            }
        }
        else
        {
            Debug.LogWarning("UI Panel reference is null. Cannot close panel.");
        }
    }
    
    /// <summary>
    /// Auto-closes the importer UI after a delay
    /// </summary>
    private IEnumerator AutoCloseAfterDelay()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        
        CloseImporterUI();
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResumeGame();
        }
        
        autoCloseCoroutine = null;
    }

    private void ShowMessage(string message)
    {
        // Try to use GameManager if available
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ShowMessage(message);
            return;
        }
        
        // Fallback to local message display
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

    private void OnDestroy()
    {
        // Cleanup input field listeners
        if (apiKeyInput != null) apiKeyInput.onValueChanged.RemoveAllListeners();
        if (hashInput != null) hashInput.onValueChanged.RemoveAllListeners();
        if (localPathInput != null) localPathInput.onValueChanged.RemoveAllListeners();
        
        // Cleanup button listeners
        if (importWebButton != null) importWebButton.onClick.RemoveAllListeners();
        if (importLocalButton != null) importLocalButton.onClick.RemoveAllListeners();
        if (resumeButton != null) resumeButton.onClick.RemoveAllListeners();

        // Cleanup importer callbacks
        if (importer != null)
        {
            importer.onImportStarted.RemoveListener(OnImportStarted);
            importer.onImportSuccess.RemoveListener(OnImportSuccess);
            importer.onImportFailed.RemoveListener(OnImportFailed);
        }
        
        // Stop any running coroutines
        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
        }
    }
} 