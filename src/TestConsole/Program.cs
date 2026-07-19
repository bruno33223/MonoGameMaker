using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MonoGameMaker.IDE.Core;
using MonoGameMaker.Runtime;

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
            bool registerSuccess = AssetPipelineSynchronizer.RegisterAsset(projectDir, mockSpritePath, "Textures", Console.WriteLine).GetAwaiter().GetResult();
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
                Tag = "Player",
                CustomProperties = new Dictionary<string, string>
                {
                    { "Speed", "150" },
                    { "Health", "100" }
                }
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
                    new SceneSerializer.EntityInstance 
                    { 
                        prefabName = "mock_prefab", 
                        x = 150, 
                        y = 200,
                        CustomProperties = new Dictionary<string, string> { { "Speed", "300" } }
                    },
                    new SceneSerializer.EntityInstance 
                    { 
                        prefabName = "mock_prefab", 
                        x = 400, 
                        y = 350 
                    }
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

            // 5. Test ProjectMigrator
            Console.WriteLine("\n[TEST 5] Testing ProjectMigrator legacy conversion...");
            
            // Downgrade csproj: remove unsafe blocks, copy local assemblies, and ImGui references
            string csprojText = File.ReadAllText(csprojPath);
            csprojText = csprojText.Replace("<AllowUnsafeBlocks>true</AllowUnsafeBlocks>", "");
            csprojText = csprojText.Replace("<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>", "");
            // Regex remove package references for ImGui.NET
            csprojText = System.Text.RegularExpressions.Regex.Replace(csprojText, @"<PackageReference\s+Include=""ImGui\.NET""[^>]*/>", "");
            File.WriteAllText(csprojPath, csprojText);
            Console.WriteLine("Downgraded csproj for migration test.");

            // Create legacy script
            string legacyScriptDir = Path.Combine(projectDir, "Scripts");
            Directory.CreateDirectory(legacyScriptDir);
            string legacyScriptPath = Path.Combine(legacyScriptDir, "LegacyPlayer.cs");
            string legacyScriptCode = @"using System;
using System.Collections.Generic;
using MonoGameMaker.Runtime;

namespace TestGame.Scripts
{
    public class LegacyPlayer : IEntityScript
    {
        public void Initialize(GameEntity targetEntity, Dictionary<string, string> customProperties)
        {
            targetEntity.Position = new Microsoft.Xna.Framework.Vector2(100, 150);
            string spd = customProperties[""Speed""];
        }
    }
}";
            File.WriteAllText(legacyScriptPath, legacyScriptCode);
            Console.WriteLine("Created legacy script LegacyPlayer.cs.");

            // Execute migrator
            MonoGameMaker.IDE.ProjectMigrator.Migrate(projectDir, Console.WriteLine);

            // Assertions
            string migratedCsprojText = File.ReadAllText(csprojPath);
            if (!migratedCsprojText.Contains("<AllowUnsafeBlocks>true</AllowUnsafeBlocks>") ||
                !migratedCsprojText.Contains("<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>") ||
                !migratedCsprojText.Contains("PackageReference Include=\"ImGui.NET\""))
            {
                Console.WriteLine("TEST FAILED: ProjectMigrator did not correctly restore csproj configuration.");
                Environment.Exit(1);
            }
            Console.WriteLine("Assertion passed: csproj migrated successfully.");

            string migratedScriptText = File.ReadAllText(legacyScriptPath);
            if (!migratedScriptText.Contains(": EntityBehavior") ||
                migratedScriptText.Contains("IEntityScript") ||
                !migratedScriptText.Contains("public override void Awake()") ||
                migratedScriptText.Contains("Initialize") ||
                !migratedScriptText.Contains("Entity.Position") ||
                !migratedScriptText.Contains("Properties[\"Speed\"]"))
            {
                Console.WriteLine("TEST FAILED: ProjectMigrator script refactoring output is incorrect.");
                Console.WriteLine($"Migrated Script Content:\n{migratedScriptText}");
                Environment.Exit(1);
            }
            Console.WriteLine("Assertion passed: Script refactored successfully.");

            // Verify compiled output with migrated script
            Console.WriteLine("Rebuilding project post-migration...");
            using (var process = new System.Diagnostics.Process { StartInfo = processInfo })
            {
                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine("TEST PASSED: Migrated project built successfully post-refactoring!");
                }
                else
                {
                    Console.WriteLine(output);
                    Console.WriteLine($"TEST FAILED: Post-migration build failed. Error: {error}");
                    Environment.Exit(1);
                }
            }

            // 6. Test GameState JSON Persistence via reflection on the compiled assembly
            Console.WriteLine("\n[TEST 6] Verifying GameState Save/Load and type restoration via reflection...");
            try
            {
                string dllPath = Path.Combine(projectDir, "bin", "Debug", "net8.0", "TestGame.dll");
                var assembly = System.Reflection.Assembly.LoadFrom(dllPath);
                var gameStateType = assembly.GetType("MonoGameMaker.Runtime.GameState");
                if (gameStateType == null)
                {
                    Console.WriteLine("TEST FAILED: GameState type not found in compiled assembly.");
                    Environment.Exit(1);
                }

                var setMethod = gameStateType.GetMethod("Set");
                var getMethod = gameStateType.GetMethod("Get");
                var saveMethod = gameStateType.GetMethod("SaveToFile");
                var loadMethod = gameStateType.GetMethod("LoadFromFile");

                if (setMethod == null || getMethod == null || saveMethod == null || loadMethod == null)
                {
                    Console.WriteLine("TEST FAILED: GameState methods (Set/Get/SaveToFile/LoadFromFile) are missing.");
                    Environment.Exit(1);
                }

                // Generic Get<T> method resolution
                var getIntMethod = getMethod.MakeGenericMethod(typeof(int));
                var getStringMethod = getMethod.MakeGenericMethod(typeof(string));
                var getDoubleMethod = getMethod.MakeGenericMethod(typeof(double));
                var getBoolMethod = getMethod.MakeGenericMethod(typeof(bool));

                var setGenericInt = setMethod.MakeGenericMethod(typeof(int));
                var setGenericString = setMethod.MakeGenericMethod(typeof(string));
                var setGenericDouble = setMethod.MakeGenericMethod(typeof(double));
                var setGenericBool = setMethod.MakeGenericMethod(typeof(bool));

                // Set values
                setGenericInt.Invoke(null, new object[] { "Coins", 50 });
                setGenericString.Invoke(null, new object[] { "PlayerName", "Antigravity" });
                setGenericDouble.Invoke(null, new object[] { "HealthPercent", 0.75 });
                setGenericBool.Invoke(null, new object[] { "IsGameOver", false });

                // Save to file
                string testSaveFile = Path.Combine(testDir, "test_save_state.json");
                saveMethod.Invoke(null, new object[] { testSaveFile });
                Console.WriteLine($"Saved test state to: {testSaveFile}");

                if (!File.Exists(testSaveFile))
                {
                    Console.WriteLine("TEST FAILED: save file was not created on disk.");
                    Environment.Exit(1);
                }

                // Clear / Reset GameState for loading verification
                var clearMethod = gameStateType.GetMethod("Clear");
                clearMethod?.Invoke(null, null);

                // Load from file
                loadMethod.Invoke(null, new object[] { testSaveFile });

                // Retrieve and verify values
                int loadedCoins = (int)getIntMethod.Invoke(null, new object[] { "Coins", 0 });
                string loadedPlayerName = (string)getStringMethod.Invoke(null, new object[] { "PlayerName", "Default" });
                double loadedHealth = (double)getDoubleMethod.Invoke(null, new object[] { "HealthPercent", 0.0 });
                bool loadedIsGameOver = (bool)getBoolMethod.Invoke(null, new object[] { "IsGameOver", true });

                if (loadedCoins != 50 || loadedPlayerName != "Antigravity" || loadedHealth != 0.75 || loadedIsGameOver != false)
                {
                    Console.WriteLine($"TEST FAILED: Deserialization value mismatch! Coins: {loadedCoins}, Name: {loadedPlayerName}, Health: {loadedHealth}, GameOver: {loadedIsGameOver}");
                    Environment.Exit(1);
                }

                Console.WriteLine("TEST PASSED: GameState successfully persisted and restored all primitive types!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TEST FAILED: Error during GameState persistence reflection test: {ex.Message}\n{ex.StackTrace}");
                Environment.Exit(1);
            }

            // 7. Test Hot Reload memory leak prevention and script teardown (Scenario A & B)
            Console.WriteLine("\n[TEST 7] Testing Hot Reload memory leak prevention and script teardown...");
            try
            {
                string dllPath = Path.Combine(projectDir, "bin", "Debug", "net8.0", "TestGame.dll");
                
                WeakReference weakAlc;
                int instancesBeforePurge;
                RunReloadTestStep(dllPath, out weakAlc, out instancesBeforePurge, sceneData);
                
                for (int i = 0; i < 10; i++)
                {
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                    GC.WaitForPendingFinalizers();
                    if (!weakAlc.IsAlive)
                    {
                        break;
                    }
                }

                if (weakAlc.IsAlive)
                {
                    Console.WriteLine("TEST FAILED: Collectible AssemblyLoadContext was not garbage collected (memory leak detected).");
                    Environment.Exit(1);
                }
                
                Console.WriteLine("TEST PASSED: Dynamic ALC collected successfully. No memory leaks detected!");

                if (sceneData.Instances.Count != instancesBeforePurge)
                {
                    Console.WriteLine($"TEST FAILED: Primary structures (sceneData.Instances) were corrupted. Count before: {instancesBeforePurge}, after: {sceneData.Instances.Count}.");
                    Environment.Exit(1);
                }

                var newAlc = new CollectibleAssemblyLoadContext(dllPath);
                var newAsm = newAlc.LoadFromAssemblyPath(dllPath);
                var newEntityManagerType = newAsm.GetType("MonoGameMaker.Runtime.EntityManager");
                var newGameEntityType = newAsm.GetType("MonoGameMaker.Runtime.GameEntity");
                var newSpawnMethod = newEntityManagerType?.GetMethod("Spawn", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                
                if (newSpawnMethod != null && newGameEntityType != null)
                {
                    var paramTypes = newSpawnMethod.GetParameters();
                    var vector2Type = paramTypes[1].ParameterType;
                    var spawnPos = Activator.CreateInstance(vector2Type, new object[] { 100f, 100f });

                    var spawned = newSpawnMethod.Invoke(null, new object[] { "mock_prefab", spawnPos });
                    if (spawned == null)
                    {
                        Console.WriteLine("TEST FAILED: Spawning new entity after reload failed.");
                        Environment.Exit(1);
                    }
                    Console.WriteLine("TEST PASSED: Successfully spawned new entity post-reload (Cenario B confirmed).");
                }
                else
                {
                    Console.WriteLine("TEST FAILED: Could not resolve Spawn method post-reload.");
                    Environment.Exit(1);
                }
                
                newAlc.Unload();
                newAlc = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TEST FAILED: Exception in Test 7: {ex.Message}\n{ex.StackTrace}");
                Environment.Exit(1);
            }

            // 8. Test CommandManager Undo/Redo stack transitions (TEST 8)
            Console.WriteLine("\n[TEST 8] Testing CommandManager Undo/Redo...");
            try
            {
                var cmdManager = new MonoGameMaker.IDE.Core.CommandManager();
                int testValue = 0;

                var mockCmd1 = new MockIncrementCommand(val => testValue = val, () => testValue, 5);
                cmdManager.ExecuteCommand(mockCmd1);
                if (testValue != 5) throw new Exception($"Expected 5, found {testValue}");

                var mockCmd2 = new MockIncrementCommand(val => testValue = val, () => testValue, 10);
                cmdManager.ExecuteCommand(mockCmd2);
                if (testValue != 10) throw new Exception($"Expected 10, found {testValue}");

                var mockCmd3 = new MockIncrementCommand(val => testValue = val, () => testValue, 15);
                cmdManager.ExecuteCommand(mockCmd3);
                if (testValue != 15) throw new Exception($"Expected 15, found {testValue}");

                cmdManager.Undo();
                if (testValue != 10) throw new Exception($"Expected 10 after first Undo, found {testValue}");

                cmdManager.Undo();
                if (testValue != 5) throw new Exception($"Expected 5 after second Undo, found {testValue}");

                cmdManager.Redo();
                if (testValue != 10) throw new Exception($"Expected 10 after Redo, found {testValue}");

                Console.WriteLine("TEST PASSED: CommandManager Undo/Redo sequence verified successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TEST FAILED: Exception in Test 8: {ex.Message}\n{ex.StackTrace}");
                Environment.Exit(1);
            }

            // 9. Test SelectionContext reactivity and events (TEST 9)
            Console.WriteLine("\n[TEST 9] Testing SelectionContext reactivity...");
            try
            {
                var selectionCtx = new MonoGameMaker.IDE.Core.SelectionContext();
                int fireCount = 0;
                object? lastSelectedObj = null;

                selectionCtx.OnSelectionChanged += obj =>
                {
                    fireCount++;
                    lastSelectedObj = obj;
                };

                var path = "Content/Textures/hero.png";
                var cmdPath = new MonoGameMaker.IDE.Core.SelectResourceCommand(selectionCtx, path);
                
                cmdPath.Execute();
                if (selectionCtx.SelectedResourcePath != path) throw new Exception($"Expected SelectedResourcePath to be {path}, found {selectionCtx.SelectedResourcePath}");
                if (fireCount != 1) throw new Exception($"Expected fireCount to be 1, found {fireCount}");
                if (lastSelectedObj as string != path) throw new Exception("Expected lastSelectedObj to be path string");

                var mockNode = new SceneSerializer.EntityInstance { prefabName = "TestNode", x = 10, y = 20 };
                var cmdNode = new MonoGameMaker.IDE.Core.SelectNodeCommand(selectionCtx, mockNode);
                
                cmdNode.Execute();
                if (selectionCtx.SelectedNode != mockNode) throw new Exception("Expected SelectedNode to be mockNode");
                if (fireCount != 2) throw new Exception($"Expected fireCount to be 2, found {fireCount}");
                if (lastSelectedObj != mockNode) throw new Exception("Expected lastSelectedObj to be mockNode");

                Console.WriteLine("TEST PASSED: SelectionContext selection updates and event dispatching verified successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TEST FAILED: Exception in Test 9: {ex.Message}\n{ex.StackTrace}");
                Environment.Exit(1);
            }

            Console.WriteLine("\n=== ALL INTEGRATION TESTS PASSED SUCCESSFULLY! ===");
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void RunReloadTestStep(string dllPath, out WeakReference weakAlc, out int instancesBeforePurge, SceneSerializer.SceneData sceneData)
        {
            var alc = new CollectibleAssemblyLoadContext(dllPath);
            var loadedAsm = alc.LoadFromAssemblyPath(dllPath);
            weakAlc = new WeakReference(alc);
            
            var entityManagerType = loadedAsm.GetType("MonoGameMaker.Runtime.EntityManager");
            var gameEntityType = loadedAsm.GetType("MonoGameMaker.Runtime.GameEntity");
            
            if (entityManagerType == null || gameEntityType == null)
            {
                throw new Exception("Could not resolve EntityManager or GameEntity in dynamically loaded assembly.");
            }

            var entity = Activator.CreateInstance(gameEntityType);
            
            var scriptType = loadedAsm.GetType("TestGame.Scripts.LegacyPlayer");
            if (scriptType == null)
            {
                var behaviorType = loadedAsm.GetType("MonoGameMaker.Runtime.EntityBehavior");
                if (behaviorType != null)
                {
                    foreach (var type in loadedAsm.GetTypes())
                    {
                        if (behaviorType.IsAssignableFrom(type) && !type.IsAbstract)
                        {
                            scriptType = type;
                            break;
                        }
                    }
                }
            }

            if (scriptType == null)
            {
                throw new Exception("Could not find any EntityBehavior script in dynamically loaded assembly.");
            }

            var scriptInst = Activator.CreateInstance(scriptType);
            gameEntityType.GetProperty("Script")?.SetValue(entity, scriptInst);
            scriptType.GetProperty("Entity")?.SetValue(scriptInst, entity);

            var entitiesField = entityManagerType.GetField("Entities", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var targetList = (System.Collections.IList?)entitiesField?.GetValue(null);
            if (targetList == null)
            {
                throw new Exception("EntityManager.Entities field is null.");
            }
            targetList.Add(entity);
            
            if (targetList.Count != 1)
            {
                throw new Exception($"Expected 1 entity in EntityManager, found {targetList.Count}.");
            }

            instancesBeforePurge = sceneData.Instances.Count;

            var purgeMethod = entityManagerType.GetMethod("PurgeAllScripts", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (purgeMethod == null)
            {
                throw new Exception("PurgeAllScripts method not found in EntityManager.");
            }
            purgeMethod.Invoke(null, null);
            
            if (targetList.Count != 0)
            {
                throw new Exception($"EntityManager.Entities was not cleared. Count: {targetList.Count}.");
            }

            alc.Unload();
        }

        private class MockIncrementCommand : MonoGameMaker.IDE.Core.IEditorCommand
        {
            private readonly Action<int> _setter;
            private readonly Func<int> _getter;
            private readonly int _newValue;
            private readonly int _oldValue;

            public MockIncrementCommand(Action<int> setter, Func<int> getter, int newValue)
            {
                _setter = setter;
                _getter = getter;
                _newValue = newValue;
                _oldValue = getter();
            }

            public void Execute() => _setter(_newValue);
            public void Undo() => _setter(_oldValue);
        }
    }
}
