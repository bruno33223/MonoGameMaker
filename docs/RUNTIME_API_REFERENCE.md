# Runtime API Reference

This document maps all public classes, interfaces, and static methods available to behaviors/scripts inside the Mono GameMaker runtime engine.

## `MonoGameMaker.Runtime` Namespace

---

### `IEntityScript` (Interface)
All custom behavior scripts must implement this interface.

#### `void Initialize(GameEntity entity, Dictionary<string, string> properties)`
- **Signature**: `void Initialize(GameEntity entity, Dictionary<string, string> properties)`
- **Explanation**: Called once when the entity is spawned or loaded into the scene. Used to parse custom properties and initialize variables.
- **Example**:
  ```csharp
  public void Initialize(GameEntity entity, Dictionary<string, string> properties)
  {
      this._entity = entity;
      if (properties.TryGetValue("Speed", out var speedStr))
      {
          float.TryParse(speedStr, out this._speed);
      }
  }
  ```

#### `void Update(GameTime gameTime)`
- **Signature**: `void Update(GameTime gameTime)`
- **Explanation**: Executed once per physics/logic frame to update behavior state.
- **Example**:
  ```csharp
  public void Update(GameTime gameTime)
  {
      var keyboardState = Microsoft.Xna.Framework.Input.Keyboard.GetState();
      if (keyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Right))
      {
          _entity.Position = new Vector2(_entity.Position.X + 5f, _entity.Position.Y);
      }
  }
  ```

#### `void Draw(SpriteBatch spriteBatch)`
- **Signature**: `void Draw(SpriteBatch spriteBatch)`
- **Explanation**: Optional custom world-space drawing loop. If empty, the engine defaults to rendering the entity's texture at its position.
- **Example**:
  ```csharp
  public void Draw(SpriteBatch spriteBatch)
  {
      // Custom sprite effects or particle drawing
      spriteBatch.Draw(_entity.Texture, _entity.Position, Color.Red);
  }
  ```

#### `void DrawUI(SpriteBatch spriteBatch)`
- **Signature**: `void DrawUI(SpriteBatch spriteBatch)`
- **Explanation**: Executed during the Screen Space UI rendering pass. Used for HUD, scores, or text elements that ignore camera transforms.
- **Example**:
  ```csharp
  public void DrawUI(SpriteBatch spriteBatch)
  {
      // Render text at absolute screen coordinates
      // spriteBatch.DrawString(myFont, "Score: 100", new Vector2(10, 10), Color.White);
  }
  ```

---

### `GameEntity` (Class)
Represents a physical entity instance present in the active scene.

#### Properties
- **`string PrefabName`** (`get; set;`): The name of the originating `.prefab` definition.
- **`Texture2D? Texture`** (`get; set;`): The sprite texture used by the entity.
- **`Vector2 Position`** (`get; set;`): The coordinates of the entity in World Space.
- **`IEntityScript? Script`** (`get; set;`): The active script instance executing behavior logic.
- **`string Tag`** (`get; set;`): Group/collision identifier tag (defaults to `"Default"`).
- **`bool IsDestroyed`** (`get; set;`): Triggers removal of this entity from the scene at the end of the current frame if set to `true`.
- **`Rectangle Bounds`** (`get;`): Returns the bounding rectangle of the entity based on texture size (falls back to 64x64 if texture is null).

---

### `EntityManager` (Static Class)
Orchestrates entity life cycles, dynamic spawning, destruction, and collision lookups.

#### `public static void Clear()`
- **Signature**: `static void Clear()`
- **Explanation**: Purges all entities from the active simulation list.
- **Example**:
  ```csharp
  EntityManager.Clear();
  ```

#### `public static GameEntity Spawn(string prefabName, Vector2 position)`
- **Signature**: `static GameEntity Spawn(string prefabName, Vector2 position)`
- **Explanation**: Dynamically spawns a new instance of a prefab at the designated coordinate, executing its script's `Initialize` method if present.
- **Example**:
  ```csharp
  var coin = EntityManager.Spawn("coin_prefab", new Vector2(400, 300));
  ```

#### `public static void Destroy(GameEntity entity)`
- **Signature**: `static void Destroy(GameEntity entity)`
- **Explanation**: Marks the selected entity for deletion. The entity will be cleaned up safely at the end of the physics loop.
- **Example**:
  ```csharp
  EntityManager.Destroy(enemyEntity);
  ```

#### `public static GameEntity GetFirstColliding(GameEntity caller, string targetTag)`
- **Signature**: `static GameEntity GetFirstColliding(GameEntity caller, string targetTag)`
- **Explanation**: Scans all active entities and returns the first entity of the specified tag whose bounds intersect with the caller's bounds.
- **Example**:
  ```csharp
  var wall = EntityManager.GetFirstColliding(_entity, "Obstacle");
  if (wall != null)
  {
      // Collision response logic
  }
  ```

---

### `SceneManager` (Static Class)
Handles active level transitions and deserialization.

#### `public static void LoadScene(string sceneName)`
- **Signature**: `static void LoadScene(string sceneName)`
- **Explanation**: Synchronously unloads the active scene, purges the `EntityManager`, and loads a new scene configuration from the JSON file.
- **Example**:
  ```csharp
  SceneManager.LoadScene("level_2");
  ```

#### Properties
- **`string CurrentSceneName`** (`get;`): Returns the clean name of the current level.
- **`RuntimeScene CurrentScene`** (`get;`): Returns metadata of the active scene (dimensions, background).

---

### `Camera2D` (Static Class)
Calculates viewport transformations and camera clamping.

#### `public static void LookAt(Vector2 target, int viewportWidth, int viewportHeight)`
- **Signature**: `static void LookAt(Vector2 target, int viewportWidth, int viewportHeight)`
- **Explanation**: Centres the camera's focus on a target coordinate, clamping the view boundaries to the bounds of the active level.
- **Example**:
  ```csharp
  Camera2D.LookAt(_entity.Position, 1280, 720);
  ```

#### Properties
- **`Vector2 Position`** (`get; set;`): The camera coordinate offset.
- **`Matrix Transform`** (`get;`): The calculated translation matrix to pass into `SpriteBatch.Begin()`.

---

### `GameState` (Static Class)
Handles persistent global values across level boundaries.

#### `public static void Set<T>(string key, T value)`
- **Signature**: `static void Set<T>(string key, T value)`
- **Explanation**: Stores a persistent object under the specified string key (removes key if value is null).
- **Example**:
  ```csharp
  GameState.Set("PlayerLives", 3);
  ```

#### `public static T Get<T>(string key, T defaultValue = default)`
- **Signature**: `static T Get<T>(string key, T defaultValue = default)`
- **Explanation**: Retrieves a persistent variable, converting it to type `T`. Returns the default value if the key does not exist.
- **Example**:
  ```csharp
  int score = GameState.Get<int>("CurrentScore", 0);
  ```
