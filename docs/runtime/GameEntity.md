# `GameEntity` (Class)

The `GameEntity` class represents a physical entity instance present in the active scene. It coordinates coordinate transforms, collision boundaries, rendering assets, tags, behavior attachments, and sprite sheet animation states.

---

## API Definition & Properties

```csharp
namespace MonoGameMaker.Runtime
{
    public class GameEntity
    {
        public Guid Id { get; set; }
        public string PrefabName { get; set; }
        public Texture2D? Texture { get; set; }
        public Vector2 Position { get; set; }
        public EntityBehavior? Script { get; set; }
        public string Tag { get; set; }
        public bool IsDestroyed { get; set; }
        public Rectangle? SourceRect { get; set; }
        
        public Rectangle Bounds { get; }

        public GameEntity();
        public GameEntity(Guid id);
        public void LoadState(Guid id);
        public void PlayAnimation(int frameWidth, int frameHeight, int startFrame, int endFrame, float fps);
        public void UpdateAnimation(GameTime gameTime);
    }
}
```

### Property Descriptions

*   **`Id`** (Guid): The unique identifier of this entity. Deterministically preserved across saving, loading, and hot reloading.
*   **`PrefabName`** (string): The identifier name matching the `.prefab` file layout definition this entity was cloned from.
*   **`Texture`** (Texture2D): The texture asset drawn in world space.
*   **`Position`** (Vector2): The world space coordinates of the entity. Modifying this changes its position in the game world.
*   **`Script`** (EntityBehavior): The execution behavior controller handling logical events.
*   **`Tag`** (string): Categorization identifier used to filter collision queries.
*   **`IsDestroyed`** (bool): Flag that cleans up the entity at the end of the physics frame.
*   **`SourceRect`** (Rectangle?): Bounding viewport rectangle used to clip specific sub-regions of the spritesheet. Automatically updated by `PlayAnimation` or set to null to draw the entire texture.
*   **`Bounds`** (Rectangle): Bounding box used for AABB collision detection. Returns:
    - If `SourceRect` is active: `new Rectangle((int)Position.X, (int)Position.Y, SourceRect.Value.Width, SourceRect.Value.Height)`
    - If `SourceRect` is null and `Texture` is not null: `new Rectangle((int)Position.X, (int)Position.Y, Texture.Width, Texture.Height)`
    - Fallback: `new Rectangle((int)Position.X, (int)Position.Y, 64, 64)`.

---

## Methods

### `GameEntity` (Constructors)
- **Signature**: `GameEntity()` / `GameEntity(Guid id)`
- **Explanation**: The parameterless constructor initializes the entity with a fresh random `Guid.NewGuid()`. The parameterized constructor allows injecting a specific GUID (typically loaded from a scene or save file) to maintain referential integrity.
- **Example**:
  ```csharp
  var randomEntity = new GameEntity(); // Generates new Guid
  var loadedEntity = new GameEntity(new Guid("d3b07384-d113-4f56-b7cd-1b759df0ee0a")); // Uses fixed Guid
  ```

### `LoadState`
- **Signature**: `void LoadState(Guid id)`
- **Explanation**: Injects or updates the entity's Guid. Used to hydrate existing entities with stored identifiers during deserialization.
- **Example**:
  ```csharp
  entity.LoadState(diskGuid);
  ```

### `PlayAnimation`
- **Signature**: `void PlayAnimation(int frameWidth, int frameHeight, int startFrame, int endFrame, float fps)`
- **Explanation**: Initiates a spritesheet animation sequence. If the specified parameters match the currently playing animation, the request is ignored to avoid resetting the frame index timer.
- **Example**:
  ```csharp
  Entity.PlayAnimation(32, 32, 0, 3, 10f); // Play 4-frame cycle at 10 FPS
  ```

### `UpdateAnimation`
- **Signature**: `void UpdateAnimation(GameTime gameTime)`
- **Explanation**: Automatically called by `EntityManager.Update` every frame to advance the animation timer and update `SourceRect`.
- **Example**: Automatically handled by the engine.

---

## Modifying Bounds & Movement Logic

To move an entity, update its `Position` property:
`entity.Position = new Vector2(entity.Position.X + delta, entity.Position.Y);`
Updating `Position` automatically adjusts the calculated `Bounds` coordinate origins for collision queries.

Because `Bounds` calculations take `SourceRect` dimensions into account, starting a spritesheet animation (e.g. `PlayAnimation(32, 32, 0, 3, 10)`) automatically reduces the entity's collision hitbox to the size of a single frame (32x32) rather than checking the entire sheet size.

---

## AI Prompting & Context Guidelines

When directing an AI coding assistant to create or modify entity instances:

*   **Prompt Scenario**: *"Adjust the entity position and query its Bounds width or height to apply boundaries checking inside the script Update method."*
*   **Prompt Scenario**: *"Invoke PlayAnimation on the entity to trigger an animation sequence when moving left."*
*   **Key Guardrail**: Avoid changing entity variables outside script tick boundaries. Direct coordinate changes should occur inside the script's `Update` lifecycle method.
