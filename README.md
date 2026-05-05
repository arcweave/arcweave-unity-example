# Arcweave Unity Example

This project demonstrates Arcweave integration with Unity, allowing you to import Arcweave projects both during development and at runtime.

![Arcweave Unity Example image](https://github.com/user-attachments/assets/f23a79a7-03c8-4d02-a899-bff6a2271c98)

You can preview the example project directly in your browser https://arcweave.itch.io/arcweave-unity-example

## Features

- Import Arcweave projects from web (API key + project hash) or local JSON
- Preloaded project support included in builds
- Conditional dialogue branching driven by Arcweave variables
- Arcweave component-based dialogue start/end detection
- Variable-to-gameplay binding (health bars, object activation, UI colors)
- Scene control (lighting, particles) driven by Arcweave attributes
- Sword swing ability unlocked at a specific narrative beat
- Character portraits loaded from Arcweave components
- Runtime narrative update without rebuilding (local JSON or web API)

## Quick Start

1. Clone this repository
2. Open the project in Unity Hub (Unity 6 or Unity 2022 LTS)
3. The Arcweave Unity plugin is already included -- no extra setup needed
4. Press Play to test the demo scene

To learn more about the plugin: https://github.com/Arcweave/arcweave-unity-plugin

## Including an Arcweave Project in Your Build

### Preparation

1. Export your Arcweave project in Unity format
2. Place the JSON file in `Assets/Arcweave/project.json`
3. Place all related images and audio files in `Assets/Resources`
4. Alternatively, use the web importer at runtime (see below)

### Import

1. Select or create an `ArcweaveProjectAsset` in `Assets/Arcweave/`
   - To create a new one: right-click in Assets > **Create > Arcweave > Project Asset**
2. In the Inspector, choose your import method:
   - **From JSON**: assign the exported `.json` file
   - **From Web**: enter your **API key** and **project hash**
3. Click **Generate Project**
4. After import, the Inspector shows variables (useful for debugging). Click **Open Project Viewer** to browse boards, elements, and connections.

*Remember to connect your Arcweave Project Asset to the ArcweaveImporter and ArcweavePlayer GameObjects.*

### How to Use This Template

1. **Arcweave project**: create boards (one per NPC), add variables, write conditional branches, attach components to elements, export
2. **Unity -- import**: assign JSON to `ArcweaveProjectAsset`, click Generate Project
3. **Unity -- NPC**: add `DialogueTrigger` with the board name matching the Arcweave board
4. **Unity -- GameManager**: set `Dialogue Start/End Component Name` if using component-based detection
5. **Unity -- portraits**: place images in `Assets/Resources/` with filenames matching Arcweave
6. **Unity -- audio**: place audio files in `Assets/Resources/`; add `ArcweavePlayerAudio` to NPCs that have audio in their board
7. **Unity -- variable binding**: use `ArcweaveObjectActivation` for object activation and `ArcweaveHealthUI` for health/slider
8. **Build**: `File > Build Settings > Build`. The `arcweave/` folder is created automatically

### Media Management

Images and audio files are searched in the same order:
1. Unity's `Resources` folder (baked into the build)
2. `[Build Folder]/arcweave/resources/` (drop files here to update post-build without rebuilding)

## For End Users

### Importing from Web

1. Launch the application
2. Press **Esc** to open the menu
3. Enter your API key and project hash
4. Click **Import Web**

### Importing from Local File

1. Place your JSON in `[Build Folder]/arcweave/project.json`
2. Place images and audio files in `[Build Folder]/arcweave/resources/`
3. Press **Esc** > click **Import Local**

### Troubleshooting

- **JSON not loading**: verify the file is exported from Arcweave in Unity format
- **Images missing**: filenames must match those in the Arcweave project
- **API key / hash invalid**: check your Arcweave account settings and project URL
- **NPC not talking**: check that `Specific Board Name` matches the Arcweave board name exactly (case-sensitive)
- **Dialogue stuck**: the last element needs a `dialogue_end` tag or `DialogueEnd` component
- **Object not activating**: check that `ArcweaveObjectActivation` is on an always-active object (e.g. the NPC) and points to the hidden object; variable name must match exactly (e.g. `sword_locked`); object must start **inactive**; variable must be boolean, default `true`
- **Sword swing not unlocking**: `Component Name` in `SwordSwingHandler` must be `Attack` (exact match); the `Attack` component must be attached to the element in Arcweave; the Animator needs an `Attack` Trigger parameter
- **Audio not playing**: verify `ArcweavePlayerAudio` is on the NPC GameObject; check that audio assets are uploaded and assigned to elements in Arcweave; audio files must be in `Assets/Resources/` (editor/build) or `[Build]/arcweave/resources/` (post-build); supported formats: mp3, wav, ogg, aiff
- **Audio not stopping (`Stop` mode)**: `ArcweaveAudioManager` must be present in the scene; add it to a persistent GameObject (e.g. `GameManager`)

---

## For Developers

### Scripts Structure

```
Assets/Scripts/
├── Arcweave/       -- ArcweavePlayer, ArcweavePlayerUI, ArcweaveImageLoader,
|                      RuntimeArcweaveImporter, ArcweaveImporterUI,
|                      ArcweaveAudioManager, ArcweaveAudioLoader, ArcweavePlayerAudio
├── Handlers/       -- ArcweaveAttributeHandler, ArcweaveElementComponentHandler,
|                      ArcweaveSceneController,
|                      SwordSwingHandler, ArcweaveHealthUI, ArcweaveObjectActivation
├── Game/           -- GameManager, PlayerController, ThirdPersonCamera, DialogueTrigger
└── Editor/         -- ArcweaveBuildProcessor
```

---

### Core Scripts

#### ArcweavePlayer

Central controller for Arcweave narratives. Manages project state, navigation between elements, variable state, and save/load. Fires events that UI and other scripts subscribe to.

**Key events:**
- `onProjectStart(Project)` / `onProjectFinish(Project)` -- project lifecycle
- `onProjectUpdated(Project)` -- fires after project is initialized or reloaded
- `onElementEnter(Element)` -- new dialogue node reached
- `onElementOptions(Options, Action<int>)` -- player choices available
- `onWaitInputNext(Action)` -- single path forward, caller invokes to advance

```csharp
arcweavePlayer.onElementEnter += (element) => {
    Debug.Log($"Entered: {element.Title}");
};
```

#### ArcweavePlayerUI

Subscribes to `ArcweavePlayer` events and renders dialogue text, images, choices, and variable debug list. Configure UI references in the Inspector.

| Inspector Field | Purpose |
|----------------|---------|
| `noContentText` | Text shown when an element has no content |
| `emptyOptionText` | Text shown for options with no label |
| `continueButtonText` | Label for the continue button |
| `restartButtonText` | Label for the restart button |
| `enableFade` | Enable fade animation for text entries |

#### DialogueTrigger

Proximity-based NPC interaction (in `Game/` folder). Measures distance to the player each frame; shows a prompt and starts dialogue on key press.

| Inspector Field | Purpose |
|----------------|---------|
| `Specific Board Name` | Arcweave board name for this NPC (case-sensitive) |
| `Arcweave Player` | Reference to ArcweavePlayer |
| `Trigger Distance` | Interaction radius in meters |
| `Interaction Key` | Key to start dialogue (default: E) |
| `Interaction Text` | TextMeshPro for the "Press E" prompt |

#### ArcweaveHealthUI

Connects an Arcweave health variable to a Unity Slider, text, and animator. Also reads a color attribute to style the slider fill.

| Inspector Field | Purpose |
|----------------|---------|
| `healthVariableName` | Arcweave variable to read (e.g. `wanda_health`) |
| `healthBar` (Slider) | UI Slider for the health bar |
| `maxHealth` | Full scale value for the slider |
| `healthText` (TMP) | Optional text showing numeric value |
| `faceCamera` | Rotate the health bar to face the camera |
| `healthyAnimatorParam` | Animator bool for healthy state (default: `Healthy`) |
| `healthyThreshold` | Percentage threshold for healthy (default: 0.4) |
| `sliderColorComponentName` | Arcweave component for color (default: `UI Settings`) |
| `sliderColorAttribute` | Attribute name for hex color (default: `SliderColor`) |

#### ArcweaveObjectActivation

Toggles a GameObject based on an Arcweave boolean variable. Uses inverted logic: `true` = object inactive, `false` = object activates.

| Inspector Field | Purpose |
|----------------|---------|
| `targetObject` | GameObject to activate/deactivate |
| `activationVariableName` | Boolean variable name (e.g. `sword_locked`) |

**Object Activation naming convention:** name variables for the locked/closed state (e.g. `sword_locked`). Default `true` = hidden. When flipped to `false`, the object activates.

#### ArcweaveSceneController

Reads the `SceneSettings` component on project load/import (in `Handlers/` folder). Attributes: `Time` (`"Day"` / `"Night"`) controls camera background; `ParticleState` (`"rain"` / `"clear"`) toggles particle systems.

| Inspector Field | Purpose |
|----------------|---------|
| `dayValue` | Value for daytime (default: `Day`) |
| `nightValue` | Value for nighttime (default: `Night`) |
| `nightColor` | Background color for night (default: black) |

#### ArcweaveAudioManager

Optional singleton (`DontDestroyOnLoad`) that acts as a scene-wide event bus for audio stop signals. Required only when using the `Stop` playback mode on audio assets — it broadcasts a stop signal to all `ArcweavePlayerAudio` instances in the scene so a clip can be halted across multiple characters simultaneously.

Add it to any persistent GameObject (e.g. `GameManager`). If not present, `Once` and `Loop` modes work normally; only `Stop` mode has no effect.

#### ArcweavePlayerAudio

Add to any NPC that has audio assets assigned to its Arcweave elements. Subscribes to `ArcweavePlayer.onElementEnter` and plays the audio assets attached to each element, but only while the `DialogueTrigger` reports an active dialogue — preventing stray audio if events fire outside a conversation.

Multiple audio assets can be assigned to a single element in Arcweave; each is played independently with its own settings. Audio assets are configured entirely in Arcweave (upload via the **Assets** tab, assign to elements). No audio clips are referenced directly in Unity.

For each asset, the script first calls `TryGetAudioClip()` (resolves from Unity `Resources`). If that returns null, it falls back to `ArcweaveAudioLoader`, which loads the file asynchronously from `[Build]/arcweave/resources/` and caches it. This means audio files can be added or swapped post-build without rebuilding.

| Inspector Field | Purpose |
|----------------|---------|
| `player` | Reference to `ArcweavePlayer` (auto-found on the same GameObject if empty) |
| `dialogueTrigger` | Reference to `DialogueTrigger` (auto-found on the same GameObject if empty) |
| `debugMode` | Enable console logs for audio events |

**Playback modes (set in Arcweave):**

| Mode | Behavior |
|------|----------|
| `Once` | Plays the clip once, then stops |
| `Loop` | Loops the clip until explicitly stopped |
| `Stop` | Stops the named clip on all `ArcweavePlayerAudio` instances (requires `ArcweaveAudioManager` in scene) |

Each audio asset also supports `volume` (0–1) and `delay` (seconds before playback starts), both set in Arcweave. Delayed clips use `AudioSource.PlayDelayed()`. Non-looping `AudioSource` components are destroyed when the element changes; looping sources persist until explicitly stopped via `Stop` mode.

#### ArcweaveImageLoader

Singleton that loads images by filename from Resources, then `[Build]/arcweave/resources/`, then custom paths. Caches loaded images for performance.

#### ArcweaveAudioLoader

Singleton that loads audio files at runtime using the same pattern as `ArcweaveImageLoader`. Checks Unity's `Resources` folder first, then `[Build]/arcweave/resources/`. Loading from the build folder is async (`UnityWebRequest`); clips are cached by filename after the first load.

Used automatically by `ArcweavePlayerAudio` as a fallback when `TryGetAudioClip()` returns null — no manual setup needed. To support post-build audio updates, place audio files in `[Build]/arcweave/resources/` alongside images.

#### RuntimeArcweaveImporter

Async import from web API or local JSON at runtime. Provides `onImportSuccess` / `onImportFailure` events. Saves credentials between sessions.

#### ArcweaveImporterUI

UI panel for runtime import. Accessible via Esc key. Supports both web and local JSON import.

#### GameManager

Singleton state machine: `Gameplay`, `Dialogue`, `Paused`. Controls cursor lock, player/camera enable, and UI visibility.

| Inspector Field | Purpose |
|----------------|---------|
| `dialogueStartTag` | Attribute tag for dialogue start (default: `dialogue_start`) |
| `dialogueEndTag` | Attribute tag for dialogue end (default: `dialogue_end`) |
| `dialogueStartComponentName` | Component name for dialogue start (optional, overrides tag) |
| `dialogueEndComponentName` | Component name for dialogue end (optional, overrides tag) |
| `dialogueAnimatorParam` | Animator bool parameter for dialogue state (default: `IsInDialogue`) |

Detection priority: component name first, attribute tag fallback. Existing boards using tags continue to work.

#### PlayerController

Camera-relative WASD movement. Drives Animator `Speed` float. Sword swing on left mouse button when `canSwing` is `true` (set by `SwordSwingHandler`).

| Inspector Field | Purpose |
|----------------|---------|
| `speedAnimatorParam` | Animator float parameter for movement speed |
| `attackAnimatorParam` | Animator trigger parameter for attack |
| `attackStateName` | Animator state name for the attack animation |

#### ThirdPersonCamera

Follows a target Transform with mouse-driven rotation. Disabled by `GameManager` during Dialogue and Paused states.

#### ArcweaveBuildProcessor

Editor-only post-build hook. Creates `arcweave/` and `arcweave/resources/` in the build output; copies `project.json` if present.

---

### Extension Pattern -- Two Abstract Classes (in `Handlers/`)

Two base classes for extending Arcweave-driven behavior. The designer configures data in Arcweave; the developer implements one method in Unity.

| | `ArcweaveAttributeHandler` | `ArcweaveElementComponentHandler` |
|---|---|---|
| **Fires when** | Project loads / imports | Every element change during dialogue |
| **Reads** | Static attribute from a global component | Component on the current element |
| **Good for** | Config, UI styling, static values | Runtime reactions to narrative beats |

#### ArcweaveAttributeHandler

Reads a named attribute from a named component at project load. Override `ApplyAttributeValue(string value)`.

```csharp
public class MyHandler : ArcweaveAttributeHandler
{
    protected override void ApplyAttributeValue(string value)
    {
        // React to the attribute value
    }
}
```

| Inspector Field | Purpose |
|----------------|---------|
| `componentName` | Arcweave component to search for |
| `attributeName` | Attribute to read from that component |

**Concrete example:** `ArcweaveHealthUI` reads a slider color attribute from a component and applies it to the health bar slider fill.

#### ArcweaveElementComponentHandler

Watches `ArcweavePlayer.onElementEnter`. Checks if the current element has a component matching `componentName`. Override `OnComponentDetected` and optionally `OnComponentAbsent`.

```csharp
public class MyHandler : ArcweaveElementComponentHandler
{
    protected override void OnComponentDetected(Element element, Component component)
    {
        string value = GetAttributeValue(component, "MyAttribute");
        // React to the component
    }
}
```

| Inspector Field | Purpose |
|----------------|---------|
| `arcweavePlayer` | Reference to ArcweavePlayer (auto-found if empty) |
| `componentName` | Arcweave component to watch for (case-sensitive) |

**Concrete example:** `SwordSwingHandler` -- detects the `Attack` component, reads `SwingSpeed` attribute, calls `PlayerController.EnableSwordSwing(speed)`. Once unlocked, the ability stays for the session.

| Inspector Field | Purpose |
|----------------|---------|
| `Component Name` | `Attack` (must match Arcweave component) |
| `Player Controller` | Reference to the player's PlayerController |
| `Swing Speed Attribute` | Name of the speed attribute (default: `SwingSpeed`) |

---

### Save/Load

Uses `PlayerPrefs`. Key prefix: `arcweave_save`. Saves current element ID and all variable values as JSON. Call `ArcweavePlayer.Save()` / `Load()`.

---

## Compatibility

- **Unity 6** (6000.0.x) -- primary development version
- **Unity 2022 LTS** (2022.3.x) -- tested and working. Unity Hub automatically resolves package versions.
- **Arcweave plugin**: included in the project. Standalone plugin: https://github.com/Arcweave/arcweave-unity-plugin
