using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Arcweave.Project;

namespace Arcweave
{
    public class ArcweavePlayerUI : MonoBehaviour
    {
        [Header("References")]
        public ArcweavePlayer player;
        public Text content;
        public RawImage cover;
        public Button buttonTemplate;
        public Button saveButton;
        public Button loadButton;
        public RawImage componentCover;

        private const float CROSSFADE_TIME = 0.3f;
        private List<Button> tempButtons = new List<Button>();
        private bool isDialogueEndElement = false;

        void OnEnable() {
            // Initialize UI elements
            componentCover.gameObject.SetActive(false);
            buttonTemplate.gameObject.SetActive(false);
            
            // Set up button listeners
            saveButton.onClick.AddListener(Save);
            loadButton.onClick.AddListener(Load);
            if (!PlayerPrefs.HasKey(ArcweavePlayer.SAVE_KEY)) {
                loadButton.gameObject.SetActive(false);
            }

            // Subscribe to Arcweave events
            player.onElementEnter += OnElementEnter;
            player.onElementOptions += OnElementOptions;
            player.onWaitInputNext += OnWaitInputNext;
            player.onProjectFinish += OnProjectFinish;
        }

        void OnDisable() {
            // Unsubscribe from events
            player.onElementEnter -= OnElementEnter;
            player.onElementOptions -= OnElementOptions;
            player.onWaitInputNext -= OnWaitInputNext;
            player.onProjectFinish -= OnProjectFinish;
        }

        void Save() {
            player.Save();
            loadButton.gameObject.SetActive(true);
        }

        void Load() {
            ClearTempButtons();
            player.Load();
        }

        void OnElementEnter(Element element) {
            // Clear any existing buttons before processing new element
            ClearTempButtons();
            
            // Reset component cover visibility
            componentCover.gameObject.SetActive(false);
            
            // Set up content text
            content.text = "<i>[ No Content ]</i>";
            if (element.HasContent())
            {
                element.RunContentScript();
                content.text = element.RuntimeContent;
            }
            
            // Animate content fade-in
            content.canvasRenderer.SetAlpha(0);
            content.CrossFadeAlpha(1f, CROSSFADE_TIME, false);

            // Handle cover image
            var image = element.GetCoverOrFirstComponentImage();
            if (cover.texture != image && image != null) {
                cover.texture = image;
                cover.canvasRenderer.SetAlpha(0);
                cover.CrossFadeAlpha(1f, CROSSFADE_TIME, false);
            }
            if (image == null) {
                cover.canvasRenderer.SetAlpha(1);
                cover.CrossFadeAlpha(0f, CROSSFADE_TIME, false);
            }

            // Handle component cover image
            var compImage = element.GetFirstComponentCoverImage();
            if (componentCover.texture != compImage && compImage != null) {
                componentCover.texture = compImage;
                componentCover.canvasRenderer.SetAlpha(0);
                componentCover.CrossFadeAlpha(1f, CROSSFADE_TIME, false);
            }
            if (compImage == null) {
                componentCover.canvasRenderer.SetAlpha(1);
                componentCover.CrossFadeAlpha(0f, CROSSFADE_TIME, false);
            }
            
            // Check if current element has the dialogue_end tag
            DialogueTrigger activeTrigger = null;
            if (GameManager.Instance != null)
                activeTrigger = GameManager.Instance.GetActiveDialogueTrigger();
                
            isDialogueEndElement = activeTrigger != null && GameManager.Instance.HasDialogueEndTag(element);
            
            if (isDialogueEndElement)
                Debug.Log("Found element with dialogue_end tag");
        }

        void OnElementOptions(Options options, System.Action<int> callback) {
            for (var i = 0; i < options.Paths.Count; i++) {
                var index = i; // Create stable copy for the delegate
                var text = !string.IsNullOrEmpty(options.Paths[i].label) ? options.Paths[i].label : "<i>[ N/A ]</i>";
                
                Button button;
                if (isDialogueEndElement) {
                    // For dialogue_end elements, create button that ends dialogue after selection
                    button = MakeButton(text, () => {
                        // First end dialogue immediately
                        EndCurrentDialogue();
                        // Then process the callback
                        callback(index);
                    });
                } else {
                    // Normal behavior for non-ending elements
                    button = MakeButton(text, () => callback(index));
                }
                
                // Position the button
                var pos = button.transform.position;
                pos.y += buttonTemplate.GetComponent<RectTransform>().rect.height * (options.Paths.Count - 1 - i);
                button.transform.position = pos;
            }
        }
        
        // End the current dialogue
        void EndCurrentDialogue() {
            var activeTrigger = GameManager.Instance?.GetActiveDialogueTrigger();
            if (activeTrigger != null) {
                Debug.Log("Ending dialogue after option selection");
                activeTrigger.EndDialogue();
            }
        }

        void OnWaitInputNext(System.Action callback) {
            if (isDialogueEndElement) {
                // For dialogue_end elements with no options, create an "End Conversation" button
                MakeButton("End Conversation", () => {
                    // First end dialogue immediately
                    EndCurrentDialogue();
                    // Then run the callback
                    callback();
                });
            } else {
                // Normal continue button
                MakeButton("Continue...", callback);
            }
        }

        void OnProjectFinish(Project.Project p) {
            // At end of project, offer restart
            MakeButton("Restart", player.PlayProject);
        }

        Button MakeButton(string name, System.Action call) {
            // Create and set up a new button from the template
            var button = Instantiate<Button>(buttonTemplate);
            tempButtons.Add(button);

            // Set up button text and position
            var text = button.GetComponentInChildren<Text>();
            var image = button.GetComponent<Image>();
            text.text = name;
            button.transform.SetParent(buttonTemplate.transform.parent);
            button.transform.position = buttonTemplate.transform.position;
            button.gameObject.SetActive(true);

            // Animate button appearance
            text.canvasRenderer.SetAlpha(0);
            text.CrossFadeAlpha(1f, CROSSFADE_TIME, false);
            image.canvasRenderer.SetAlpha(0);
            image.CrossFadeAlpha(1f, CROSSFADE_TIME, false);

            // Add the onClick action
            button.onClick.AddListener(() => {
                ClearTempButtons();
                call();
            });

            return button;
        }

        public void ClearTempButtons() {
            // Clean up temporary buttons
            foreach (var b in tempButtons) {
                Destroy(b.gameObject);
            }
            tempButtons.Clear();
        }
    }
}