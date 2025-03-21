# Arcweave Unity Demo

This project demonstrates Arcweave integration with Unity, allowing you to import Arcweave projects both during development and at runtime.

## Features

- Import Arcweave projects from web (using API key and project hash)
- Import Arcweave projects from local JSON file
- Support for preloaded projects included in the build
- Support for Arcweave images from different sources (Resources, StreamingAssets, build folder)
- Simple user interface for importing
- Arcweave variable and event management
- Scene control based on dialogue flow
- Character movement and camera control
- Particle system effects controlled by Arcweave variables

## For Developers

### Initial Setup

1. Clone this repository
2. Open the project in Unity
3. Make sure the Arcweave asset is correctly imported

### Including a Preloaded Project in the Build

To include a preloaded project (JSON and images) in the build:

1. Place your JSON file in `Assets/Arcweave/project.json`
2. Place your images in `Assets/Arcweave/images/`
3. Go to `Arcweave > Copy Project to StreamingAssets` in the Unity menu
4. Files will be copied to `Assets/StreamingAssets/arcweave/`
5. When building, these files will be automatically included

You can also copy only the JSON or only the images using the separate commands:
- `Arcweave > Copy JSON to StreamingAssets`
- `Arcweave > Copy Images to StreamingAssets`

### Build Process

During the build process:

1. Files in StreamingAssets are automatically included in the build
2. An `arcweave` folder is created in the build directory
3. At startup, the application will automatically load the preloaded project
4. Users can import new files by placing them in the `arcweave` folder

### Image Management

Images are searched for in this order:
1. Unity's `Resources` folder (original behavior)
2. `StreamingAssets/arcweave/images/` (for preloaded images)
3. `[Game Folder]/arcweave/images/` (for user-added images)

To manually load an image from any source, you can use the ArcweaveImageLoader system:

```csharp
// Get an instance of the loader
Arcweave.ArcweaveImageLoader imageLoader = Arcweave.ArcweaveImageLoader.Instance;

// Load an image from the specified path
Texture2D texture = imageLoader.LoadImage("path/to/image.png");
```

## Core Scripts Overview

### ArcweaveImporterUI.cs
Handles the user interface for importing Arcweave projects at runtime. Manages API key and project hash inputs, loading indicators, and status messages. Saves credentials in PlayerPrefs for convenience.

### RuntimeArcweaveImporter.cs
Core functionality for importing Arcweave projects during runtime. Supports importing from web (using API key and hash) or from local JSON files. Automatically loads prepackaged projects from StreamingAssets if available.

### ArcweavePlayer.cs
Main controller for playing through Arcweave narratives. Handles project initialization, navigation between elements, managing dialogue options, and saving/loading project state. Provides events that UI can subscribe to.

### ArcweavePlayerUI.cs
Displays the Arcweave content to the player, including text, images, and options. Subscribes to events from ArcweavePlayer to update the UI accordingly.

### GameManager.cs
Manages overall game state (Gameplay, Dialogue, Paused) and transitions between states. Controls player and camera during different states and handles UI activation/deactivation. Implements a singleton pattern for global access.

### ArcweaveDialogueTrigger.cs
Triggers dialogue interactions when the player enters trigger zones. Can be configured to start specific elements or boards from the Arcweave project.

### ArcweaveSceneController.cs
Controls scene-specific elements based on Arcweave variables and components. Manages visual elements, lighting, and environmental effects based on narrative progression.

### ArcweaveImageLoader.cs
Handles loading images from multiple sources (Resources, StreamingAssets, build folder). Implements caching for better performance.

### ArcweaveVariableEvents.cs
Links Arcweave variables to Unity events. Allows gameplay elements to react to changes in narrative variables.

### ArcweaveAttributeHandler.cs
Handles Arcweave component attributes, allowing them to affect game objects and components.

### ArcweaveSliderColorHandler.cs
Changes UI slider colors based on Arcweave variables, creating dynamic UI that responds to narrative choices.

### PlayerController.cs
Simple character controller that handles player movement and rotation. Disabled during dialogue.

### ThirdPersonCamera.cs
Camera controller that follows the player. Can be configured for different viewing angles and distances.

### ParticleSystemController.cs
Controls particle effects based on Arcweave variables, creating dynamic environmental effects tied to narrative.

## For End Users

### Importing an Arcweave Project from Web

1. Launch the application
2. Enter your API key and project hash in the appropriate fields
3. Click the "Import Web" button
4. Wait for the import to complete

### Importing an Arcweave Project from Local File

1. Launch the application
2. Place your JSON file in the `arcweave` folder next to the application executable
   - On Windows: `[Game Folder]/arcweave/project.json`
3. If your project includes images, place them in `[Game Folder]/arcweave/images/`
4. Click the "Import Local" button
5. Wait for the import to complete

### Troubleshooting

If you encounter issues during import:

- Make sure the JSON file is correctly formatted
- Verify that the file path is correct
- Check that the API key and project hash are valid (for web import)
- For image issues, verify they are in the correct folder and that filenames match those in the JSON
- Restart the application and try again

## How to Use This Template

1. **Set Up Your Arcweave Project**:
   - Create your story in Arcweave
   - Add components and variables as needed
   - Use the attribute "starting_dialogue_elements" on elements where you want the dialogue to start

2. **Customize the UI**:
   - Modify ArcweavePlayerUI prefab to match your game's visual style
   - Update dialogue box, buttons, and option list to fit your needs

3. **Connect to Your Game**:
   - Use ArcweaveVariableEvents to make your game react to narrative choices
   - Create trigger zones with ArcweaveDialogueTrigger to start conversations
   - Modify the PlayerController and ThirdPersonCamera to fit your game's movement style

4. **Test and Iterate**:
   - Use the runtime importer to quickly test changes to your Arcweave project
   - Use debug logs to track variable changes and dialogue flow
   - Adjust triggers and scene controllers as needed

## License

This project is released under the MIT License. See the LICENSE file for more details. 