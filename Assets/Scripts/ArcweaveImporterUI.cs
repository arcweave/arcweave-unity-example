using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Handles the UI for importing Arcweave projects at runtime
/// </summary>
public class ArcweaveImporterUI : MonoBehaviour
{
    public RuntimeArcweaveImporter importer;
    
    [Header("UI References")]
    public TMP_InputField apiKeyInput;
    public TMP_InputField hashInput;
    public Button importButton;
    public Button resumeButton;
    public GameObject loadingIndicator;

    [Header("Message UI")]
    public TextMeshProUGUI messageText; // Questo è il testo nel pannello dei messaggi separato
    public float messageDisplayTime = 2f; // Tempo per cui mostrare il messaggio

    // PlayerPrefs keys for saving credentials
    private const string API_KEY_PREF = "ArcweaveAPIKey";
    private const string HASH_PREF = "ArcweaveProjectHash";

    private void Start()
    {
        InitializeImporter();
        SetupUI();
        SetupCallbacks();
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
        // Load saved credentials or use defaults from importer
        string savedApiKey = PlayerPrefs.GetString(API_KEY_PREF, importer.apiKey);
        string savedHash = PlayerPrefs.GetString(HASH_PREF, importer.projectHash);

        // Setup API Key input
        if (apiKeyInput != null)
        {
            apiKeyInput.text = savedApiKey;
            apiKeyInput.onValueChanged.AddListener((value) => {
                importer.SetApiKey(value);
                PlayerPrefs.SetString(API_KEY_PREF, value);
                PlayerPrefs.Save();
            });
        }

        // Setup Project Hash input
        if (hashInput != null)
        {
            hashInput.text = savedHash;
            hashInput.onValueChanged.AddListener((value) => {
                importer.SetProjectHash(value);
                PlayerPrefs.SetString(HASH_PREF, value);
                PlayerPrefs.Save();
            });
        }

        // Setup Import button
        if (importButton != null)
        {
            importButton.onClick.AddListener(() => {
                importer.ImportWithNewCredentials(apiKeyInput.text, hashInput.text);
            });
        }

        // Setup Resume button
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(() => {
                GameManager.Instance.ResumeGame();
            });
        }

        // Initialize loading indicator
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(false);
        }

        // Nascondi il messaggio all'inizio
        if (messageText != null)
        {
            messageText.gameObject.SetActive(false);
        }
    }

    private void SetupCallbacks()
    {
        importer.onImportStarted.AddListener(OnImportStarted);
        importer.onImportSuccess.AddListener(OnImportSuccess);
        importer.onImportFailed.AddListener(OnImportFailed);
    }

    #region Import Callbacks

    private void OnImportStarted()
    {
        if (importButton != null) importButton.interactable = false;
        if (loadingIndicator != null) loadingIndicator.SetActive(true);
        GameManager.Instance.ShowMessage("Importing...");
    }

    private void OnImportSuccess()
    {
        if (importButton != null) importButton.interactable = true;
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
        GameManager.Instance.ShowMessage("Import successful!");
    }

    private void OnImportFailed()
    {
        if (importButton != null) importButton.interactable = true;
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
        GameManager.Instance.ShowMessage("Import failed!");
    }

    #endregion

    private void ShowMessage(string message)
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

    private void OnDestroy()
    {
        CleanupListeners();
    }

    private void CleanupListeners()
    {
        // Cleanup input field listeners
        if (apiKeyInput != null)
            apiKeyInput.onValueChanged.RemoveListener((value) => {
                importer.SetApiKey(value);
                PlayerPrefs.SetString(API_KEY_PREF, value);
                PlayerPrefs.Save();
            });
        
        if (hashInput != null)
            hashInput.onValueChanged.RemoveListener((value) => {
                importer.SetProjectHash(value);
                PlayerPrefs.SetString(HASH_PREF, value);
                PlayerPrefs.Save();
            });
        
        // Cleanup button listeners
        if (importButton != null)
            importButton.onClick.RemoveListener(() => {
                importer.ImportWithNewCredentials(apiKeyInput.text, hashInput.text);
            });

        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(() => {
                GameManager.Instance.ResumeGame();
            });

        // Cleanup importer callbacks
        importer.onImportStarted.RemoveListener(OnImportStarted);
        importer.onImportSuccess.RemoveListener(OnImportSuccess);
        importer.onImportFailed.RemoveListener(OnImportFailed);
    }
} 