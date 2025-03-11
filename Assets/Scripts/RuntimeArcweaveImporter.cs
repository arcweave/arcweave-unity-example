using UnityEngine;
using Arcweave;
using UnityEngine.Events;

public class RuntimeArcweaveImporter : MonoBehaviour
{
    public ArcweaveProjectAsset arcweaveAsset;
    
    // Eventi che notificano lo stato dell'importazione
    public UnityEvent onImportStarted;
    public UnityEvent onImportSuccess;
    public UnityEvent onImportFailed;

    // Cambia questi valori e chiama ImportProject per reimportare
    public string apiKey;
    public string projectHash;

    [Header("Particle System Control")]
    public ParticleSystemController particleSystemController; // Riferimento al controller del Particle System

    private bool isImporting = false;

    private void Start()
    {
        // Opzionale: copia i valori iniziali dall'asset
        if (arcweaveAsset != null)
        {
            apiKey = arcweaveAsset.userAPIKey;
            projectHash = arcweaveAsset.projectHash;
        }
    }

    public void ImportProject()
    {
        if (isImporting)
        {
            Debug.LogWarning("Already importing project, please wait...");
            return;
        }

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(projectHash))
        {
            Debug.LogError("API Key and Project Hash must be set!");
            onImportFailed?.Invoke();
            return;
        }

        if (arcweaveAsset == null)
        {
            Debug.LogError("Arcweave Asset not assigned!");
            onImportFailed?.Invoke();
            return;
        }

        isImporting = true;
        onImportStarted?.Invoke();

        // Aggiorna i valori nell'asset
        arcweaveAsset.importSource = ArcweaveProjectAsset.ImportSource.FromWeb;
        arcweaveAsset.userAPIKey = apiKey;
        arcweaveAsset.projectHash = projectHash;

        // Importa il progetto
        arcweaveAsset.ImportProject(() => {
            isImporting = false;
            if (arcweaveAsset.Project != null)
            {
                Debug.Log("Project imported successfully!");
                onImportSuccess?.Invoke();
                
                // Aggiorna il Particle System dopo l'importazione
                UpdateParticleSystem();
            }
            else
            {
                Debug.LogError("Failed to import project!");
                onImportFailed?.Invoke();
            }
        });
    }

    private void UpdateParticleSystem()
    {
        if (particleSystemController != null)
        {
            particleSystemController.UpdateParticleSystem();
        }
        else
        {
            Debug.LogWarning("ParticleSystemController non assegnato!");
        }
    }

    // Metodi di utility per UI
    public void SetApiKey(string newKey)
    {
        apiKey = newKey;
    }

    public void SetProjectHash(string newHash)
    {
        projectHash = newHash;
    }

    public void ImportWithNewCredentials(string newApiKey, string newHash)
    {
        if (arcweaveAsset == null) return;

        arcweaveAsset.importSource = ArcweaveProjectAsset.ImportSource.FromWeb;
        arcweaveAsset.userAPIKey = newApiKey;
        arcweaveAsset.projectHash = newHash;
        
        // Importa il progetto e reinizializza il sistema
        arcweaveAsset.ImportProject(() => {
            if (arcweaveAsset.Project != null)
            {
                Debug.Log("Project imported successfully!");
                
                // Trova e reinizializza l'ArcweavePlayer
                var player = FindAnyObjectByType<ArcweavePlayer>();
                if (player != null)
                {
                    // Resetta le variabili e reinizializza il progetto
                    player.ResetVariables();
                    // Riassegna il progetto appena importato
                    player.aw = arcweaveAsset;
                    
                    // Se il gioco è in pausa, torna al gameplay
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.ResumeGame();
                    }
                }
                
                // Aggiorna il Particle System dopo l'importazione
                UpdateParticleSystem();
                
                // Notifica l'aggiornamento delle variabili
                var variableEvents = FindAnyObjectByType<ArcweaveVariableEvents>();
                if (variableEvents != null)
                {
                    // Resetta la condizione di disattivazione permanente prima di aggiornare le variabili
                    variableEvents.ResetObjectActivation();
                    variableEvents.UpdateSliderColor();
                    variableEvents.UpdateHealthFromVariable();
                    variableEvents.UpdateObjectActivation();
                }
                
                onImportSuccess?.Invoke();
            }
            else
            {
                Debug.LogError("Failed to import project!");
                onImportFailed?.Invoke();
            }
        });
    }
} 