# Collision Masks (Custom Hitboxes)

Mono GameMaker supports custom Collision Masks (hitboxes) configured via the IDE and computed dynamically by the engine. This allows decoupling an entity's physical collision boundaries from its visual sprite dimensions (useful for character hitboxes, projectiles, and solid tiles).

---

## Bounding Box Calculation Formula

The `GameEntity.Bounds` property computes the physical bounding rectangle (`Rectangle`) dynamically in World Space:

*   **X coordinate**: `Position.X + HitboxOffset.X`
*   **Y coordinate**: `Position.Y + HitboxOffset.Y`
*   **Width**: If `HitboxSize.X > 0` uses `HitboxSize.X`, else falls back to the animation clip width (`SourceRect.Width`) or sprite texture width.
*   **Height**: If `HitboxSize.Y > 0` uses `HitboxSize.Y`, else falls back to the animation clip height (`SourceRect.Height`) or sprite texture height.

```csharp
public Rectangle Bounds
{
    get
    {
        int w = HitboxSize.X > 0f ? (int)HitboxSize.X : (SourceRect.HasValue ? SourceRect.Value.Width : (Texture != null ? Texture.Width : 64));
        int h = HitboxSize.Y > 0f ? (int)HitboxSize.Y : (SourceRect.HasValue ? SourceRect.Value.Height : (Texture != null ? Texture.Height : 64));
        return new Rectangle((int)(Position.X + HitboxOffset.X), (int)(Position.Y + HitboxOffset.Y), w, h);
    }
}
```

---

## Graceful Fallback Behavior

If no custom collision mask is configured for a prefab (i.e. `HitboxWidth` and `HitboxHeight` are set to `0`), the collision system falls back to visual dimensions:
*   Uses the active animation sheet frame width/height (if `PlayAnimation` is active).
*   Uses the whole sprite texture width/height (if no animation is active).
*   Uses a default fallback size of `64x64` pixels (if the entity has no sprite texture).

This ensures full backward compatibility with all existing scenes and prefabs.

---

## Editor Configuration & Visual Debugging

1.  **Prefab Inspector**:
    Select a prefab in the project explorer. Below the Texture selection, use the DragFloat widgets to adjust:
    *   **Offset X / Y**: Shift the hitbox origin relative to the entity position.
    *   **Width / Height**: Change the size of the collision box.
2.  **Visual Debug Highlight**:
    The scene viewport draws a semi-transparent green outline (`Color.Green * 0.5f`) matching the exact coordinates of the active hitbox bounds on top of all entities. This allows immediate visual verification of collision zones during editing and viewport play-mode simulation.

---

## AI Prompting & Context Guidelines

When directing an AI coding assistant to handle character movement or custom collision zones:

*   **Prompt Scenario**: *"Adjust the prefab collision hitbox to a tight 16x32 box centered on the player sprite base so they can pass behind tree tops."*
*   **Key Guardrail**: Do not write offset or dimension checks manually in C# script updates. Set the hitbox coordinates in the editor, and consume `entity.Bounds` directly in collision checks.
