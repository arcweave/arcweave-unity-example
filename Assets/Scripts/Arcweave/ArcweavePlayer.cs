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
        private int navigationVersion;
        private Project.Project initializedProject;

        public bool HasSavedProgress => ArcweaveSave.HasSavedProgress;
        private bool IsInitialized => aw != null && aw.Project != null &&
                                      ReferenceEquals(initializedProject, aw.Project);

        //events that that UI (or otherwise) can subscribe to get notified and act accordingly.
        public event OnProjectStart onProjectStart;
        public event OnProjectFinish onProjectFinish;
        public event OnElementEnter onElementEnter;
        public event OnElementOptions onElementOptions;
        public event OnWaitingInputNext onWaitInputNext;
        public event OnWaitingInputNext onWaitInputFinish;
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
        /// Initialize once. Editor Play starts from imported defaults; builds resume saved progress.
        /// </summary>
        public void EnsureInitialized()
        {
            InitializeProject(restoreSavedProgress: !Application.isEditor);
        }

        private void InitializeProject(bool restoreSavedProgress)
        {
            if (IsInitialized) return;
            
            if (aw == null || aw.Project == null)
            {
                Debug.LogError("Cannot initialize Arcweave project - missing project asset");
                return;
            }
            
            aw.Project.Initialize();
            if (restoreSavedProgress) ArcweaveSave.TryRestore(aw.Project, out _);
            currentElement = null;
            navigationVersion++;
            initializedProject = aw.Project;
            
            if (onProjectUpdated != null) onProjectUpdated(aw.Project);
        }

        /// <summary>Installs an imported project and restores its compatible saved progress.</summary>
        public void SetProject(ArcweaveProjectAsset asset)
        {
            aw = asset;
            InitializeProject(restoreSavedProgress: true);
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
            if (!IsInitialized) return;
            
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
                Finish();
                return;
            }
            
            int version = ++navigationVersion;
            currentElement = element;
            currentElement.Visits++;
            
            // Check if element has content
            if (!currentElement.HasContent())
            {
                Debug.LogWarning($"Element '{currentElement.Title}' has no content");
            }
            else
            {
                currentElement.RunContentScript();
            }
            
            NotifyCurrent(onElementEnter, version, handler => handler(element));

            // A listener may have started another conversation while rendering this element.
            if (version != navigationVersion) return;

            if (GameManager.IsDialogueEnd(element))
            {
                WaitForFinish(version);
                return;
            }
            
            var currentState = currentElement.GetOptions();
            if (currentState.hasPaths) 
            {
                if (currentState.hasOptions) 
                {
                    NotifyCurrent(onElementOptions, version, handler =>
                        handler(currentState, (index) =>
                        {
                            if (version != navigationVersion || index < 0 || index >= currentState.Paths.Count) return;
                            Next(currentState.Paths[index]);
                        }));
                    return;
                }

                NotifyCurrent(onWaitInputNext, version, handler => handler(() =>
                    {
                        if (version == navigationVersion) Next(currentState.Paths[0]);
                    }));
                return;
            }

            WaitForFinish(version);
        }

        private void WaitForFinish(int version)
        {
            NotifyCurrent(onWaitInputFinish, version, handler => handler(() =>
                {
                    if (version == navigationVersion) Finish();
                }));
        }

        private void Finish()
        {
            if (currentElement == null) return;
            Save();
            currentElement = null;
            int version = ++navigationVersion;
            var finishedProject = aw.Project;
            NotifyCurrent(onProjectFinish, version, handler => handler(finishedProject));
        }

        // A listener can navigate synchronously. Do not send later listeners an obsolete presentation.
        private void NotifyCurrent<T>(T listeners, int version, System.Action<T> notify) where T : System.Delegate
        {
            if (listeners == null) return;
            foreach (T listener in listeners.GetInvocationList())
            {
                if (version != navigationVersion) return;
                notify(listener);
            }
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
            
            ArcweaveSave.Save(aw.Project, currentElement.Id);
        }

        /// <summary>
        /// Loads the previously current element and the variables and moves Next to that element.
        /// </summary>
        public void Load() 
        {
            TryLoad();
        }

        /// <summary>Loads progress, falling back to the start if the saved node was removed.</summary>
        public bool TryLoad()
        {
            EnsureInitialized();
            if (!IsInitialized) return false;
            if (!ArcweaveSave.TryRestore(aw.Project, out var id))
            {
                Debug.LogWarning("No compatible saved Arcweave progress is available for this project.");
                return false;
            }

            var element = string.IsNullOrEmpty(id) ? aw.Project.StartingElement : aw.Project.ElementWithId(id);
            if (element == null)
            {
                Debug.LogWarning("Cannot resume Arcweave progress - no starting element is available.");
                return false;
            }

            Next(element);
            return true;
        }

        /// <summary>
        /// Reset all variables to their default values
        /// </summary>
        public void ResetVariables() 
        {
            ArcweaveSave.Clear();
            currentElement = null;
            navigationVersion++;
            initializedProject = null;

            if (aw != null && aw.Project != null)
            {
                EnsureInitialized();
            }
        }
    }
}
