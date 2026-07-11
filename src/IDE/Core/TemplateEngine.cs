using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MonoGameMaker.IDE.Core
{
    public static class TemplateEngine
    {
        public static string GetDotnetPath()
        {
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string localDotnet = Path.Combine(userProfile, ".dotnet", "dotnet.exe");
            if (File.Exists(localDotnet)) return localDotnet;

            string pfDotnet = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "dotnet.exe");
            if (File.Exists(pfDotnet)) return pfDotnet;

            return "dotnet";
        }

        public static void ConfigureDotnetPath(ProcessStartInfo psi)
        {
            string dotnetPath = GetDotnetPath();
            string? dotnetDir = Path.GetDirectoryName(dotnetPath);
            if (!string.IsNullOrEmpty(dotnetDir))
            {
                string currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
                string newPath = dotnetDir + Path.PathSeparator + currentPath;
                psi.EnvironmentVariables["PATH"] = newPath;
            }
        }

        public static async Task<bool> ScaffoldProjectAsync(string targetDirectory, string projectName, Action<string> logCallback)
        {
            try
            {
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                logCallback($"Scaffolding project '{projectName}' in '{targetDirectory}'...");

                string dotnetPath = GetDotnetPath();
                logCallback($"Using dotnet path: {dotnetPath}");

                // 1. Run dotnet new mgdesktopgl -n projectName -o targetDirectory
                var processInfo = new ProcessStartInfo
                {
                    FileName = dotnetPath,
                    Arguments = $"new mgdesktopgl -n \"{projectName}\" -o \"{targetDirectory}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                ConfigureDotnetPath(processInfo);

                using (var process = new Process { StartInfo = processInfo })
                {
                    process.Start();
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    logCallback(output);
                    if (process.ExitCode != 0)
                    {
                        logCallback($"Error scaffolding project (Exit Code {process.ExitCode}): {error}");
                        return false;
                    }
                }

                // 2. Locate generated csproj
                string csprojPath = Path.Combine(targetDirectory, $"{projectName}.csproj");
                if (!File.Exists(csprojPath))
                {
                    logCallback($"Error: csproj not found at {csprojPath}");
                    return false;
                }

                // 3. Edit csproj:
                // - Change TargetFramework to net8.0
                // - Add CopyToOutputDirectory directives for Scenes and Prefabs
                logCallback("Updating csproj configuration for target framework, scenes, and prefabs...");
                string csprojContent = File.ReadAllText(csprojPath);
                
                csprojContent = Regex.Replace(csprojContent, @"<TargetFramework>.*?</TargetFramework>", "<TargetFramework>net8.0</TargetFramework>");

                string copyDirectives = @"
  <PropertyGroup>
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
  </PropertyGroup>
  <ItemGroup>
    <None Update=""Content\Scenes\**\*.*"">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Update=""Prefabs\**\*.*"">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>";
                csprojContent = csprojContent.Replace("</Project>", copyDirectives);
                File.WriteAllText(csprojPath, csprojContent);

                // 4. Create base directories
                logCallback("Creating folder structure...");
                string contentPath = Path.Combine(targetDirectory, "Content");
                string texturesPath = Path.Combine(contentPath, "Textures");
                string audioPath = Path.Combine(contentPath, "Audio");
                string modelsPath = Path.Combine(contentPath, "Models");
                string scenesPath = Path.Combine(contentPath, "Scenes");
                string scriptsPath = Path.Combine(targetDirectory, "Scripts");
                string prefabsPath = Path.Combine(targetDirectory, "Prefabs");

                Directory.CreateDirectory(texturesPath);
                Directory.CreateDirectory(audioPath);
                Directory.CreateDirectory(modelsPath);
                Directory.CreateDirectory(scenesPath);
                Directory.CreateDirectory(scriptsPath);
                Directory.CreateDirectory(prefabsPath);

                // 5. Create default scene_init.json
                string sceneInitPath = Path.Combine(scenesPath, "scene_init.json");
                if (!File.Exists(sceneInitPath))
                {
                    File.WriteAllText(sceneInitPath, "{\n  \"Width\": 1280,\n  \"Height\": 720,\n  \"BackgroundColor\": {\n    \"X\": 0.1,\n    \"Y\": 0.1,\n    \"Z\": 0.2\n  },\n  \"BackgroundImage\": \"\",\n  \"Instances\": []\n}");
                }

                // 6. Inject Runtime folders and files
                logCallback("Injecting SceneLoader and runtime scripts...");
                string runtimeDir = Path.Combine(targetDirectory, "Runtime");
                Directory.CreateDirectory(runtimeDir);

                string entityBehaviorPath = Path.Combine(runtimeDir, "EntityBehavior.cs");
                File.WriteAllText(entityBehaviorPath, GetEntityBehaviorCode());

                string gameEntityPath = Path.Combine(runtimeDir, "GameEntity.cs");
                File.WriteAllText(gameEntityPath, GetGameEntityCode());

                string sharedTypesPath = Path.Combine(runtimeDir, "SharedTypes.cs");
                File.WriteAllText(sharedTypesPath, GetSharedTypesCode());

                string sceneLoaderPath = Path.Combine(runtimeDir, "SceneLoader.cs");
                File.WriteAllText(sceneLoaderPath, GetSceneLoaderCode());

                string entityManagerPath = Path.Combine(runtimeDir, "EntityManager.cs");
                File.WriteAllText(entityManagerPath, GetEntityManagerCode());

                string gameStatePath = Path.Combine(runtimeDir, "GameState.cs");
                File.WriteAllText(gameStatePath, GetGameStateCode());

                string sceneManagerPath = Path.Combine(runtimeDir, "SceneManager.cs");
                File.WriteAllText(sceneManagerPath, GetSceneManagerCode());

                string camera2DPath = Path.Combine(runtimeDir, "Camera2D.cs");
                File.WriteAllText(camera2DPath, GetCamera2DCode());

                // 7. Inject Game1.cs
                logCallback("Injecting customized Game1 boilerplate...");
                string game1Path = Path.Combine(targetDirectory, "Game1.cs");
                File.WriteAllText(game1Path, GetGame1Code(projectName));

                // 7.5. Inject AI architecture manifests
                logCallback("Generating AI architecture manifests (.cursorrules and AI_ARCHITECTURE.md)...");
                string cursorRulesPath = Path.Combine(targetDirectory, ".cursorrules");
                string aiArchPath = Path.Combine(targetDirectory, "AI_ARCHITECTURE.md");
                string manifestContent = GetAiManifestTemplate(projectName);
                File.WriteAllText(cursorRulesPath, manifestContent, System.Text.Encoding.UTF8);
                File.WriteAllText(aiArchPath, manifestContent, System.Text.Encoding.UTF8);

                // 8. Restore the project
                logCallback("Running dotnet restore on the scaffolded project...");
                var restoreProcessInfo = new ProcessStartInfo
                {
                    FileName = dotnetPath,
                    Arguments = $"restore \"{csprojPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                ConfigureDotnetPath(restoreProcessInfo);

                using (var process = new Process { StartInfo = restoreProcessInfo })
                {
                    process.Start();
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    logCallback(output);
                    if (process.ExitCode != 0)
                    {
                        logCallback($"Warning: restore failed, but project is structured. Details: {error}");
                    }
                }

                logCallback("Project successfully scaffolded!");
                return true;
            }
            catch (Exception ex)
            {
                logCallback($"Unexpected error during project scaffolding: {ex.Message}");
                return false;
            }
        }

        private static string GetEntityBehaviorCode()
        {
            return @"using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameMaker.Runtime
{
    public abstract class EntityBehavior
    {
        public GameEntity Entity { get; internal set; }
        public Dictionary<string, string> Properties { get; internal set; }

        public virtual void Awake() { }
        public virtual void Update(GameTime gameTime) { }
        public virtual void Draw(SpriteBatch spriteBatch) { }
        public virtual void DrawUI(SpriteBatch spriteBatch) { }
        public virtual void OnCollision(GameEntity other) { }
    }
}
";
        }

        private static string GetGameEntityCode()
        {
            return @"using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameMaker.Runtime
{
    public class GameEntity
    {
        public string PrefabName { get; set; } = string.Empty;
        public Texture2D Texture { get; set; }
        public Vector2 Position { get; set; }
        public EntityBehavior Script { get; set; }
        public string Tag { get; set; } = ""Default"";
        public bool IsDestroyed { get; set; } = false;
        
        public Rectangle? SourceRect { get; set; } = null;
        public Vector2 HitboxOffset { get; set; } = Vector2.Zero;
        public Vector2 HitboxSize { get; set; } = Vector2.Zero;

        private int _animFrameWidth;
        private int _animFrameHeight;
        private int _animStartFrame;
        private int _animEndFrame;
        private float _animFps;
        private double _animTimer;
        private int _animCurrentFrame;
        private bool _isAnimating;

        public Rectangle Bounds
        {
            get
            {
                int w = HitboxSize.X > 0f ? (int)HitboxSize.X : (SourceRect.HasValue ? SourceRect.Value.Width : (Texture != null ? Texture.Width : 64));
                int h = HitboxSize.Y > 0f ? (int)HitboxSize.Y : (SourceRect.HasValue ? SourceRect.Value.Height : (Texture != null ? Texture.Height : 64));
                return new Rectangle((int)(Position.X + HitboxOffset.X), (int)(Position.Y + HitboxOffset.Y), w, h);
            }
        }

        public void PlayAnimation(int frameWidth, int frameHeight, int startFrame, int endFrame, float fps)
        {
            if (_animFrameWidth == frameWidth && _animFrameHeight == frameHeight &&
                _animStartFrame == startFrame && _animEndFrame == endFrame && _animFps == fps && _isAnimating)
            {
                return;
            }

            _animFrameWidth = frameWidth;
            _animFrameHeight = frameHeight;
            _animStartFrame = startFrame;
            _animEndFrame = endFrame;
            _animFps = fps;
            _animTimer = 0.0;
            _animCurrentFrame = startFrame;
            _isAnimating = true;

            UpdateSourceRect();
        }

        public void UpdateAnimation(GameTime gameTime)
        {
            if (!_isAnimating) return;

            if (_animFps <= 0f)
            {
                _animCurrentFrame = _animStartFrame;
                UpdateSourceRect();
                return;
            }

            _animTimer += gameTime.ElapsedGameTime.TotalSeconds;
            double frameDuration = 1.0 / _animFps;

            if (_animTimer >= frameDuration)
            {
                int framesToAdvance = (int)(_animTimer / frameDuration);
                _animTimer %= frameDuration;

                int frameCount = _animEndFrame - _animStartFrame + 1;
                if (frameCount <= 0) frameCount = 1;

                int localFrameIndex = (_animCurrentFrame - _animStartFrame + framesToAdvance) % frameCount;
                _animCurrentFrame = _animStartFrame + localFrameIndex;

                UpdateSourceRect();
            }
        }

        private void UpdateSourceRect()
        {
            if (Texture == null)
            {
                SourceRect = null;
                return;
            }

            int columns = Texture.Width / _animFrameWidth;
            if (columns <= 0) columns = 1;

            int col = _animCurrentFrame % columns;
            int row = _animCurrentFrame / columns;

            SourceRect = new Rectangle(col * _animFrameWidth, row * _animFrameHeight, _animFrameWidth, _animFrameHeight);
        }
    }
}
";
        }

        private static string GetSharedTypesCode()
        {
            return @"using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameMaker.Runtime
{
    public class RuntimeScene
    {
        public int Width { get; set; } = 1280;
        public int Height { get; set; } = 720;
        public Color BackgroundColor { get; set; } = Color.CornflowerBlue;
        public Texture2D BackgroundImage { get; set; }
        public List<GameEntity> Entities { get; set; } = new List<GameEntity>();
    }

    public class SceneData
    {
        public int Width { get; set; } = 1280;
        public int Height { get; set; } = 720;
        public System.Numerics.Vector3 BackgroundColor { get; set; } = new System.Numerics.Vector3(0.1f, 0.1f, 0.2f);
        public string BackgroundImage { get; set; } = string.Empty;
        public List<EntityInstance> Instances { get; set; } = new List<EntityInstance>();
    }

    public class EntityInstance
    {
        public string prefabName { get; set; } = string.Empty;
        public float x { get; set; }
        public float y { get; set; }
        public Dictionary<string, string> CustomProperties { get; set; } = new Dictionary<string, string>();
    }

    public class PrefabData
    {
        public string TextureName { get; set; } = string.Empty;
        public string ScriptName { get; set; } = string.Empty;
        public string Tag { get; set; } = ""Default"";
        public float HitboxOffsetX { get; set; } = 0f;
        public float HitboxOffsetY { get; set; } = 0f;
        public float HitboxWidth { get; set; } = 0f;
        public float HitboxHeight { get; set; } = 0f;
        public Dictionary<string, string> CustomProperties { get; set; } = new Dictionary<string, string>();
    }
}
";
        }

        private static string GetSceneLoaderCode()
        {
            return @"using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameMaker.Runtime
{
    public static class SceneLoader
    {
        public static RuntimeScene LoadScene(string jsonPath, ContentManager content)
        {
            var entities = new List<GameEntity>();
            int sceneWidth = 1280;
            int sceneHeight = 720;
            Color bgColor = Color.CornflowerBlue;
            Texture2D bgTexture = null;

            try
            {
                if (!File.Exists(jsonPath))
                {
                    var contentRoot = content.RootDirectory;
                    var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, contentRoot, jsonPath);
                    if (File.Exists(fullPath))
                    {
                        jsonPath = fullPath;
                    }
                    else
                    {
                        var alternativePath = Path.Combine(Directory.GetCurrentDirectory(), contentRoot, jsonPath);
                        if (File.Exists(alternativePath))
                        {
                            jsonPath = alternativePath;
                        }
                    }
                }

                if (File.Exists(jsonPath))
                {
                    string jsonContent = File.ReadAllText(jsonPath);
                    var options = new JsonSerializerOptions { IncludeFields = true };
                    var sceneData = JsonSerializer.Deserialize<SceneData>(jsonContent, options);
                    
                    if (sceneData != null)
                    {
                        sceneWidth = sceneData.Width;
                        sceneHeight = sceneData.Height;
                        
                        var numColor = sceneData.BackgroundColor;
                        bgColor = new Color(numColor.X, numColor.Y, numColor.Z);

                        if (!string.IsNullOrEmpty(sceneData.BackgroundImage))
                        {
                            try
                            {
                                string bgAssetPath = ""Textures/"" + sceneData.BackgroundImage;
                                if (bgAssetPath.EndsWith("".png"") || bgAssetPath.EndsWith("".jpg"") || bgAssetPath.EndsWith("".jpeg""))
                                {
                                    bgAssetPath = bgAssetPath.Substring(0, bgAssetPath.LastIndexOf('.'));
                                }
                                bgTexture = content.Load<Texture2D>(bgAssetPath);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($""Error loading background image {sceneData.BackgroundImage}: {ex.Message}"");
                            }
                        }

                        if (sceneData.Instances != null)
                        {
                            foreach (var inst in sceneData.Instances)
                            {
                                // Load Prefab metadata
                                string prefabPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ""Prefabs"", $""{inst.prefabName}.prefab"");
                                if (!File.Exists(prefabPath))
                                {
                                    prefabPath = Path.Combine(Directory.GetCurrentDirectory(), ""Prefabs"", $""{inst.prefabName}.prefab"");
                                }

                                string textureName = """";
                                string scriptName = """";
                                var instPrefab = new PrefabData();

                                if (File.Exists(prefabPath))
                                {
                                    try
                                    {
                                        string prefabJson = File.ReadAllText(prefabPath);
                                        var prefabData = JsonSerializer.Deserialize<PrefabData>(prefabJson);
                                        if (prefabData != null)
                                        {
                                            instPrefab = prefabData;
                                            textureName = prefabData.TextureName;
                                            scriptName = prefabData.ScriptName;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($""Error loading prefab data: {ex.Message}"");
                                    }
                                }

                                Texture2D texture = null;
                                if (!string.IsNullOrEmpty(textureName))
                                {
                                    try
                                    {
                                        string assetPath = ""Textures/"" + textureName;
                                        if (assetPath.EndsWith("".png"") || assetPath.EndsWith("".jpg"") || assetPath.EndsWith("".jpeg""))
                                        {
                                            assetPath = assetPath.Substring(0, assetPath.LastIndexOf('.'));
                                        }
                                        texture = content.Load<Texture2D>(assetPath);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($""Error loading texture {textureName}: {ex.Message}"");
                                    }
                                }

                                 // Reflection script loading
                                 EntityBehavior scriptInstance = null;
                                 if (!string.IsNullOrEmpty(scriptName))
                                 {
                                     try
                                     {
                                         Type scriptType = null;
                                         foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                                         {
                                             var t = assembly.GetType(scriptName);
                                             if (t == null)
                                             {
                                                 t = assembly.GetType(assembly.GetName().Name + ""."" + scriptName);
                                             }
                                             if (t == null)
                                             {
                                                 t = assembly.GetType(assembly.GetName().Name + "".Scripts."" + scriptName);
                                             }
                                             if (t != null)
                                             {
                                                 scriptType = t;
                                                 break;
                                             }
                                         }

                                         if (scriptType != null && typeof(EntityBehavior).IsAssignableFrom(scriptType))
                                         {
                                             scriptInstance = (EntityBehavior)Activator.CreateInstance(scriptType);
                                         }
                                     }
                                     catch (Exception ex)
                                     {
                                         Console.WriteLine($""Error resolving script {scriptName}: {ex.Message}"");
                                     }
                                 }

                                 // Merge properties
                                 var mergedProps = new Dictionary<string, string>();
                                 if (instPrefab.CustomProperties != null)
                                 {
                                     foreach (var kv in instPrefab.CustomProperties)
                                     {
                                         mergedProps[kv.Key] = kv.Value;
                                     }
                                 }
                                 if (inst.CustomProperties != null)
                                 {
                                     foreach (var kv in inst.CustomProperties)
                                     {
                                         mergedProps[kv.Key] = kv.Value;
                                     }
                                 }

                                 var gameEntity = new GameEntity
                                 {
                                     PrefabName = inst.prefabName,
                                     Texture = texture,
                                     Position = new Vector2(inst.x, inst.y),
                                     Script = scriptInstance,
                                     Tag = instPrefab.Tag ?? ""Default"",
                                     HitboxOffset = new Vector2(instPrefab.HitboxOffsetX, instPrefab.HitboxOffsetY),
                                     HitboxSize = new Vector2(instPrefab.HitboxWidth, instPrefab.HitboxHeight)
                                 };

                                 if (scriptInstance != null)
                                 {
                                     try
                                     {
                                         scriptInstance.Entity = gameEntity;
                                         scriptInstance.Properties = mergedProps;
                                         scriptInstance.Awake();
                                     }
                                     catch (Exception ex)
                                     {
                                         Console.WriteLine($""Error initializing script: {ex.Message}"");
                                     }
                                 }

                                entities.Add(gameEntity);
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($""Scene config file not found: {jsonPath}"");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($""Error parsing scene: {ex.Message}"");
            }

            return new RuntimeScene
            {
                Width = sceneWidth,
                Height = sceneHeight,
                BackgroundColor = bgColor,
                BackgroundImage = bgTexture,
                Entities = entities
            };
        }
    }
}
";
        }

        private static string GetEntityManagerCode()
        {
            return @"using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameMaker.Runtime
{
    public static class EntityManager
    {
        public static List<GameEntity> Entities = new List<GameEntity>();
        private static List<GameEntity> _entitiesToAdd = new List<GameEntity>();
        
        public static ContentManager Content { get; set; }

        public static void Clear()
        {
            Entities.Clear();
            _entitiesToAdd.Clear();
        }

        public static GameEntity Spawn(string prefabName, Vector2 position)
        {
            if (prefabName.EndsWith("".prefab"", StringComparison.OrdinalIgnoreCase))
            {
                prefabName = Path.GetFileNameWithoutExtension(prefabName);
            }

            string prefabPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ""Prefabs"", $""{prefabName}.prefab"");
            if (!File.Exists(prefabPath))
            {
                prefabPath = Path.Combine(Directory.GetCurrentDirectory(), ""Prefabs"", $""{prefabName}.prefab"");
            }

            string textureName = """";
            string scriptName = """";
            string tag = ""Default"";
            var instPrefab = new PrefabData();

            if (File.Exists(prefabPath))
            {
                try
                {
                    string prefabJson = File.ReadAllText(prefabPath);
                    var prefabData = System.Text.Json.JsonSerializer.Deserialize<PrefabData>(prefabJson);
                    if (prefabData != null)
                    {
                        instPrefab = prefabData;
                        textureName = prefabData.TextureName;
                        scriptName = prefabData.ScriptName;
                        tag = prefabData.Tag;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($""Error loading prefab data: {ex.Message}"");
                }
            }

            Texture2D texture = null;
            if (!string.IsNullOrEmpty(textureName) && Content != null)
            {
                try
                {
                    string assetPath = ""Textures/"" + textureName;
                    if (assetPath.EndsWith("".png"") || assetPath.EndsWith("".jpg"") || assetPath.EndsWith("".jpeg""))
                    {
                        assetPath = assetPath.Substring(0, assetPath.LastIndexOf('.'));
                    }
                    texture = Content.Load<Texture2D>(assetPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($""Error loading texture {textureName}: {ex.Message}"");
                }
            }

            EntityBehavior scriptInstance = null;
            if (!string.IsNullOrEmpty(scriptName))
            {
                try
                {
                    Type scriptType = null;
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        var t = assembly.GetType(scriptName);
                        if (t == null)
                        {
                            t = assembly.GetType(assembly.GetName().Name + ""."" + scriptName);
                        }
                        if (t == null)
                        {
                            t = assembly.GetType(assembly.GetName().Name + "".Scripts."" + scriptName);
                        }
                        if (t != null)
                        {
                            scriptType = t;
                            break;
                        }
                    }

                    if (scriptType != null && typeof(EntityBehavior).IsAssignableFrom(scriptType))
                    {
                        scriptInstance = (EntityBehavior)Activator.CreateInstance(scriptType);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($""Error resolving script {scriptName}: {ex.Message}"");
                }
            }

            var gameEntity = new GameEntity
            {
                PrefabName = prefabName,
                Texture = texture,
                Position = position,
                Script = scriptInstance,
                Tag = tag,
                HitboxOffset = new Vector2(instPrefab.HitboxOffsetX, instPrefab.HitboxOffsetY),
                HitboxSize = new Vector2(instPrefab.HitboxWidth, instPrefab.HitboxHeight)
            };

            if (scriptInstance != null)
            {
                try
                {
                    var prefabProps = new Dictionary<string, string>();
                    if (instPrefab.CustomProperties != null)
                    {
                        prefabProps = instPrefab.CustomProperties;
                    }
                    scriptInstance.Entity = gameEntity;
                    scriptInstance.Properties = prefabProps;
                    scriptInstance.Awake();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($""Error initializing script: {ex.Message}"");
                }
            }

            _entitiesToAdd.Add(gameEntity);
            return gameEntity;
        }

        public static void Destroy(GameEntity entity)
        {
            if (entity != null)
            {
                entity.IsDestroyed = true;
            }
        }

        public static GameEntity GetFirstColliding(GameEntity caller, string targetTag)
        {
            if (caller == null) return null;
            
            foreach (var entity in Entities)
            {
                if (entity != caller && !entity.IsDestroyed && entity.Tag == targetTag)
                {
                    if (caller.Bounds.Intersects(entity.Bounds))
                    {
                        return entity;
                    }
                }
            }
            return null;
        }

        public static void Update(GameTime gameTime)
        {
            if (_entitiesToAdd.Count > 0)
            {
                Entities.AddRange(_entitiesToAdd);
                _entitiesToAdd.Clear();
            }

            foreach (var entity in Entities)
            {
                if (!entity.IsDestroyed)
                {
                    entity.Script?.Update(gameTime);
                    entity.UpdateAnimation(gameTime);
                }
            }

            // O(N^2) pairwise collisions check
            for (int i = 0; i < Entities.Count; i++)
            {
                var entA = Entities[i];
                if (entA.IsDestroyed) continue;
                for (int j = i + 1; j < Entities.Count; j++)
                {
                    var entB = Entities[j];
                    if (entB.IsDestroyed) continue;
                    if (entA.Bounds.Intersects(entB.Bounds))
                    {
                        entA.Script?.OnCollision(entB);
                        entB.Script?.OnCollision(entA);
                    }
                }
            }

            Entities.RemoveAll(e => e.IsDestroyed);
        }

        public static void Draw(SpriteBatch spriteBatch, Texture2D defaultTexture)
        {
            foreach (var entity in Entities)
            {
                if (entity.IsDestroyed) continue;

                if (entity.Script != null)
                {
                    entity.Script.Draw(spriteBatch);
                }
                else if (entity.Texture != null)
                {
                    spriteBatch.Draw(entity.Texture, entity.Position, entity.SourceRect, Color.White);
                }
                else
                {
                    spriteBatch.Draw(defaultTexture, new Rectangle((int)entity.Position.X, (int)entity.Position.Y, 64, 64), Color.White);
                }
            }
        }

        public static void DrawUI(SpriteBatch spriteBatch)
        {
            foreach (var entity in Entities)
            {
                if (entity.IsDestroyed) continue;
                entity.Script?.DrawUI(spriteBatch);
            }
        }
    }
}
";
        }

        private static string GetGameStateCode()
        {
            return @"using System;
using System.Collections.Generic;

namespace MonoGameMaker.Runtime
{
    public static class GameState
    {
        public static Dictionary<string, object> Data = new Dictionary<string, object>();

        public static void Set<T>(string key, T value)
        {
            if (value == null)
            {
                Data.Remove(key);
            }
            else
            {
                Data[key] = value;
            }
        }

        public static T Get<T>(string key, T defaultValue = default)
        {
            if (Data.TryGetValue(key, out var val))
            {
                try
                {
                    return (T)Convert.ChangeType(val, typeof(T));
                }
                catch
                {
                    if (val is T typedVal)
                    {
                        return typedVal;
                    }
                }
            }
            return defaultValue;
        }
    }
}
";
        }

        private static string GetSceneManagerCode()
        {
            return @"using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;

namespace MonoGameMaker.Runtime
{
    public static class SceneManager
    {
        private static ContentManager _content;
        public static string CurrentSceneName { get; private set; } = string.Empty;
        public static RuntimeScene CurrentScene { get; private set; }

        public static void Initialize(ContentManager content)
        {
            _content = content;
            EntityManager.Content = content;
        }

        public static void LoadScene(string sceneName)
        {
            if (_content == null)
            {
                Console.WriteLine(""SceneManager error: Initialize with ContentManager before loading scenes."");
                return;
            }

            CurrentSceneName = sceneName;

            // Clear current entities
            EntityManager.Clear();

            // Handle scene name format
            string cleanSceneName = sceneName;
            if (cleanSceneName.EndsWith("".json"", StringComparison.OrdinalIgnoreCase))
            {
                cleanSceneName = cleanSceneName.Substring(0, cleanSceneName.Length - 5);
            }
            if (cleanSceneName.StartsWith(""Scenes/"", StringComparison.OrdinalIgnoreCase))
            {
                cleanSceneName = cleanSceneName.Substring(7);
            }

            string jsonPath = Path.Combine(""Scenes"", $""{cleanSceneName}.json"");

            // Load scene entities and assign to EntityManager
            CurrentScene = SceneLoader.LoadScene(jsonPath, _content);
            EntityManager.Entities = CurrentScene != null ? CurrentScene.Entities : new List<GameEntity>();
        }
    }
}
";
        }

        private static string GetCamera2DCode()
        {
            return @"using System;
using Microsoft.Xna.Framework;

namespace MonoGameMaker.Runtime
{
    public static class Camera2D
    {
        public static Vector2 Position { get; set; } = Vector2.Zero;
        
        public static Matrix Transform => Matrix.CreateTranslation(-Position.X, -Position.Y, 0);

        public static void LookAt(Vector2 target, int viewportWidth, int viewportHeight)
        {
            float targetX = target.X - (viewportWidth / 2f);
            float targetY = target.Y - (viewportHeight / 2f);

            int sceneWidth = viewportWidth;
            int sceneHeight = viewportHeight;

            if (SceneManager.CurrentScene != null)
            {
                sceneWidth = SceneManager.CurrentScene.Width;
                sceneHeight = SceneManager.CurrentScene.Height;
            }

            float minX = 0f;
            float maxX = Math.Max(0f, sceneWidth - viewportWidth);
            float minY = 0f;
            float maxY = Math.Max(0f, sceneHeight - viewportHeight);

            Position = new Vector2(
                MathHelper.Clamp(targetX, minX, maxX),
                MathHelper.Clamp(targetY, minY, maxY)
            );
        }
    }
}
";
        }

        private static string GetGame1Code(string projectName)
        {
            return $@"using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.IO;
using MonoGameMaker.Runtime;

namespace {projectName}
{{
    public class Game1 : Game
    {{
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Texture2D _defaultTexture;

        public Game1()
        {{
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = ""Content"";
            IsMouseVisible = true;
            
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
        }}

        protected override void Initialize()
        {{
            base.Initialize();
        }}

        protected override void LoadContent()
        {{
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _defaultTexture = new Texture2D(GraphicsDevice, 1, 1);
            _defaultTexture.SetData(new[] {{ Color.Magenta }});

            SceneManager.Initialize(Content);
            SceneManager.LoadScene(""scene_init"");
        }}

        protected override void Update(GameTime gameTime)
        {{
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // Dynamically synchronize graphics backbuffer with loaded scene dimensions
            if (SceneManager.CurrentScene != null)
            {{
                if (_graphics.PreferredBackBufferWidth != SceneManager.CurrentScene.Width ||
                    _graphics.PreferredBackBufferHeight != SceneManager.CurrentScene.Height)
                {{
                    _graphics.PreferredBackBufferWidth = SceneManager.CurrentScene.Width;
                    _graphics.PreferredBackBufferHeight = SceneManager.CurrentScene.Height;
                    _graphics.ApplyChanges();
                }}
            }}

            EntityManager.Update(gameTime);

            base.Update(gameTime);
        }}

        protected override void Draw(GameTime gameTime)
        {{
            GraphicsDevice.Clear(SceneManager.CurrentScene?.BackgroundColor ?? Color.Black);

            // Draw World Space entities transformed by the Camera
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Camera2D.Transform);
            
            if (SceneManager.CurrentScene != null && SceneManager.CurrentScene.BackgroundImage != null)
            {{
                _spriteBatch.Draw(SceneManager.CurrentScene.BackgroundImage, new Rectangle(0, 0, SceneManager.CurrentScene.Width, SceneManager.CurrentScene.Height), Color.White);
            }}
            
            EntityManager.Draw(_spriteBatch, _defaultTexture);
            _spriteBatch.End();

            // Draw Screen Space user interface components
            _spriteBatch.Begin();
            EntityManager.DrawUI(_spriteBatch);
            _spriteBatch.End();

            base.Draw(gameTime);
        }}
    }}
}}
";
        }

        private static string GetAiManifestTemplate(string projectName)
        {
            return $@"# AI Architecture Manifest & Rules - {projectName}

You are an AI assistant helping a developer build gameplay features in this project.
This project uses MonoGame DesktopGL (.NET 8.0) and is structured as a data-driven engine.
You must adhere to the following architecture rules and constraints at all times.

## Project Context
- **Framework**: MonoGame DesktopGL (.NET 8.0)
- **Architecture**: Data-Driven entity prefab system. All entities in scenes are instances of Prefabs.

## Folder Taxonomy
- `Content/Textures/`: Holds raw image assets (`.png`, `.jpg`, `.jpeg`). Never place gameplay configuration files here.
- `Prefabs/`: Holds JSON metadata files (with `.prefab` extension) containing `TextureName` (pointing to textures in Content/Textures), `ScriptName` (pointing to script classes in Scripts folder), and optional `Tag`.
- `Content/Scenes/`: Holds level/scene JSON layouts (specifically `scene_init.json`), which instance Prefabs by referencing their `prefabName` and specifying their `x` and `y` coordinates.
- `Scripts/`: Holds pure C# script behavior classes implementing the `MonoGameMaker.Runtime.IEntityScript` interface.

## Hard Architectural Rules
1. **NO Game1.cs Modifications**: Never modify `Game1.cs` to add custom gameplay logic or variables. It acts solely as the boilerplate engine bootstrap. All entity lifecycle operations must use `EntityManager`.
2. **NO Raw Texture Instancing**: Never instance a raw texture directly in `scene_init.json`. You MUST create a `.prefab` file inside the `Prefabs` directory that references the texture, and instance that prefab instead.
3. **MGCB Asset Registry**: To add or use any texture or audio asset, place it in the appropriate folder and make sure it is registered in `Content/Content.mgcb` so the MonoGame Content Builder compiler can compile it.
4. **Behavior Script Injection**: All gameplay behaviors must be implemented as scripts inheriting from `MonoGameMaker.Runtime.IEntityScript` inside the `Scripts/` folder, and then bound to an entity by entering the script class name in its `.prefab` file.
5. **Scene Transitions**: To dynamically change scenes (e.g. go from a Menu to a Level), call `MonoGameMaker.Runtime.SceneManager.LoadScene(""scene_name"")` (without directory prefix or .json extension).
6. **Global State Persistence**: Use `MonoGameMaker.Runtime.GameState.Data[""VariableName""]` or helper methods `GameState.Set(""key"", value)` / `GameState.Get<T>(""key"")` to store values like Score, Health, or Lives that must survive scene transitions.
7. **Camera Follow/LookAt**: To make the camera track the player or target, call `MonoGameMaker.Runtime.Camera2D.LookAt(playerPosition, viewportWidth, viewportHeight)` inside script Update loops.
8. **Drawing static HUD/UI**: To render static user interface components (e.g. Healthbar, Score, text) that do not scroll with the world, implement code exclusively inside the `DrawUI(SpriteBatch spriteBatch)` method of the script.
";
        }
    }
}
