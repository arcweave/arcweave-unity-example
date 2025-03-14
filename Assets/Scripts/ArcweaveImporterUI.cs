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
    public TextMeshProUGUI pathInfoText;
    
    [Header("Other UI")]
    public Button resumeButton;
    public GameObject loadingIndicator;
    public TextMeshProUGUI messageText;
    public float messageDisplayTime = 2f;

    // PlayerPrefs keys for saving credentials
    private const string API_KEY_PREF = "ArcweaveAPIKey";
    private const string HASH_PREF = "ArcweaveProjectHash";
    private const string LOCAL_PATH_PREF = "ArcweaveLocalPath";

    private void Start()
    {
        InitializeImporter();
        SetupUI();
        SetupCallbacks();
        UpdatePathInfo();
    }

    private void InitializeImporter()
    {
        if (importer == null)
        {
            importer = FindObjectOfType<RuntimeArcweaveImporter>();
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
        }

        if (hashInput != null)
        {
            hashInput.text = savedHash;
            hashInput.onValueChanged.AddListener((value) => {
                importer.SetProjectHash(value);
                PlayerPrefs.SetString(HASH_PREF, value);
                PlayerPrefs.Save();
            });
        }
        
        // Setup local import fields
        if (localPathInput != null)
        {
            localPathInput.text = savedLocalPath;
            localPathInput.onValueChanged.AddListener((value) => {
                importer.SetLocalJsonFilePath(value);
                PlayerPrefs.SetString(LOCAL_PATH_PREF, value);
                PlayerPrefs.Save();
                UpdatePathInfo();
            });
        }

        // Setup buttons
        if (importWebButton != null)
        {
            importWebButton.onClick.AddListener(() => {
                importer.ImportFromWeb();
            });
        }
        
        if (importLocalButton != null)
        {
            importLocalButton.onClick.AddListener(() => {
                importer.ImportFromLocalFile();
            });
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(() => {
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
    
    private void UpdatePathInfo()
    {
        if (pathInfoText != null && importer != null)
        {
            pathInfoText.text = $"Posiziona il file JSON in:\n{importer.GetUserFriendlyPath()}";
        }
    }

    private void SetupCallbacks()
    {
        importer.onImportStarted.AddListener(OnImportStarted);
        importer.onImportSuccess.AddListener(OnImportSuccess);
        importer.onImportFailed.AddListener(OnImportFailed);
    }

    private void OnImportStarted()
    {
        if (importWebButton != null) importWebButton.interactable = false;
        if (importLocalButton != null) importLocalButton.interactable = false;
        if (loadingIndicator != null) loadingIndicator.SetActive(true);
        
        ShowMessage("Importing...");
    }

    private void OnImportSuccess()
    {
        if (importWebButton != null) importWebButton.interactable = true;
        if (importLocalButton != null) importLocalButton.interactable = true;
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
        
        ShowMessage("Import successful!");
    }

    private void OnImportFailed()
    {
        if (importWebButton != null) importWebButton.interactable = true;
        if (importLocalButton != null) importLocalButton.interactable = true;
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
        
        ShowMessage("Import failed!");
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
    }
} 