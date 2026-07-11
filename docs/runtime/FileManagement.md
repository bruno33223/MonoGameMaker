# ProjectExplorer File Management Hub Manual

This manual details the operational file management capabilities, context menu creation rules, file deletion workflows, and background cache synchronization of the Mono GameMaker IDE.

---

## 1. High-Level Description
The `ProjectExplorer` unifies resource authoring by serving as a central File Management Hub. Instead of using separate creation controls scattered throughout properties tabs, developers can right-click nodes inside the Project Explorer to procedurally instantiate folders, behaviors, prefabs, and scene files.

The system ensures security and data integrity by linking file deletion directly with cache invalidation and MGCB asset pipeline unregistration.

---

## 2. Supported Extensions & Context Operations

| Extension / Category | Default Folder Target | Context Command | Backend Action |
| :--- | :--- | :--- | :--- |
| **`.cs`** (Behavior Script) | `Scripts/` (or subfolders) | *Create EntityBehavior Script* | Generates a compileable `EntityBehavior` template using `TemplateEngine`. |
| **`.prefab`** (Object Prefab) | `Prefabs/` (or subfolders) | *Create New Object Prefab* | Serializes a blank prefab data structure to JSON using `PrefabSerializer`. |
| **`.json`** (Scene Layout) | `Content/Scenes/` | *Create New Scene Layout* | Scaffolds a standard canvas descriptor with dimensions and clear color. |
| **Folders** (Directories) | *Any folder node* | *New Folder* | Creates a physical subdirectory in workspace using `Directory.CreateDirectory`. |
| **Assets** (Textures/Audio) | `Content/` subfolders | *Delete File* | Calls `AssetPipelineSynchronizer.UnregisterAsset` to remove physical and MGCB entries. |

---

## 3. Use Cases (Context for AI Assistants)
- **Adding new scripts procedurally**: When requested to create a new controller behavior script, the AI assistant should guide the user to right-click the `Scripts` directory and trigger "Create EntityBehavior Script".
- **Deleting obsolete assets safely**: When removing redundant sprites or textures, the AI assistant should suggest right-clicking the target asset and choosing "Delete File", which cleans up both the physical file and its MGCB compilation registration.

---

## 4. Practical C# Compileable Example (Script Scaffolding Template)

When a script is created using *Create EntityBehavior Script*, the system generates the following code block as the default template:

```csharp
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameMaker.Runtime;

namespace MyGame.Scripts
{
    /// <summary>
    /// Procedurally generated script behavior template.
    /// </summary>
    public class CustomController : EntityBehavior
    {
        public override void Awake()
        {
            // Initial initialization logic (e.g. read Properties parameters)
            GlobalState.Log("CustomController initialized!");
        }

        public override void Update(GameTime gameTime)
        {
            // Update logic executed once per frame
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // Custom drawing logic
        }

        public override void DrawUI(SpriteBatch spriteBatch)
        {
            // Custom UI drawing overlay
        }
    }
}
```
