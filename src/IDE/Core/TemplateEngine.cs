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
                logCallback("Injecting SceneLoader and IEntityScript runtime scripts...");
                string runtimeDir = Path.Combine(targetDirectory, "Runtime");
                Directory.CreateDirectory(runtimeDir);
                
                string iEntityScriptPath = Path.Combine(runtimeDir, "IEntityScript.cs");
                File.WriteAllText(iEntityScriptPath, GetIEntityScriptCode());

                string sceneLoaderPath = Path.Combine(runtimeDir, "SceneLoader.cs");
                File.WriteAllText(sceneLoaderPath, GetSceneLoaderCode());

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

        private static string GetIEntityScriptCode()
        {
            return @"using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameMaker.Runtime
{
    public interface IEntityScript
    {
        void Update(GameTime gameTime);
        void Draw(SpriteBatch spriteBatch);
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
        public static List<GameEntity> LoadScene(string jsonPath, ContentManager content)
        {
            var entities = new List<GameEntity>();
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
                    
                    if (sceneData != null && sceneData.Instances != null)
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

                            if (File.Exists(prefabPath))
                            {
                                try
                                {
                                    string prefabJson = File.ReadAllText(prefabPath);
                                    var prefabData = JsonSerializer.Deserialize<PrefabData>(prefabJson);
                                    if (prefabData != null)
                                    {
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
                            IEntityScript scriptInstance = null;
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

                                    if (scriptType != null && typeof(IEntityScript).IsAssignableFrom(scriptType))
                                    {
                                        scriptInstance = (IEntityScript)Activator.CreateInstance(scriptType);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($""Error resolving script {scriptName}: {ex.Message}"");
                                }
                            }

                            entities.Add(new GameEntity
                            {
                                PrefabName = inst.prefabName,
                                Texture = texture,
                                Position = new Vector2(inst.x, inst.y),
                                Script = scriptInstance
                            });
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
            return entities;
        }
    }

    public class SceneData
    {
        public int Width { get; set; } = 1280;
        public int Height { get; set; } = 720;
        public System.Numerics.Vector3 BackgroundColor { get; set; }
        public string BackgroundImage { get; set; } = string.Empty;
        public List<EntityInstance> Instances { get; set; } = new List<EntityInstance>();
    }

    public class EntityInstance
    {
        public string prefabName { get; set; } = string.Empty;
        public float x { get; set; }
        public float y { get; set; }
    }

    public class PrefabData
    {
        public string TextureName { get; set; } = string.Empty;
        public string ScriptName { get; set; } = string.Empty;
        public string Tag { get; set; } = ""Default"";
    }

    public class GameEntity
    {
        public string PrefabName { get; set; } = string.Empty;
        public Texture2D Texture { get; set; }
        public Vector2 Position { get; set; }
        public IEntityScript Script { get; set; }
    }
}
";
        }

        private static string GetGame1Code(string projectName)
        {
            return $@"using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.IO;
using MonoGameMaker.Runtime;

namespace {projectName}
{{
    public class Game1 : Game
    {{
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private List<GameEntity> _entities = new List<GameEntity>();
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

            string jsonPath = Path.Combine(""Scenes"", ""scene_init.json"");
            _entities = SceneLoader.LoadScene(jsonPath, Content);
        }}

        protected override void Update(GameTime gameTime)
        {{
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            foreach (var entity in _entities)
            {{
                entity.Script?.Update(gameTime);
            }}

            base.Update(gameTime);
        }}

        protected override void Draw(GameTime gameTime)
        {{
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin();

            foreach (var entity in _entities)
            {{
                if (entity.Script != null)
                {{
                    entity.Script.Draw(_spriteBatch);
                }}
                else if (entity.Texture != null)
                {{
                    _spriteBatch.Draw(entity.Texture, entity.Position, Color.White);
                }}
                else
                {{
                    _spriteBatch.Draw(_defaultTexture, new Rectangle((int)entity.Position.X, (int)entity.Position.Y, 64, 64), Color.White);
                }}
            }}

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
1. **NO Game1.cs Modifications**: Never modify `Game1.cs` to add custom gameplay logic or variables. It acts solely as the boilerplate engine bootstrap.
2. **NO Raw Texture Instancing**: Never instance a raw texture directly in `scene_init.json`. You MUST create a `.prefab` file inside the `Prefabs` directory that references the texture, and instance that prefab instead.
3. **MGCB Asset Registry**: To add or use any texture or audio asset, place it in the appropriate folder and make sure it is registered in `Content/Content.mgcb` so the MonoGame Content Builder compiler can compile it.
4. **Behavior Script Injection**: All gameplay behaviors must be implemented as scripts inheriting from `MonoGameMaker.Runtime.IEntityScript` inside the `Scripts/` folder, and then bound to an entity by entering the script class name in its `.prefab` file.
";
        }
    }
}
