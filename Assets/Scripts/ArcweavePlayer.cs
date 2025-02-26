using UnityEngine;
using Arcweave.Project;
using System.Linq;

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

        public const string SAVE_KEY = "arcweave_save";

        public Arcweave.ArcweaveProjectAsset aw;

        public bool autoStart = true;

        private Element currentElement;

        //events that that UI (or otherwise) can subscribe to get notified and act accordingly.
        public event OnProjectStart onProjectStart;
        public event OnProjectFinish onProjectFinish;
        public event OnElementEnter onElementEnter;
        public event OnElementOptions onElementOptions;
        public event OnWaitingInputNext onWaitInputNext;

        //...
        void Start() { if ( autoStart ) PlayProject(); }

        //...
        public void PlayProject() {
    if (aw == null) {
        Debug.LogError("There is no Arcweave Project assigned in the inspector of Arcweave Player");
        return;
    }

    aw.Project.Initialize();
    
    // Trova l'elemento iniziale con la logica personalizzata
    Element startingElement = FindStartingElement();
    
    if (startingElement == null) {
        Debug.LogError("No starting element found with the specified criteria");
        return;
    }

    if (onProjectStart != null) onProjectStart(aw.Project);
    
    Next(startingElement);
}

private Element FindStartingElement() {
    // Cerca in tutte le board
    foreach (var board in aw.Project.boards) {
        // Cerca gli elementi nella board
        foreach (var node in board.Nodes.OfType<Element>()) {
            // Controlla se l'elemento ha un componente "character"
            if (node.TryGetComponent("character", out var characterComponent)) {
                // Opzionalmente, puoi aggiungere ulteriori controlli sull'attributo
                var hasStartingDialogueAttribute = node.Attributes.Any(attr => 
                    attr.Name == "starting_dialogue_elements" && 
                    (bool)attr.data == true
                );
                
                if (hasStartingDialogueAttribute) {
                    return node;
                }
            }
        }
    }
    
    // Fallback all'elemento iniziale originale se non trova nulla
    return aw.Project.StartingElement;
}

        ///Moves to the next element through a path
        void Next(Path path) {
            path.ExecuteAppendedConnectionLabels();
            Next(path.TargetElement);
        }

        ///Moves to the next/an element directly
        void Next(Element element) {
            currentElement = element;
            currentElement.Visits++;
            if ( onElementEnter != null ) onElementEnter(element);
            var currentState = currentElement.GetOptions();
            if ( currentState.hasPaths ) {
                if ( currentState.hasOptions ) {
                    if ( onElementOptions != null ) {
                        onElementOptions(currentState, (index) => Next(currentState.Paths[index]));
                    }
                    return;
                }

                if ( onWaitInputNext != null ) onWaitInputNext(() => Next(currentState.Paths[0]));
                return;
            }
            currentElement = null;
            if ( onProjectFinish != null ) onProjectFinish(aw.Project);
        }

        ///----------------------------------------------------------------------------------------------

        ///Save the current element and the variables.
        public void Save() {
            var id = currentElement.Id;
            var variables = aw.Project.SaveVariables();
            PlayerPrefs.SetString(SAVE_KEY+"_currentElement", id);
            PlayerPrefs.SetString(SAVE_KEY+"_variables", variables);
        }

        ///Loads the prviously current element and the variables and moves Next to that element.
        public void Load() {
            var id = PlayerPrefs.GetString(SAVE_KEY+"_currentElement");
            var variables = PlayerPrefs.GetString(SAVE_KEY+"_variables");
            var element = aw.Project.ElementWithId(id);
            aw.Project.LoadVariables(variables);
            Next(element);
        }
    }
}