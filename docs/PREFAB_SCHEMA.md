# Prefab & Scene Serialization Schema

This document outlines the JSON schema format used by Mono GameMaker to serialize prefabs and scene configuration files.

---

## 1. Prefab Schema (`.prefab`)

Prefabs are stored as JSON files inside the `Prefabs/` directory. They define the base properties, assets, and behavior scripts shared by all instances of this object.

### JSON Structure

```json
{
  "TextureName": "player_sprite",
  "ScriptName": "MyGame.Scripts.PlayerController",
  "Tag": "Player",
  "CustomProperties": {
    "Speed": "350",
    "JumpForce": "700"
  }
}
```

### Property Descriptions

*   **`TextureName`** (string): The asset name of the sprite texture file located in `Content/Textures/` (without path prefix or extension, e.g., `"player_sprite"` for `Content/Textures/player_sprite.png`).
*   **`ScriptName`** (string): The fully-qualified namespace class name of the C# script behavior that implements `IEntityScript` to run on this entity (e.g. `"MyGame.Scripts.PlayerController"`). Leave empty if the object has no behavior.
*   **`Tag`** (string): Group/classification identifier used primarily for query lookups and collision detection filtering (e.g. `"Player"`, `"Enemy"`, `"Solid"`). Defaults to `"Default"`.
*   **`CustomProperties`** (key-value dictionary of string-to-string): Default parameter parameters passed into the script's `Initialize` method upon spawning.

---

## 2. Scene Configuration Schema (`.json`)

Scenes are stored inside the `Content/Scenes/` folder (with `scene_init.json` acting as the default starting scene).

### JSON Structure

```json
{
  "Width": 1920,
  "Height": 1080,
  "BackgroundColor": {
    "X": 0.1,
    "Y": 0.1,
    "Z": 0.2
  },
  "BackgroundImage": "background_wood",
  "Instances": [
    {
      "prefabName": "player",
      "x": 200,
      "y": 150,
      "CustomProperties": {
        "Speed": "400"
      }
    },
    {
      "prefabName": "ground",
      "x": 0,
      "y": 600,
      "CustomProperties": {}
    }
  ]
}
```

### Property Descriptions

*   **`Width`** (int): Width of the scene boundaries in pixels.
*   **`Height`** (int): Height of the scene boundaries in pixels.
*   **`BackgroundColor`** (Vector3): Clear color vector mapping R, G, and B parameters respectively as floating values between `0.0` and `1.0`.
*   **`BackgroundImage`** (string): Optional background sprite asset name located in `Content/Textures/`.
*   **`Instances`** (array): Array containing list of physical entity instances present in the scene layout at load time.
    *   **`prefabName`** (string): Matches the filename of the target prefab inside the `Prefabs/` folder (without extension).
    *   **`x`** (float): The starting coordinate position on the X axis.
    *   **`y`** (float): The starting coordinate position on the Y axis.
    *   **`CustomProperties`** (dictionary): Instance-specific overrides for custom properties. Merged with the prefab's default properties at load time (instance properties override prefab defaults).

---

## 3. Division of Responsibility: IDE ECS vs. C# Scripts

Mono GameMaker coordinates entity data between the visual authoring environment and the active script compiler.

| Property / Feature | Managed by IDE Editor | Managed by C# Script Behavior |
| :--- | :--- | :--- |
| **Asset Binding** | Set via inspector, saved to `.prefab`. | Dynamically loaded via `ContentManager` in runtime. |
| **Grid Placement** | Visually positioned, saved to scene instances. | Controlled dynamically by modifying `entity.Position` in script updates. |
| **Collisions** | Static tags defined in editor. | Checked dynamically using `EntityManager.GetFirstColliding(caller, targetTag)`. |
| **Entity Spawn/Destruction** | Added/Removed in editor layout lists. | Handled via `EntityManager.Spawn(...)` and `EntityManager.Destroy(...)`. |
| **Camera Focus** | Static editing view offset. | Dynamically target-tracked and clamped via `Camera2D.LookAt()`. |
