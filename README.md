# Arcweave Unity Example

This project demonstrates Arcweave integration with Unity, allowing you to import Arcweave projects both during development and at runtime.

![Arcweave Unity Example image](https://github.com/user-attachments/assets/f23a79a7-03c8-4d02-a899-bff6a2271c98)


## Features

- Import Arcweave projects from web (using API key and project hash)
- Import Arcweave projects from local JSON file
- Support for preloaded projects included in the build
- Support for Arcweave images from different sources (Resources, build folder)
- Simple user interface for importing
- Arcweave variable and event management
- Scene control based on dialogue flow
- Character movement and camera control
- Particle system effects controlled by Arcweave variables

### Initial Setup

1. Clone this repository
2. Open the project in Unity
3. Make sure the Asset folder is correctly imported
4. Arcweave Unity plugin is already imported in the project so don't worry. 
   To know more about the plugin visit: https://github.com/Arcweave/arcweave-unity-plugin

## Including an Arcweave project in your unity build

### Preparation Steps

To include your custom Arcweave project (JSON and images) in your Unity build:

1. Export your Arcweave project in Unity format
2. Place the JSON file in `Assets/Arcweave/project.json`
3. Place all related images in `Assets/Resources`
4. Alternatively, you can use the web importer (explained below)

### Import Process

To import your Arcweave data into Unity:

1. **Use or modify an existing ArcweaveProjectAsset**:
    - Edit the existing ArcweaveProjectAsset in the `Assets/Arcweave` folder
    - **OR** create a new one if needed
2. If creating a new ArcweaveProjectAsset:
    - Right-click in the Unity Assets tab
    - Select **Create > Arcweave > Project Asset**
    - Name the new `.asset` file as you prefer
3. Configure the import settings:
    - Open the inspector for your ArcweaveProjectAsset
    - Choose your preferred import method:
        - **From JSON**: Assign the `json.txt` file you exported
        - **From Web**: Enter your **user API key** and **project hash**
    - Click **Generate Project** to start the import process

### Verification and Viewing

After successful import:

- The inspector will display your imported project name and global variables (useful for runtime debugging)
- Click **"Open Project Viewer"** to access a visual editor showing all elements of your project (boards, elements, connections, etc.)

**Note**: Using ArcweaveProjectAsset files allows you to import multiple Arcweave projects into a single Unity project.

*Remember to connect your Arcweave Project Asset to the ArcweaveImporter and ArcweavePlayer Game Objects*

### How to Use This Template

1. **Set Up Your Arcweave Project**:
    - Create a new Game Engine Example Project
    - Add components, elements and variables as needed
    - Use the "tag" attribute "dialogue_start" on elements where you want the dialogue to start
2. **Customize the UI**:
    - Modify ArcweavePlayerUI prefab to match your game's visual style
    - Update dialogue box, buttons, and option list to fit your needs
3. **Connect to Your Game**:
    - Use ArcweaveVariableEvents to make your game react to Arcweave's logic
    - Create trigger zones with ArcweaveDialogueTrigger to start narrative parts
    - Modify the PlayerController and ThirdPersonCamera to fit your game's movement style
4. **Test and Iterate**:
    - Use the runtime importer to quickly test changes to your Arcweave project
    - Use debug logs to track variable changes and dialogue flow
    - Adjust triggers and scene controllers as needed
5. **Build:**
    - During the build process:
        1. An `arcweave` folder is created in the build directory
        2. Files in the Asset/Resources folder are compressed in Unity format
        3. At startup, the application will automatically load the preloaded project
        4. Users can import new files by placing them in the `arcweave` folder

### Image Management

Images are searched for in this order:

1. Unity's `Resources` folder (original behavior)
2. `[Game Folder]/arcweave/images/` (for user-added images)

## For End Users

### Importing an Arcweave Project from Web

1. Launch the application
2. Press Esc key to open the Menu
3. Enter your API key and project hash in the appropriate fields
4. Click the "Import Web" button
5. Wait for the import to complete

### Importing an Arcweave Project from Local File

1. Launch the application
2. Place your JSON file in the `arcweave` folder next to the application executable
    - On Windows: `[Game Folder]/arcweave/project.json`
3. If your project includes images, place them in `[Game Folder]/arcweave/images/`
4. Press Esc key to open the Menu
5. Click the "Import Local" button
6. Wait for the import to complete

### Troubleshooting

If you encounter issues during import:

- Make sure the JSON file is exported form the Unity Export
- Verify that the file path is correct
- Check that the API key and project hash are valid (for web import)
- For image issues, verify they are in the correct folder and that filenames match those in the JSON
- Restart the application and try again

## For Developers

## Core Scripts Overview

### ArcweaveAttributeHandler

Handles Arcweave component attributes, allowing them to affect game objects and components.

**Key Features**:

- Automatically finds Arcweave components and attributes by name
- Subscribes to project events to update when Arcweave data changes
- Provides a simple way to respond to attribute changes

**Design Approach**:
We designed this as an abstract class so you can easily create custom handlers for different types of attributes. You only need to implement the `ApplyAttributeValue` method in your derived class.

**Usage Example**:

```csharp
// Create a custom attribute handler
public class MyCustomHandler : ArcweaveAttributeHandler
{
    protected override void ApplyAttributeValue(string value)
    {
        // Apply the attribute value to your gameplay systems
        Debug.Log($"Attribute value: {value}");
    }
}
```

### ArcweaveDialogueTrigger

Triggers dialogue interactions when the player enters trigger zones. Can be configured to start specific elements or boards from the Arcweave project.

**Key Features**:

- Proximity-based interaction with visual indicators
- Keyboard input to start conversations
- Automatically finds and displays dialogue from Arcweave boards

**Design Approach**:
This component creates a seamless way to start conversations in your game world. The interaction is designed to feel natural, with visual prompts appearing when the player is close enough to interact.

**Usage Example**:

1. Attach to any NPC or interactive object
2. Set the interaction distance and key
3. Set the Arcweave board containing the dialogue
4. The component handles everything else automatically

### ArcweaveImageLoader

Handles loading images from multiple sources (Resources, custom, or build folder). Implements caching for better performance.

**Key Features**:

- Loads images from Resources folder, build folder, or custom locations
- Caches images to improve performance
- Supports runtime image loading via file paths
- Fallback system ensures images load from somewhere

**Design Approach**:
We created a flexible system that doesn't just rely on Resources, allowing users to add new images after building their game. The caching system greatly improves performance for repeated image usage.

**Usage Example**:

```csharp
// Get the singleton instance
ArcweaveImageLoader.Instance.LoadImage("myimage.png");

// Add custom search paths
ArcweaveImageLoader.Instance.AddSearchPath(Application.persistentDataPath + "/CustomImages");
```

### ArcweavePlayer

Main controller for playing through Arcweave narratives. Handles project initialization, navigation between elements, managing dialogue options, and saving/loading project state. Provides events that UI can subscribe to.

**Key Features**:

- Manages project state and navigation between elements
- Handles variable state and saving/loading
- Provides events for UI elements to respond to changes
- Supports starting from specific elements

**Design Approach**:
This component serves as the central hub for Arcweave functionality. It's designed to be extended but works out-of-the-box, with events that make it easy to hook into the narrative flow.

**Usage Example**:

```csharp
// Subscribe to events
arcweavePlayer.onElementEnter += (element) => {
    Debug.Log($"Entered element: {element.Title}");
};

// Navigate to a specific element
arcweavePlayer.Next(someElement);

// Save/load state
arcweavePlayer.Save();
arcweavePlayer.Load();
```

### ArcweavePlayerUI

Displays the Arcweave content to the player, including text, images, and options. Subscribes to events from ArcweavePlayer to update the UI accordingly.

**Key Features**:

- Shows element content, images, and options
- Handles button creation for dialogue choices
- Supports text animations and visual effects
- Displays variables for debugging

**Design Approach**:
This component connects the Arcweave system to Unity's UI system. It's designed to be flexible yet work well with minimal setup. The component handles all the complexity of displaying dynamic content.

**Usage Example**:

1. Assign references to UI elements in the inspector
2. Link to your ArcweavePlayer component
3. The UI will automatically display content as the player navigates the project

### ArcweaveSceneController

Controls scene-specific elements based on Arcweave variables and components. Manages visual elements, lighting, and environmental effects based on narrative progression.

**Key Features**:

- Updates camera background for day/night cycles
- Controls particle systems for weather effects
- Responds to game state changes
- Updates when Arcweave data changes

**Design Approach**:
We created this to show how narrative data can directly influence your game world. Instead of writing complex scripts to interpret narrative choices, you can use Arcweave attributes as a design-friendly way to control your scene.

**Usage Example**:

1. Create Arcweave component with "Time" and "ParticleState" attributes
2. Assign camera and particle systems in the inspector
3. As the attributes change in Arcweave, your scene automatically updates

### ArcweaveSliderColorHandler

Changes UI slider colors based on Arcweave variables, creating dynamic UI that responds to narrative choices.

**Key Features**:

- Parses color values from Arcweave
- Updates slider fill colors in real-time
- Simple demonstration of extending ArcweaveAttributeHandler

**Design Approach**:
This is a small, focused example showing how to create concrete handlers for specific UI elements. It demonstrates the power of the attribute handler pattern with minimal code.

**Usage Example**:

1. Attach to a GameObject with a UI Slider
2. Set the component and attribute names in Arcweave
3. Colors will update automatically when the attribute changes

### ArcweaveVariableEvents

Links Arcweave variables to Unity events. Allows gameplay elements to react to changes in narrative variables.

**Key Features**:

- Updates health bars from Arcweave variables
- Controls GameObject activation based on variables
- Updates animator parameters from health values
- Changes UI colors from Arcweave attributes

**Design Approach**:
This component demonstrates real gameplay integration with Arcweave. It shows how narrative variables can directly affect gameplay systems and UI, creating a tight integration between narrative and mechanics.

**Usage Example**:

1. Configure which Arcweave variables to monitor
2. Link to Unity objects and UI elements
3. As the narrative changes variables, your gameplay elements automatically respond

### GameManager

Manages overall game state (Gameplay, Dialogue, Paused) and transitions between states. Controls player and camera during different states and handles UI activation/deactivation. Implements a singleton pattern for global access.

**Key Features**:

- Manages game states (Gameplay, Dialogue, Paused)
- Controls UI visibility and cursor state
- Handles dialogue start/end detection
- Shows temporary messages to the player

**Design Approach**:
The GameManager serves as the core connector between gameplay and narrative. It uses a state machine pattern for clarity and maintainability, making it easy to understand how the game transitions between states.

**Usage Example**:

```csharp
// Change game state
GameManager.Instance.SetGameState(GameManager.GameState.Dialogue);

// Show a message to the player
GameManager.Instance.ShowMessage("Item collected!");

// Subscribe to state changes
GameManager.Instance.OnGameStateChanged += (newState) => {
    Debug.Log($"Game state changed to: {newState}");
};
```

### ParticleSystemController

Controls particle effects based on Arcweave variables, creating dynamic environmental effects tied to narrative.

**Key Features**:

- Turns particle systems on/off based on Arcweave data
- Automatically updates when projects are imported or finished
- Simple weather state management

**Design Approach**:
This focused controller shows how to create environmental effects driven by narrative. It's designed to be simple but effective, demonstrating the direct link between narrative choices and visual effects.

**Usage Example**:

1. Create an Arcweave component with a "WeatherState" attribute
2. Assign your rain particle system
3. Set the attribute to "rain" or "clear" in Arcweave
4. Weather in your game will automatically update

### RuntimeArcweaveImporter

Core functionality for importing Arcweave projects during runtime. Supports importing from web (using API key and hash) or from local JSON files. Automatically loads saved project.

**Key Features**:

- Supports web API and local JSON imports
- Handles authentication and request management
- Provides events for success/failure handling
- Works in builds as well as the Unity editor

**Design Approach**:
This component allows you to update narrative content without rebuilding your game. It's designed to be robust with error handling and events that make it easy to integrate with UI systems.

**Usage Example**:

```csharp
// Import from web API
importer.SetApiKey("your-api-key");
importer.SetProjectHash("your-project-hash");
importer.ImportFromWeb();

// Import from local file
importer.SetLocalJsonFilePath("path/to/project.json");
importer.ImportFromLocalFile();
```

### ArcweaveImporterUI

Handles the user interface for importing Arcweave projects at runtime. Manages API key and project hash inputs, loading indicators, and status messages. 

**Key Features**:

- Support for both web and local JSON imports
- Saves credentials between sessions
- Shows loading indicators and status messages (not implemented in demo project)
- Auto-closes UI after successful imports

**Design Approach**:
This UI makes it simple for players or developers to update narrative content without rebuilding the game. It remembers settings between sessions for convenience.

**Usage Example**:

1. Add to a pause menu or settings screen
2. Configure auto-close behavior as needed
3. Players can now update narrative content from within the game

### ArcweaveBuildProcessor

Creates folders in the build directory for user-added content.

**Key Features**:

- Automatically runs after building your game
- Creates arcweave/images folders for content
- Optionally copies JSON project file

**Design Approach**:
This editor script ensures your built game is ready to accept new content. It's designed to work automatically, creating the folder structure needed for runtime importing without any manual steps.

**Usage Example**:
No manual usage is required - it runs automatically after you build your game.

### PlayerController

**Purpose**: Controls the player character's movement and interactions within the game world.

**Key Features**:

- Handles basic character movement (walk, run, jump)
- Integrates with the dialogue system to enable/disable controls during conversations
- Manages player animations and state transitions
- Provides smooth movement with configurable speed settings

**Design Approach**:
PlayerController works seamlessly with the Arcweave dialogue system. When dialogues begin, the controller automatically disables player input, and when dialogues end, it restores control. This creates a smooth transition between gameplay and narrative sequences.

**Usage Example**:

1. Attach to your player character
2. Configure movement speeds and input settings
3. Link with GameManager to handle game state changes
4. The controller will automatically handle input enabling/disabling during dialogues

### ThirdPersonCamera

Provides a third-person camera that follows the player with smooth movement.

**Key Features**:

- Follows the player with adjustable distance and height
- Smooth rotation and position interpolation
- Collision detection to prevent camera clipping through walls
- Support for dialogue camera positions during conversations

**Design Approach**:
This camera system is designed to provide an immersive view during gameplay while also supporting dialogue scenarios. The camera can smoothly transition to different positions when dialogues begin, focusing on the conversation. The integration with the GameManager ensures the camera behaves appropriately in different game states.

**Usage Example**:

1. Attach to a camera GameObject in your scene
2. Assign your player character as the target
3. Configure distance, height, and smoothing parameters
4. The camera will automatically adjust based on gameplay or dialogue contexts

