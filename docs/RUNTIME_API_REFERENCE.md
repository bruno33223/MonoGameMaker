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
| **`Camera3D`** | Perspective 3D/2.5D camera, view/projection matrices, trauma screen shake, and unprojection raycasting | **[Camera3D Manual](runtime/Camera3D.md)** |
| **`SceneManager`** | Synchronous level loading (`LoadScene`) and Content boots | **[SceneManager Manual](runtime/SceneManager.md)** |
| **`TextRenderer`** | Static global text rendering and UI space printing | **[TextRenderer Manual](runtime/TextRenderer.md)** |
| **`ImGuiUI`** | Immediate-mode UI layout drawing and canvas isolation | **[ImGuiUI Manual](runtime/ImGuiUI.md)** |
| **`ProjectMigrator`** | Idempotent automatic migration of legacy projects and scripts | **[ProjectMigrator Manual](runtime/ProjectMigrator.md)** |
| **`TemplateEngine`** | Autonomous project scaffolding, subsystem cloning, and runtime synchronization | **[TemplateEngine Manual](runtime/TemplateEngine.md)** |
| **`SimulationStates`** | Control simulation loop (Play, Pause, Step) and focus-based input isolation | **[SimulationStates Manual](runtime/SimulationStates.md)** |
| **`SaveSystem`** | JSON-based Save/Load persistence for GameState variables | **[SaveSystem Manual](runtime/SaveSystem.md)** |
| **`FileManagement`** | Active hub context creations and background deletion pipeline | **[FileManagement Manual](runtime/FileManagement.md)** |
| **`CustomFonts`** | Procedures for creating, editing, compiling, and drawing custom fonts | **[CustomFonts Manual](runtime/CustomFonts.md)** |
| **`ScriptTeardown`** | Deterministic script disposal and memory management during hot reloading | **[ScriptTeardown Manual](runtime/ScriptTeardown.md)** |
| **`SelectionContext`** | Segregated active selection contexts for entities and resources | **[SelectionContext Manual](runtime/SelectionContext.md)** |
| **`CommandSystem`** | Editor command-driven state mutations and Undo/Redo history stacks | **[CommandSystem Manual](runtime/CommandSystem.md)** |
| **`PbrShaders`** | Physically-Based Rendering HLSL shader with normal mapping, directional/point lights, and emissive HDR | **[PbrShaders Manual](runtime/PbrShaders.md)** |
| **`PostProcessManager`** | High-performance RenderTarget2D pipeline, multi-pass bloom, separable Gaussian blur, and ACES Filmic tone mapping | **[PostProcessManager Manual](runtime/PostProcessManager.md)** |
| **`PbrMaterial`** | PBR material properties container (Albedo, Normal, Emissive maps, Roughness, Metallic) with auto shader binding | **[PbrMaterial Manual](runtime/PbrMaterial.md)** |
| **`TextureProcessor`** | Procedural texture processing (background removal, Sobel normal map generation, emissive extraction, PBR material creation) | **[TextureProcessor Manual](runtime/TextureProcessor.md)** |
| **`ParticleSystem3D`** | Zero-GC 3D/2.5D volumetric particle emitter with kinematics, presets (explosions, sparks, shockwaves, thrusters), and 2D/3D billboard renderers | **[ParticleSystem3D Manual](runtime/ParticleSystem3D.md)** |

---

## Advanced Combat, 3D & Audio Runtime Extensions (Fase 3)

| Component | Description | Reference Link |
| :--- | :--- | :--- |
| **`IPoolable`** | Zero-GC lifecycle recycling interface (`OnSpawn`, `OnDespawn`, `Reset`) | **[IPoolable Manual](runtime/IPoolable.md)** |
| **`ObjectPool<T>`** | High-performance thread-safe Zero-GC object pool with pre-warming | **[ObjectPool Manual](runtime/ObjectPool.md)** |
| **`ResolutionManager`** | Virtual 1080p resolution, letterbox/pillarbox matrices and coordinate conversion | **[ResolutionManager Manual](runtime/ResolutionManager.md)** |
| **`SoundSynthesizer`** | Procedural 16-bit 44.1kHz DSP software audio synthesis with Tanh analog mastering | **[SoundSynthesizer Manual](runtime/SoundSynthesizer.md)** |
| **`AudioManager`** | Multi-bus audio management, voice stealing, timestamp throttling, loops and headless fallback | **[AudioManager Manual](runtime/AudioManager.md)** |
| **`Mp3AudioLoader`** | Pure managed NLayer MP3 decoding, WSOLA speech time-stretching and cockpit Schroeder reverb | **[Mp3AudioLoader Manual](runtime/Mp3AudioLoader.md)** |
| **`BitmapFontRenderer`** | Built-in procedural 5x7 matrix bitmap font in pure C# with PT-BR Latin-1 support | **[BitmapFontRenderer Manual](runtime/BitmapFontRenderer.md)** |
| **`Mesh3DFactory`** | Procedural 3D mesh synthesis (spheres, perturbed asteroids, shards, spacecraft hulls, shield domes) | **[Mesh3DFactory Manual](runtime/Mesh3DFactory.md)** |
| **`CombatContext`** | Context Object unifying combat frame state, services, and zero parameter tunneling | **[CombatContext Manual](runtime/CombatContext.md)** |
| **`GameFlowController`** | State machine governing application life cycle and screen flows decoupled from GameState | **[GameFlowController Manual](runtime/GameFlowController.md)** |
| **`EnemyEntityBehavior`** | Base class for hostile combat entity behaviors integrating with EntityManager and Inspector | **[EnemyEntityBehavior Manual](runtime/EnemyEntityBehavior.md)** |
| **`BossModularPart`** | Modular anatomical sub-parts for epic bosses with local offset transforms and independent health | **[BossModularPart Manual](runtime/BossModularPart.md)** |
| **`InputService`** | Multiplatform managed input service (Keyboard/Mouse/GamePad, 1080p virtual coords, deadzone, zero Win32 P/Invoke) | **[InputService Manual](runtime/InputService.md)** |


---

## Real-Time Strategy (RTS) Engine Architecture (Fase 2)

| Component | Description | Reference Link |
| :--- | :--- | :--- |
| **`RectangleF`** | Estrutura imutável de ponto flutuante alinhada aos eixos para consultas AABB e seleções em caixa | **[RectangleF Manual](runtime/RectangleF.md)** |
| **`SpatialHashGrid`** | Particionamento espacial 2D CSR contíguo Zero-GC para até 1.000 unidades com consultas de raio e caixa em O(1) | **[SpatialHashGrid Manual](runtime/SpatialHashGrid.md)** |
| **`CostField`** | Matriz 1D contígua de custos de terreno e obstáculos intransponíveis para campos de fluxo | **[CostField Manual](runtime/CostField.md)** |
| **`IntegrationField`** | Propagação de onda Wavefront/Dijkstra contígua com fila estática circular sem alocação | **[IntegrationField Manual](runtime/IntegrationField.md)** |
| **`FlowField`** | Campo vetorial de gradiente com amostragem bilinear suave contínua em O(1) | **[FlowField Manual](runtime/FlowField.md)** |
| **`FlowFieldManager`** | Gestor centralizado com pool LRU multi-alvo para navegação independente de tropas | **[FlowFieldManager Manual](runtime/FlowFieldManager.md)** |
| **`FogState`** | Enumeração formal de 3 estados do autômato de Névoa de Guerra (Unexplored, Explored, Visible) | **[FogState Manual](runtime/FogState.md)** |
| **`FogOfWarSystem`** | Névoa de guerra em 3 estados com Line-of-Sight desacoplado a 15-20Hz e upload de textura Alpha8 | **[FogOfWarSystem Manual](runtime/FogOfWarSystem.md)** |
| **`UnitCommand`** | Estruturas e enums de ordens militares e papéis táticos para despacho de unidades RTS | **[UnitCommand Manual](runtime/UnitCommand.md)** |
| **`UnitCommandQueue`** | Fila circular estática Zero-GC para encadeamento e despacho sequencial de ordens RTS | **[UnitCommandQueue Manual](runtime/UnitCommandQueue.md)** |
| **`RTSSelectionManager`** | Seleção tática por clique e Marquee Box com prioridade militar e conversão de tela/câmera | **[RTSSelectionManager Manual](runtime/RTSSelectionManager.md)** |
| **`ControlGroupManager`** | Gestor de grupos táticos (Ctrl+1..9 e Shift+1..9) com descarte de mortas e recentralização de câmera | **[ControlGroupManager Manual](runtime/ControlGroupManager.md)** |
| **`UnitInstanceData`** | Estrutura de vértice de 64 bytes alinhada à linha de cache para GPU Hardware Instancing | **[UnitInstanceData Manual](runtime/UnitInstanceData.md)** |
| **`InstancedUnitRenderer`** | Renderização em lote via GPU Hardware Instancing em 1 única Draw Call para 1.000 unidades | **[InstancedUnitRenderer Manual](runtime/InstancedUnitRenderer.md)** |
| **`RtsUnitPhysicsData`** | Estrutura não-gerenciada contígua de 48 bytes alinhada à linha de cache para física RTS sem alocação | **[RtsUnitPhysicsData Manual](runtime/RtsUnitPhysicsData.md)** |
| **`RtsPhysics`** | Núcleo matemático de repulsão suave de Reynolds (1/d^2) e relaxamento rígido (Circle-Circle Hard Push) | **[RtsPhysics Manual](runtime/RtsPhysics.md)** |
| **`RtsSimulationSystem`** | Orquestrador de simulação em lote Zero-GC para até 1.000 unidades com FlowField e SpatialGrid | **[RtsSimulationSystem Manual](runtime/RtsSimulationSystem.md)** |

---

## Shared Runtime Integration

All classes listed above are compiled inside the core IDE library assembly under the namespace `MonoGameMaker.Runtime`. Scaffolded user game projects automatically reference these types, ensuring 100% API compatibility between editor viewport simulation and standalone game executables.

### Doc-as-Code (IntelliSense/LLM Reference)
- **[RUNTIME_SUMMARY.cs](file:///src/Runtime/Docs/RUNTIME_SUMMARY.cs)**: Clean C# signatures and extensive XML comments for all runtime modules, serving as an in-IDE IntelliSense dictionary and prompt context for code-writing LLMs.
