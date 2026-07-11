# `GameEntity` (Class)

The `GameEntity` class is the base runtime representation of an entity instance. It coordinates coordinate transforms, collision boundaries, rendering assets, tags, and behavior attachments.

---

## API Definition & Properties

```csharp
namespace MonoGameMaker.Runtime
{
    public class GameEntity
    {
        public string PrefabName { get; set; }
        public Texture2D? Texture { get; set; }
        public Vector2 Position { get; set; }
        public IEntityScript? Script { get; set; }
        public string Tag { get; set; }
        public bool IsDestroyed { get; set; }
        
        public Rectangle Bounds { get; }
    }
}
```

### Property Descriptions

*   **`PrefabName`** (string): The identifier name matching the `.prefab` file layout definition this entity was cloned from.
*   **`Texture`** (Texture2D): The texture asset drawn in world space.
*   **`Position`** (Vector2): The world space coordinates of the entity. Modifying this changes its position in the game world.
*   **`Script`** (IEntityScript): The execution controller handling update/draw logic triggers.
*   **`Tag`** (string): Categorization identifier used to filter collision queries.
*   **`IsDestroyed`** (bool): Flag that cleans up the entity at the end of the physics frame.
*   **`Bounds`** (Rectangle): Bounding box used for AABB collision detection. Returns:
    `new Rectangle((int)Position.X, (int)Position.Y, Texture.Width, Texture.Height)`
    If `Texture` is null, returns a default size rectangle:
    `new Rectangle((int)Position.X, (int)Position.Y, 64, 64)`.

---

## Modifying Bounds & Movement Logic

To move an entity, update its `Position` property:
`entity.Position = new Vector2(entity.Position.X + delta, entity.Position.Y);`
Updating `Position` automatically adjusts the calculated `Bounds` coordinate origins for collision queries.

### Overriding Bounding Box
Because `Bounds` is a read-only calculated property based on the active `Texture` dimension, if a custom collision box size is needed (e.g. smaller box for player hitboxes), you can set a smaller/larger texture asset or implement custom collision offset adjustments directly inside the script collision logic:

```csharp
// Inside custom script Update:
// Instead of checking generic caller.Bounds, construct a custom boundary rectangle:
Rectangle customHitbox = new Rectangle(
    (int)_entity.Position.X + 8, // X offset
    (int)_entity.Position.Y + 8, // Y offset
    _entity.Texture.Width - 16,  // Custom width
    _entity.Texture.Height - 16  // Custom height
);
```

---

## AI Prompting & Context Guidelines

When directing an AI coding assistant to create or modify entity instances:

*   **Prompt Scenario**: *"Write a movement update logic block that adjusts player Position based on axis inputs, clamping coordinates within the boundary limits."*
*   **Prompt Scenario**: *"Create a script that dynamically updates the entity Tag property to 'Hazardous' when health drops below 50."*
*   **Key Guardrail**: Avoid changing entity variables outside script tick boundaries. Direct coordinate changes should occur inside the script's `Update` lifecycle method.
