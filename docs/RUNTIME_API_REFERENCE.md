# Runtime API Reference Index

This directory contains the detailed documentation manuals for each core class, interface, and subsystem inside the Mono GameMaker runtime ecosystem.

## Core Runtime Modules

| Component | Description | Reference Link |
| :--- | :--- | :--- |
| **`EntityBehavior`** | Script lifecycle events (`Awake`, `Update`, `Draw`, `DrawUI`, `OnCollision`) | **[EntityBehavior Manual](runtime/EntityBehavior.md)** |
| **`GameEntity`** | Entity properties (`Position`, `Texture`, `Tag`, `Bounds`, `Script`, `PlayAnimation`) | **[GameEntity Manual](runtime/GameEntity.md)** |
| **`CollisionMasks`** | Custom hitboxes, offsets, scaling boundaries, and fallbacks | **[CollisionMasks Manual](runtime/CollisionMasks.md)** |
| **`Animations`** | Uniform grid-based spritesheet animators (`SourceRect` updates, `PlayAnimation`) | **[Animations Manual](runtime/Animations.md)** |
| **`EntityManager`** | Life cycles (`Spawn`, `Destroy`) and pairwise collision trigger loop | **[EntityManager Manual](runtime/EntityManager.md)** |
| **`GameState`** | Global persistent storage (`Set`, `Get`) and type conversion fallbacks | **[GameState Manual](runtime/GameState.md)** |
| **`Camera2D`** | Camera translation matrix calculation (`Transform`) and boundaries clamping (`LookAt`) | **[Camera2D Manual](runtime/Camera2D.md)** |
| **`SceneManager`** | Synchronous level loading (`LoadScene`) and Content boots | **[SceneManager Manual](runtime/SceneManager.md)** |
| **`TextRenderer`** | Static global text rendering and UI space printing | **[TextRenderer Manual](runtime/TextRenderer.md)** |
| **`ImGuiUI`** | Immediate-mode UI layout drawing and canvas isolation | **[ImGuiUI Manual](runtime/ImGuiUI.md)** |
| **`ProjectMigrator`** | Idempotent automatic migration of legacy projects and scripts | **[ProjectMigrator Manual](runtime/ProjectMigrator.md)** |

---

## Shared Runtime Integration

All classes listed above are compiled inside the core IDE library assembly under the namespace `MonoGameMaker.Runtime`. Scaffolded user game projects automatically reference these types, ensuring 100% API compatibility between editor viewport simulation and standalone game executables.

### Doc-as-Code (IntelliSense/LLM Reference)
- **[RUNTIME_SUMMARY.cs](file:///src/Runtime/Docs/RUNTIME_SUMMARY.cs)**: Clean C# signatures and extensive XML comments for all runtime modules, serving as an in-IDE IntelliSense dictionary and prompt context for code-writing LLMs.
