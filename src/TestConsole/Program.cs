using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MonoGameMaker.IDE.Core;

namespace TestConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== MONO GAMEMAKER MVP INTEGRATION TEST ===");

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string testDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "test-outputs"));
            string projectDir = Path.Combine(testDir, "TestGame");

            // Clean previous test outputs
            if (Directory.Exists(testDir))
            {
                try
                {
                    Directory.Delete(testDir, true);
                    Console.WriteLine("Cleaned previous test-outputs directory.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: could not delete previous test directory: {ex.Message}");
                }
            }

            Directory.CreateDirectory(testDir);

            // 1. Scaffold project
            Console.WriteLine("\n[TEST 1] Scaffolding new MonoGame project...");
            bool scaffoldSuccess = await TemplateEngine.ScaffoldProjectAsync(projectDir, "TestGame", Console.WriteLine);
            if (!scaffoldSuccess)
            {
                Console.WriteLine("TEST FAILED: Scaffolding failed.");
                Environment.Exit(1);
            }

            // Verify folder structure
            string[] expectedDirs = new[] { "Content/Textures", "Content/Audio", "Content/Models", "Content/Scenes", "Scripts", "Runtime", "Prefabs" };
            foreach (var dir in expectedDirs)
            {
                string path = Path.Combine(projectDir, dir);
                if (!Directory.Exists(path))
                {
                    Console.WriteLine($"TEST FAILED: Folder '{dir}' was not created.");
                    Environment.Exit(1);
                }
            }

            // Verify AI architecture manifests
            string cursorrulesPath = Path.Combine(projectDir, ".cursorrules");
            string aiArchitecturePath = Path.Combine(projectDir, "AI_ARCHITECTURE.md");
            if (!File.Exists(cursorrulesPath) || !File.Exists(aiArchitecturePath))
            {
                Console.WriteLine("TEST FAILED: AI architecture manifests (.cursorrules or AI_ARCHITECTURE.md) were not created.");
                Environment.Exit(1);
            }

            // Check contents
            string cursorrulesContent = File.ReadAllText(cursorrulesPath);
            if (!cursorrulesContent.Contains("AI Architecture Manifest & Rules") || !cursorrulesContent.Contains("NO Game1.cs Modifications"))
            {
                Console.WriteLine("TEST FAILED: AI architecture manifest content is invalid or missing rules.");
                Environment.Exit(1);
            }

            Console.WriteLine("TEST PASSED: Folder structure and AI architecture manifests created successfully.");

            // 2. Create mock asset (2x2 red BMP renamed to png)
            Console.WriteLine("\n[TEST 2] Generating mock sprite image...");
            string mockSpritePath = Path.Combine(testDir, "mock_sprite.png");
            byte[] bmpData = new byte[] {
                0x42, 0x4D, // BM
                0x3E, 0x00, 0x00, 0x00, // file size (62 bytes)
                0x00, 0x00, 0x00, 0x00, // reserved
                0x36, 0x00, 0x00, 0x00, // offset to pixel data (54 bytes)
                0x28, 0x00, 0x00, 0x00, // header size (40 bytes)
                0x02, 0x00, 0x00, 0x00, // width (2 pixels)
                0x02, 0x00, 0x00, 0x00, // height (2 pixels)
                0x01, 0x00, // planes (1)
                0x18, 0x00, // bits per pixel (24 bit)
                0x00, 0x00, 0x00, 0x00, // compression (0)
                0x08, 0x00, 0x00, 0x00, // image size (8 bytes, padded)
                0x13, 0x0B, 0x00, 0x00, // horizontal res
                0x13, 0x0B, 0x00, 0x00, // vertical res
                0x00, 0x00, 0x00, 0x00, // colors in color table
                0x00, 0x00, 0x00, 0x00, // important colors
                // pixel data (B, G, R)
                0x00, 0x00, 0xFF,  0x00, 0x00, 0xFF,  0x00, 0x00,
                0x00, 0x00, 0xFF,  0x00, 0x00, 0xFF,  0x00, 0x00
            };
            File.WriteAllBytes(mockSpritePath, bmpData);

            // Register asset
            Console.WriteLine("Registering mock sprite in asset pipeline...");
            bool registerSuccess = AssetPipelineSynchronizer.RegisterAsset(projectDir, mockSpritePath, "Textures", Console.WriteLine);
            if (!registerSuccess)
            {
                Console.WriteLine("TEST FAILED: Asset pipeline registration failed.");
                Environment.Exit(1);
            }

            // Verify copied file and MGCB entries
            string destSpritePath = Path.Combine(projectDir, "Content", "Textures", "mock_sprite.png");
            if (!File.Exists(destSpritePath))
            {
                Console.WriteLine("TEST FAILED: Asset file was not copied physically.");
                Environment.Exit(1);
            }

            string mgcbPath = Path.Combine(projectDir, "Content", "Content.mgcb");
            string mgcbContent = File.ReadAllText(mgcbPath);
            if (!mgcbContent.Contains("Textures/mock_sprite.png"))
            {
                Console.WriteLine("TEST FAILED: MGCB registration entry not found in Content.mgcb.");
                Environment.Exit(1);
            }
            Console.WriteLine("TEST PASSED: Sprite registered and synchronized successfully.");

            // 3. Serialize Scene Configuration with Prefabs
            Console.WriteLine("\n[TEST 3] Generating mock prefab and scene_init.json...");
            string prefabsDir = Path.Combine(projectDir, "Prefabs");
            Directory.CreateDirectory(prefabsDir);

            string mockPrefabPath = Path.Combine(prefabsDir, "mock_prefab.prefab");
            var prefabData = new PrefabData
            {
                TextureName = "mock_sprite",
                ScriptName = "MockScript",
                Tag = "Player"
            };

            bool savePrefabSuccess = PrefabSerializer.SavePrefab(mockPrefabPath, prefabData, Console.WriteLine);
            if (!savePrefabSuccess)
            {
                Console.WriteLine("TEST FAILED: Prefab serialization failed.");
                Environment.Exit(1);
            }

            var sceneData = new SceneSerializer.SceneData
            {
                Width = 1280,
                Height = 720,
                BackgroundColor = new System.Numerics.Vector3(0.1f, 0.1f, 0.2f),
                Instances = new List<SceneSerializer.EntityInstance>
                {
                    new SceneSerializer.EntityInstance { prefabName = "mock_prefab", x = 150, y = 200 },
                    new SceneSerializer.EntityInstance { prefabName = "mock_prefab", x = 400, y = 350 }
                }
            };

            bool serializeSuccess = SceneSerializer.SaveScene(projectDir, sceneData, Console.WriteLine);
            if (!serializeSuccess)
            {
                Console.WriteLine("TEST FAILED: Scene serialization failed.");
                Environment.Exit(1);
            }

            string sceneJsonPath = Path.Combine(projectDir, "Content", "Scenes", "scene_init.json");
            if (!File.Exists(sceneJsonPath))
            {
                Console.WriteLine("TEST FAILED: scene_init.json file does not exist.");
                Environment.Exit(1);
            }

            string jsonContent = File.ReadAllText(sceneJsonPath);
            Console.WriteLine($"Serialized JSON content:\n{jsonContent}");
            Console.WriteLine("TEST PASSED: Scene serialized successfully.");

            // 4. Try building the generated project
            Console.WriteLine("\n[TEST 4] Compiling generated project to verify all boilerplate compiles...");
            string dotnetPath = TemplateEngine.GetDotnetPath();
            string csprojPath = Path.Combine(projectDir, "TestGame.csproj");

            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = dotnetPath,
                Arguments = $"build \"{csprojPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            TemplateEngine.ConfigureDotnetPath(processInfo);

            using (var process = new System.Diagnostics.Process { StartInfo = processInfo })
            {
                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine("TEST PASSED: Scaffolded MonoGame project built successfully!");
                }
                else
                {
                    Console.WriteLine(output);
                    Console.WriteLine($"TEST FAILED: Build failed with exit code {process.ExitCode}. Error: {error}");
                    Environment.Exit(1);
                }
            }

            Console.WriteLine("\n=== ALL INTEGRATION TESTS PASSED SUCCESSFULLY! ===");
        }
    }
}
