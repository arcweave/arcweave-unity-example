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
2. Open the project in Unity Hub (validated with Unity 6000.0.39f1)
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
2. **Unity - import**: assign JSON to `ArcweaveProjectAsset`, click Generate Project
3. **Unity - NPC**: add `DialogueTrigger` with the board name matching the Arcweave board
4. **Unity - GameManager**: set `Dialogue Start/End Component Name` if using component-based detection
5. **Unity - portraits**: place images in `Assets/Resources/` with filenames matching Arcweave
6. **Unity - audio**: place audio files in `Assets/Resources/`; add `ArcweavePlayerAudio` to NPCs that have audio in their board
7. **Unity - variable binding**: use `ArcweaveObjectActivation` for object activation and `ArcweaveHealthUI` for health/slider
8. **Build**: `File > Build Settings > Build`. The `arcweave/` folder is created automatically

### Media Management

Images and audio files are searched in the same order:
1. Unity's `Resources` folder (baked into the build)
2. `[Build Folder]/arcweave/resources/` (for files not already bundled under the same name)

Bundled Resources take precedence over external files with the same name. External media can be added or replaced without rebuilding; restart the application after replacing cached audio. Image caches are cleared on import.

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

- **Variables when pressing Play**: each Editor Play session starts from the imported defaults without deleting saved progress. Manual web/local imports also use the imported defaults. Use **Load** to explicitly resume a save. Build startup automatically restores compatible saved values; removed variables are ignored and new or incompatible variables use their defaults.
- **JSON not loading**: verify the file is exported from Arcweave in Unity format
- **Images missing**: filenames must match those in the Arcweave project
- **API key / hash invalid**: check your Arcweave account settings and project URL
- **NPC not talking**: check that `Specific Board Name` matches the Arcweave board name exactly (case-sensitive)
- **Dialogue endings**: an element ends the conversation when it has a `dialogue_end` component/tag, or when no outgoing connection resolves to another element. Its content stays visible until **Close** is clicked; no extra end element is needed. Put final variable changes in that element's content script, because connections after an explicit end marker are not followed.
- **Object not activating**: keep `ArcweaveObjectActivation` on an always-active controller and assign its target. Use either a matching boolean variable name or an element ID in `Deactivate After Visiting Element Id`. The binding sets the target's active state; no particular initial active state is required.
- **Sword swing not unlocking**: `Component Name` in `SwordSwingHandler` must exactly match a component attached to an Arcweave element (the demo uses `attack`). Configure the player's Animator trigger separately.
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
└── Editor/         -- ArcweaveBuildProcessor, ArcweaveSaveReset
```

---

### Core Scripts

#### ArcweavePlayer

Central controller for Arcweave narratives. Manages project state, navigation between elements, variable state, and save/load. Fires events that UI and other scripts subscribe to.

**Key events:**
- `onProjectStart(Project)` / `onProjectFinish(Project)` -- project lifecycle
- `onProjectUpdated(Project)` -- fires after project is initialized or reloaded
- `onElementEnter(Element)` -- new dialogue node reached
- `onElementRestored(Element)` -- saved node presented again without firing entry effects
- `onElementOptions(Options, Action<int>)` -- player choices available
- `onWaitInputNext(Action)` -- single path forward, caller invokes to advance
- `onWaitInputFinish(Action)` -- final element is displayed, caller invokes to save and finish

The player runs each element's content script on entry, before notifying UI/audio listeners or evaluating outgoing paths. On Load, it evaluates the saved element to render its text, then restores the snapshot's variables and visits before notifying `onElementRestored`; audio and other entry effects are not replayed. The UI only renders the resulting content. Completion callbacks are valid only for the element that produced them, so repeated clicks and stale buttons cannot advance a finished conversation.

```csharp
arcweavePlayer.onElementEnter += (element) => {
    Debug.Log($"Entered: {element.Title}");
};
```

#### ArcweavePlayerUI

Subscribes to `ArcweavePlayer` events and renders dialogue text, images, choices, and variable debug list. Configure UI references in the Inspector.

| Inspector Field | Purpose |
|----------------|---------|
| `player` | ArcweavePlayer whose events drive the UI |
| `content`, `cover`, `componentCover` | Dialogue text and image references |
| `buttonTemplate` | Template used for choices, Continue and Close |
| `saveButton`, `loadButton` | Optional save/load controls |
| `variablesText` | Text for the variable debug list |
| `showVariables`, `showAllVariables` | Enable the list and include component/board scopes |
| `variableUpdateInterval` | Seconds between variable refreshes |
| `animateTextEntries`, `crossfadeTime` | Enable entry animation and set its duration |

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

Toggles a GameObject from the current Arcweave state. With a boolean binding, `true` = inactive and `false` = active. With an optional element ID, the object is active until that element has been visited. Load, reset and imports can change the state in either direction; there is no separate permanent-deactivation flag.

| Inspector Field | Purpose |
|----------------|---------|
| `targetObject` | GameObject to activate/deactivate |
| `activationVariableName` | Boolean variable name (e.g. `sword_locked`) |
| `deactivateAfterVisitingElementId` | Optional exported element ID; takes precedence over the variable and hides the target when its visit count is positive |

**Object Activation naming convention:** name variables for the locked/closed state (e.g. `sword_locked`). Default `true` = hidden. When flipped to `false`, the object activates.

The demo bottle uses the visit binding configured in the scene: it represents the Healer's remaining supply, so consuming the player's potion must not make it reappear. The script has no character names, inventory names or story IDs built into it. Configure another variable or element ID to reuse it. If a binding is missing from an imported project, the handler leaves the target unchanged; update the Inspector binding for that project.

#### ArcweaveSceneController

Reads a configured component on initialization, Load, import and dialogue completion (in `Handlers/`). The script defaults are `SceneSettings`, `Time` and `ParticleState`; the demo scene uses `Environment`, `time` and `weather`. Exposed attribute variables are read using component/attribute Custom IDs, with static attribute data as a fallback. Day/night controls the camera background; rain/clear controls the assigned particle systems.

| Inspector Field | Purpose |
|----------------|---------|
| `dayValue` | Value for daytime (default: `Day`) |
| `componentName` | Component containing the scene settings |
| `timeAttribute`, `particleAttribute` | Attribute names for time and weather |
| `nightValue` | Value for nighttime (default: `Night`) |
| `nightColor` | Background color for night (default: black) |

#### ArcweaveAudioManager

Optional singleton (`DontDestroyOnLoad`) that acts as a scene-wide event bus for audio stop signals. Required only when using the `Stop` playback mode on audio assets — it broadcasts a stop signal to all `ArcweavePlayerAudio` instances in the scene so a clip can be halted across multiple characters simultaneously.

Add it to any persistent GameObject (e.g. `GameManager`). If not present, `Once` and `Loop` modes work normally; only `Stop` mode has no effect.

#### ArcweavePlayerAudio

Add to any NPC that has audio assets assigned to its Arcweave elements. Subscribes to `ArcweavePlayer.onElementEnter` and plays the audio assets attached to each element, but only while the `DialogueTrigger` reports an active dialogue — preventing stray audio if events fire outside a conversation.

Multiple audio assets can be assigned to a single element in Arcweave; each is played independently with its own settings. Audio assets are configured entirely in Arcweave (upload via the **Assets** tab, assign to elements). No audio clips are referenced directly in Unity.

For each asset, the script first calls `TryGetAudioClip()` (resolves from Unity `Resources`). If that returns null, it falls back to `ArcweaveAudioLoader`, which loads the file asynchronously from `[Build]/arcweave/resources/` and caches it for the session. External files do not override a clip already bundled in Resources. Restart after replacing an external clip that was already loaded.

| Inspector Field | Purpose |
|----------------|---------|
| `player` | Reference to `ArcweavePlayer` (auto-found on the same GameObject if empty) |
| `dialogueTrigger` | Reference to `DialogueTrigger` (auto-found on the same GameObject if empty) |
| `debugMode` | Enable console logs for audio events |

**Playback modes (set in Arcweave):**

| Mode | Behavior |
|------|----------|
| `Once` | Plays the clip once, then stops |
| `Loop` | Loops across elements in the current conversation until Stop or dialogue completion |
| `Stop` | Stops the named clip on all `ArcweavePlayerAudio` instances (requires `ArcweaveAudioManager` in scene) |

Each audio asset also supports `volume` (0–1) and `delay` (seconds before playback starts), both set in Arcweave. Delayed clips use `AudioSource.PlayDelayed()`. Element changes cancel pending loads and non-looping sources; loops remain tracked for Stop. Closing the dialogue, loading/resetting/replacing the project, or disabling the handler stops all its sources and pending loads. Load presents the saved node without replaying its audio.

#### ArcweaveImageLoader

Singleton that loads images by filename from Resources, then `[Build]/arcweave/resources/`, then custom paths. Caches loaded images for performance.

#### ArcweaveAudioLoader

Singleton that loads audio files at runtime using the same pattern as `ArcweaveImageLoader`. Checks Unity's `Resources` folder first, then `[Build]/arcweave/resources/`. Loading from the build folder is async (`UnityWebRequest`); clips are cached by filename after the first load.

Used automatically by `ArcweavePlayerAudio` as a fallback when `TryGetAudioClip()` returns null — no manual setup needed. To support post-build audio updates, place audio files in `[Build]/arcweave/resources/` alongside images.

#### RuntimeArcweaveImporter

Async import from web API or local JSON at runtime. Provides `onImportSuccess` / `onImportFailed` events. The UI saves credentials between sessions. Failed requests or invalid JSON report failure and allow retrying while preserving the previous project.

#### ArcweaveImporterUI

UI panel for runtime import. Accessible via Esc key. Supports both web and local JSON import. Successful imports close an open panel after a real-time delay, including when game time is paused. A late response to an import does not restart UI coroutines on an already closed panel.

#### GameManager

Singleton state machine: `Gameplay`, `Dialogue`, `Paused`. Controls cursor lock, player/camera enable, and UI visibility.

| Inspector Field | Purpose |
|----------------|---------|
| `dialogueStartTag` | Attribute tag for dialogue start (default: `dialogue_start`) |
| `dialogueEndTag` | Attribute tag for dialogue end (default: `dialogue_end`) |
| `dialogueStartComponentName` | Component name for dialogue start (optional, overrides tag) |
| `dialogueEndComponentName` | Component name for dialogue end (optional, overrides tag) |
| `dialogueAnimatorParam` | Animator bool parameter for dialogue state (default: `IsInDialogue`) |

Detection priority: component name first, attribute tag fallback. An end tag's value must exactly match the configured tag. Standalone playback without a GameManager uses `dialogue_end` for both the component name and tag value. Explicit end markers take precedence over outgoing connections; unmarked elements also end when no valid outgoing path exists. Both cases show **Close** before returning to gameplay. Resuming the importer only changes a paused game; a delayed import completion cannot dismiss an active dialogue.

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

Editor-only post-build hook. Creates `arcweave/` and `arcweave/resources/` in the build output. Builds use the imported scene asset; the hook does not copy the repository's possibly older JSON. An optional `arcweave/project.json` placed beside the executable is imported at startup. Build into a new folder when testing bundled content: existing external JSON files are preserved.

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

**Concrete example:** `SwordSwingHandler` watches its configured component name and calls `PlayerController.EnableSwordSwing()`. On initialization, Load or import, it derives the unlock from visits to any element carrying that component. A fresh import or reset relocks it. No particular element ID or character is required.

| Inspector Field | Purpose |
|----------------|---------|
| `Component Name` | Must exactly match the Arcweave component (demo: `attack`) |
| `Player Controller` | Reference to the player's PlayerController |

For new persistent Unity behavior, store the decision in an Arcweave variable or element visit and subscribe to `onProjectUpdated` to apply that state after initialization, Load, reset and import. Use `onElementEnter` for one-time reactions during play, and `onElementRestored` for presentation only. Arbitrary Unity fields, transforms, animation playback positions and audio playback positions are not saved automatically. This keeps the save format independent of particular NPCs, quests and Unity components.

---

### Save/Load

Uses one `PlayerPrefs` slot, `arcweave_save_progress`, containing a versioned JSON snapshot of the current element, variables, element visit counts, and exported board IDs. Call `ArcweavePlayer.Save()` / `Load()`, or `TryLoad()` when the caller needs to know whether navigation succeeded.

- Each Editor Play session starts with imported default values. Initializing another NPC during the same session keeps current gameplay values. Existing saves are left intact; no manual reset is needed to test from the beginning.
- Build startup restores compatible global, board, and component variables and element visits, including earlier decisions tested with `visits()`. NPC interactions still start at their configured dialogue entry point.
- Explicit loading resumes the saved element. If that element was deleted, loading falls back to the project's starting element while keeping compatible variables.
- The demo saves automatically when a conversation is closed. Quitting before Close discards unsaved changes unless `Save()` was called explicitly. Player position and the currently interacting NPC are not part of the save.
- A shared board ID identifies another export of the same project, so text edits, renames, and adding or removing some boards preserve progress. An export with no shared board IDs starts with defaults. Copies retaining the same exported IDs share this identity.
- Manual web/local imports use the exported defaults and leave the saved slot intact. The automatic local import at build startup restores compatible progress. Saving a different project replaces the single slot.
- Legacy `arcweave_save_variables` and `arcweave_save_currentElement` data can be restored when an element or variable ID still belongs to the current project; migration happens on the next actual save. Unrelated saves, unreadable data, and unsupported versions are left untouched until an actual save or reset.
- Saves written before visit tracking can recover only their saved current element's visit. Earlier visits were never stored and cannot be reconstructed reliably from inventory variables. Use a fresh save to validate complete narrative history.
- When resuming with **Load** or starting a build, compatible saved values take precedence over newly edited defaults. Manual imports apply the new defaults immediately. **Tools > Arcweave > Reset Saved Progress** deliberately deletes both current and legacy progress; `ArcweavePlayer.ResetVariables()` also resets the running project.

---

## Compatibility

- **Unity 6** -- current save, dialogue, import and build checks use **6000.0.39f1**, including Windows 64-bit builds.
- **Unity 2022 LTS** (2022.3.x) -- previously listed as supported; the current changes have not been revalidated on this version.
- **Arcweave plugin**: included in the project. Standalone plugin: https://github.com/Arcweave/arcweave-unity-plugin
