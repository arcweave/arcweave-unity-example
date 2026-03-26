using UnityEngine;
using Arcweave.Project;

namespace Arcweave
{
    ///This is not required to utilize an arweave project but can be helpful for some projects as well as a learning example.
    public class ArcweavePlayer : MonoBehaviour
    {
        //Delegates for the events.
        public delegate void OnProjectStart(Project.Project project);
        public delegate void OnProjectFinish(Project.Project project);
        public delegate void OnElementEnter(Element element);
        public delegate void OnElementOptions(Options options, System.Action<int> next);
        public delegate void OnWaitingInputNext(System.Action next);
        public delegate void OnProjectUpdated(Project.Project project);

        public const string SAVE_KEY = "arcweave_save";

        public Arcweave.ArcweaveProjectAsset aw;

        public bool autoStart = true;

        private Element currentElement;
        private bool isInitialized = false;

        //events that that UI (or otherwise) can subscribe to get notified and act accordingly.
        public event OnProjectStart onProjectStart;
        public event OnProjectFinish onProjectFinish;
        public event OnElementEnter onElementEnter;
        public event OnElementOptions onElementOptions;
        public event OnWaitingInputNext onWaitInputNext;
        public event OnProjectUpdated onProjectUpdated;

        void Awake()
        {
            // Ensure we have a valid project asset
            if (aw == null)
            {
                Debug.LogError("No Arcweave Project Asset assigned to ArcweavePlayer");
            }
        }

        void Start() 
        { 
            if (autoStart) PlayProject(); 
        }

        /// <summary>
        /// Initialize the project if not already initialized
        /// </summary>
        public void EnsureInitialized()
        {
            if (isInitialized) return;
            
            if (aw == null || aw.Project == null)
            {
                Debug.LogError("Cannot initialize Arcweave project - missing project asset");
                return;
            }
            
            // Initialize the project
            aw.Project.Initialize();
            
            // Load saved variables if they exist
            if (PlayerPrefs.HasKey(SAVE_KEY + "_variables"))
            {
                var variables = PlayerPrefs.GetString(SAVE_KEY + "_variables");
                Debug.Log($"Loading variables: {variables}");
                aw.Project.LoadVariables(variables);
                
                // Verify variables were loaded correctly
                var currentVars = aw.Project.SaveVariables();
                Debug.Log($"Current variables after loading: {currentVars}");
            }
            else
            {
                Debug.Log("No saved variables found");
            }
            
            isInitialized = true;
            
            if (onProjectUpdated != null) onProjectUpdated(aw.Project);
        }

        /// <summary>
        /// Play the Arcweave project from the beginning
        /// </summary>
        public void PlayProject() 
        {
            if (aw == null) 
            {
                Debug.LogError("There is no Arcweave Project assigned in the inspector of Arcweave Player");
                return;
            }

            // Ensure project is initialized
            EnsureInitialized();
            
            Element startingElement = FindStartingElement();
            
            if (startingElement == null) 
            {
                Debug.LogError("No starting element found with the specified criteria");
                return;
            }

            if (onProjectStart != null) onProjectStart(aw.Project);
            
            Next(startingElement);
        }

        /// <summary>
        /// Find a suitable starting element for the project.
        /// Returns the starting element configured in the Arcweave project.
        /// Note: in the 3D demo, autoStart should be false — DialogueTrigger
        /// drives dialogue per-NPC via EnsureInitialized() + Next(element) directly.
        /// </summary>
        private Element FindStartingElement()
        {
            return aw?.Project?.StartingElement;
        }

        /// <summary>
        /// Moves to the next element through a path
        /// </summary>
        void Next(Path path) 
        {
            if (path == null)
            {
                Debug.LogError("Cannot navigate to null path");
                return;
            }
            
            path.ExecuteAppendedConnectionLabels();
            Next(path.TargetElement);
        }

        /// <summary>
        /// Moves to the next/an element directly
        /// </summary>
        public void Next(Element element) 
        {
            if (element == null)
            {
                Debug.LogError("Cannot navigate to null element");
                if (onProjectFinish != null) onProjectFinish(aw.Project);
                return;
            }
            
            currentElement = element;
            currentElement.Visits++;
            
            // Check if element has content
            if (!currentElement.HasContent())
            {
                Debug.LogWarning($"Element '{currentElement.Title}' has no content");
            }
            
            if (onElementEnter != null) onElementEnter(element);
            
            var currentState = currentElement.GetOptions();
            if (currentState.hasPaths) 
            {
                if (currentState.hasOptions) 
                {
                    if (onElementOptions != null) 
                    {
                        onElementOptions(currentState, (index) => Next(currentState.Paths[index]));
                    }
                    return;
                }

                if (onWaitInputNext != null) onWaitInputNext(() => Next(currentState.Paths[0]));
                return;
            }
            
            Save();
            
            currentElement = null;
            if (onProjectFinish != null) onProjectFinish(aw.Project);
        }

        /// <summary>
        /// Save the current element and the variables.
        /// </summary>
        public void Save() 
        {
            if (currentElement == null)
            {
                Debug.LogWarning("Cannot save - no current element");
                return;
            }
            
            var id = currentElement.Id;
            var variables = aw.Project.SaveVariables();
            Debug.Log($"Saving variables: {variables}");
            PlayerPrefs.SetString(SAVE_KEY+"_currentElement", id);
            PlayerPrefs.SetString(SAVE_KEY+"_variables", variables);
            PlayerPrefs.Save(); // Force immediate save
        }

        /// <summary>
        /// Loads the previously current element and the variables and moves Next to that element.
        /// </summary>
        public void Load() 
        {
            if (!PlayerPrefs.HasKey(SAVE_KEY+"_currentElement") || !PlayerPrefs.HasKey(SAVE_KEY+"_variables"))
            {
                Debug.LogWarning("Cannot load - no saved state found");
                return;
            }
            
            var id = PlayerPrefs.GetString(SAVE_KEY+"_currentElement");
            var variables = PlayerPrefs.GetString(SAVE_KEY+"_variables");
            
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError("Cannot load - invalid element ID");
                return;
            }
            
            var element = aw.Project.ElementWithId(id);
            if (element == null)
            {
                Debug.LogError($"Cannot load - element with ID '{id}' not found");
                return;
            }
            
            aw.Project.LoadVariables(variables);
            Next(element);
        }

        /// <summary>
        /// Reset all variables to their default values
        /// </summary>
        public void ResetVariables() 
        {
            PlayerPrefs.DeleteKey(SAVE_KEY + "_variables");
            PlayerPrefs.DeleteKey(SAVE_KEY + "_currentElement");
            
            if (aw != null && aw.Project != null)
            {
                aw.Project.Initialize(); // This will reset all variables to their default values
                isInitialized = true;
            }
        }
    }
}