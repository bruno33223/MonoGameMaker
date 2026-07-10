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
                // - Add None Update for Content/Rooms to copy to output directory
                logCallback("Updating csproj configuration for target framework and rooms...");
                string csprojContent = File.ReadAllText(csprojPath);
                
                // Replace target framework
                csprojContent = Regex.Replace(csprojContent, @"<TargetFramework>.*?</TargetFramework>", "<TargetFramework>net8.0</TargetFramework>");

                // Inject CopyToOutputDirectory directive for Rooms JSON files
                string roomsInclude = @"
  <ItemGroup>
    <None Update=""Content\Rooms\**\*.*"">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>";
                csprojContent = csprojContent.Replace("</Project>", roomsInclude);
                File.WriteAllText(csprojPath, csprojContent);

                // 4. Create base directories
                logCallback("Creating folder structure...");
                string contentPath = Path.Combine(targetDirectory, "Content");
                string spritesPath = Path.Combine(contentPath, "Sprites");
                string backgroundsPath = Path.Combine(contentPath, "Backgrounds");
                string soundsPath = Path.Combine(contentPath, "Sounds");
                string roomsPath = Path.Combine(contentPath, "Rooms");
                string scriptsPath = Path.Combine(targetDirectory, "Scripts");

                Directory.CreateDirectory(spritesPath);
                Directory.CreateDirectory(backgroundsPath);
                Directory.CreateDirectory(soundsPath);
                Directory.CreateDirectory(roomsPath);
                Directory.CreateDirectory(scriptsPath);

                // 5. Create default room_init.json
                string roomInitPath = Path.Combine(roomsPath, "room_init.json");
                if (!File.Exists(roomInitPath))
                {
                    File.WriteAllText(roomInitPath, "[]");
                }

                // 6. Inject Runtime/RoomLoader.cs
                logCallback("Injecting RoomLoader runtime script...");
                string runtimeDir = Path.Combine(targetDirectory, "Runtime");
                Directory.CreateDirectory(runtimeDir);
                string roomLoaderPath = Path.Combine(runtimeDir, "RoomLoader.cs");
                File.WriteAllText(roomLoaderPath, GetRoomLoaderCode());

                // 7. Inject Game1.cs
                logCallback("Injecting customized Game1 boilerplate...");
                string game1Path = Path.Combine(targetDirectory, "Game1.cs");
                File.WriteAllText(game1Path, GetGame1Code(projectName));

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

        private static string GetRoomLoaderCode()
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
    public static class RoomLoader
    {
        public static List<GameEntity> LoadRoom(string jsonPath, ContentManager content)
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
                    var instances = JsonSerializer.Deserialize<List<RoomInstance>>(jsonContent);
                    if (instances != null)
                    {
                        foreach (var inst in instances)
                        {
                            Texture2D texture = null;
                            try
                            {
                                string assetPath = ""Sprites/"" + inst.spriteName;
                                if (assetPath.EndsWith("".png"") || assetPath.EndsWith("".jpg"") || assetPath.EndsWith("".jpeg""))
                                {
                                    assetPath = assetPath.Substring(0, assetPath.LastIndexOf('.'));
                                }
                                texture = content.Load<Texture2D>(assetPath);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($""Error loading texture {inst.spriteName}: {ex.Message}"");
                            }

                            entities.Add(new GameEntity
                            {
                                SpriteName = inst.spriteName,
                                Texture = texture,
                                Position = new Vector2(inst.x, inst.y)
                            });
                        }
                    }
                }
                else
                {
                    Console.WriteLine($""Room config file not found: {jsonPath}"");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($""Error parsing room: {ex.Message}"");
            }
            return entities;
        }
    }

    public class RoomInstance
    {
        public string spriteName { get; set; } = string.Empty;
        public float x { get; set; }
        public float y { get; set; }
    }

    public class GameEntity
    {
        public string SpriteName { get; set; } = string.Empty;
        public Texture2D Texture { get; set; }
        public Vector2 Position { get; set; }
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
            
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 600;
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

            string jsonPath = Path.Combine(""Rooms"", ""room_init.json"");
            _entities = RoomLoader.LoadRoom(jsonPath, Content);
        }}

        protected override void Update(GameTime gameTime)
        {{
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            base.Update(gameTime);
        }}

        protected override void Draw(GameTime gameTime)
        {{
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin();

            foreach (var entity in _entities)
            {{
                if (entity.Texture != null)
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
    }
}
