using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ImGuiNET;
using MonoGameMaker.IDE.Core;
using MonoGameMaker.IDE.Windows;
using MonoGameMaker.Runtime;

namespace MonoGameMaker.IDE
{
    public class ToolEngine : Game
    {
        private GraphicsDeviceManager _graphics;
        private ImGuiRenderer _imGuiRenderer = null!;

        // Popups states
        private bool _showNewProjectPopup;
        private bool _showOpenProjectPopup;

        // Simulation / Play State
        public static bool IsPlaying
        {
            get => GlobalState.IsPlaying;
            set => GlobalState.IsPlaying = value;
        }
        private bool _wasPlaying = false;
        private bool _isLayoutReady = false;
        private readonly InputManager _inputManager = new();

        // Layout States
        private bool _isFirstFrame = true;
        private bool _resetLayout;
        private int _layoutType;
        private int _customCopyLines = 50;

        // Form fields
        private string _newProjectName = "MyGame";
        private string _newProjectPath = "C:\\Temp";
        private string _openProjectPath = "C:\\Temp\\MyGame";
        private string _openProjectError = "";

        public ToolEngine()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // Set resizable and window dimensions
            Window.AllowUserResizing = true;
            _graphics.PreferredBackBufferWidth = 1200;
            _graphics.PreferredBackBufferHeight = 750;

            Exiting += (s, a) => 
            {
                StopRunningGame();
                GlobalState.CurrentProjectCache?.Dispose();
                TextureCache.UnloadAll();
            };
        }

        protected override void Initialize()
        {
            _imGuiRenderer = new ImGuiRenderer(this);
            _imGuiRenderer.RebuildFontAtlas();

            // Enable docking
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;

            GlobalState.GraphicsDevice = GraphicsDevice;
            
            var pixelTex = new Texture2D(GraphicsDevice, 1, 1);
            pixelTex.SetData(new[] { Microsoft.Xna.Framework.Color.White });
            GlobalState.PixelTexture = pixelTex;

            // Initialize preview texture cache
            TextureCache.Initialize(GraphicsDevice, _imGuiRenderer);

            // Subscribe to drag & drop events
            Window.FileDrop += OnFileDrop;

            GlobalState.Log("Tool Engine initialized.");
            base.Initialize();
        }

        private SceneSerializer.SceneData? GetActiveScene()
        {
            if (GlobalState.CurrentProjectPath == null || GlobalState.SelectedResourcePath == null) return null;
            if (GlobalState.SelectedResourcePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                string absolutePath = Path.Combine(GlobalState.CurrentProjectPath, GlobalState.SelectedResourcePath);
                return ResourceEditors.GetSceneData(absolutePath);
            }
            return null;
        }

        protected override void Update(GameTime gameTime)
        {
            _inputManager.Update();

            if (GlobalState.IsPlaying && !_wasPlaying)
            {
                // Transition to play: compile and load DLL first
                bool buildSuccess = AssemblyReloader.CompileAndLoad(GlobalState.CurrentProjectPath, GlobalState.Log);
                if (!buildSuccess && AssemblyReloader.LoadedAssembly == null)
                {
                    GlobalState.Log("Error: Could not start simulation because compilation failed and no previous assembly was loaded.");
                    GlobalState.IsPlaying = false;
                    _wasPlaying = false;
                }
                else
                {
                    var activeScene = GetActiveScene();
                    if (activeScene != null)
                    {
                        GlobalState.Log("Simulating Active Scene (Play Mode)");
                        GlobalState.SimEntities.Clear();

                        // 1. Resolve target project's EntityManager from loaded assembly
                        Type? entityManagerType = AssemblyReloader.LoadedAssembly?.GetType("MonoGameMaker.Runtime.EntityManager");
                        if (entityManagerType != null)
                        {
                            var clearMethod = entityManagerType.GetMethod("Clear", BindingFlags.Public | BindingFlags.Static);
                            clearMethod?.Invoke(null, null);

                            var entitiesField = entityManagerType.GetField("Entities", BindingFlags.Public | BindingFlags.Static);
                            if (entitiesField != null)
                            {
                                var targetList = (System.Collections.IList?)entitiesField.GetValue(null);

                                foreach (var inst in activeScene.Instances)
                                {
                                    // Resolve prefab data
                                    string prefabPath = Path.Combine(GlobalState.CurrentProjectPath!, "Prefabs", $"{inst.prefabName}.prefab");
                                    PrefabData prefabData = new PrefabData();
                                    if (File.Exists(prefabPath))
                                    {
                                        try
                                        {
                                            string prefabJson = File.ReadAllText(prefabPath);
                                            var deserialized = System.Text.Json.JsonSerializer.Deserialize<PrefabData>(prefabJson);
                                            if (deserialized != null) prefabData = deserialized;
                                        }
                                        catch (Exception ex)
                                        {
                                            GlobalState.Log($"Error reading prefab {inst.prefabName}: {ex.Message}");
                                        }
                                    }

                                    // Resolve texture
                                    Texture2D? texture = null;
                                    if (!string.IsNullOrEmpty(prefabData.TextureName))
                                    {
                                        string texPath = Path.Combine(GlobalState.CurrentProjectPath!, "Content", "Textures", prefabData.TextureName + ".png");
                                        if (!File.Exists(texPath)) texPath = Path.Combine(GlobalState.CurrentProjectPath!, "Content", "Textures", prefabData.TextureName + ".jpg");
                                        if (!File.Exists(texPath)) texPath = Path.Combine(GlobalState.CurrentProjectPath!, "Content", "Textures", prefabData.TextureName + ".jpeg");
                                        texture = TextureCache.GetTexture(texPath);
                                    }

                                    // Resolve script type using Reflection over AssemblyReloader.LoadedAssembly
                                    object? scriptInstance = null;
                                    if (!string.IsNullOrEmpty(prefabData.ScriptName))
                                    {
                                        try
                                        {
                                            Type? scriptType = AssemblyReloader.LoadedAssembly.GetType(prefabData.ScriptName);
                                            if (scriptType == null)
                                            {
                                                scriptType = AssemblyReloader.LoadedAssembly.GetType(AssemblyReloader.LoadedAssembly.GetName().Name + "." + prefabData.ScriptName);
                                            }
                                            if (scriptType == null)
                                            {
                                                scriptType = AssemblyReloader.LoadedAssembly.GetType(AssemblyReloader.LoadedAssembly.GetName().Name + ".Scripts." + prefabData.ScriptName);
                                            }

                                            if (scriptType != null)
                                            {
                                                Type? baseType = scriptType.BaseType;
                                                bool isBehavior = false;
                                                while (baseType != null)
                                                {
                                                    if (baseType.FullName == "MonoGameMaker.Runtime.EntityBehavior")
                                                    {
                                                        isBehavior = true;
                                                        break;
                                                    }
                                                    baseType = baseType.BaseType;
                                                }

                                                if (isBehavior)
                                                {
                                                    scriptInstance = Activator.CreateInstance(scriptType);
                                                }
                                                else
                                                {
                                                    GlobalState.Log($"Warning: Script type '{prefabData.ScriptName}' does not inherit from EntityBehavior.");
                                                }
                                            }
                                            else
                                            {
                                                GlobalState.Log($"Warning: Script type '{prefabData.ScriptName}' not found in assembly.");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            GlobalState.Log($"Error resolving script {prefabData.ScriptName}: {ex.Message}");
                                        }
                                    }

                                    // Merge properties
                                    var mergedProps = new Dictionary<string, string>();
                                    if (prefabData.CustomProperties != null)
                                    {
                                        foreach (var kv in prefabData.CustomProperties) mergedProps[kv.Key] = kv.Value;
                                    }
                                    if (inst.CustomProperties != null)
                                    {
                                        foreach (var kv in inst.CustomProperties) mergedProps[kv.Key] = kv.Value;
                                    }

                                    // Create GameEntity via reflection
                                    object? gameEntity = null;
                                    Type? gameEntityType = AssemblyReloader.LoadedAssembly.GetType("MonoGameMaker.Runtime.GameEntity");
                                    if (gameEntityType != null)
                                    {
                                        gameEntity = Activator.CreateInstance(gameEntityType);
                                        gameEntityType.GetProperty("PrefabName")?.SetValue(gameEntity, inst.prefabName);
                                        gameEntityType.GetProperty("Texture")?.SetValue(gameEntity, texture);
                                        gameEntityType.GetProperty("Position")?.SetValue(gameEntity, new Vector2(inst.x, inst.y));
                                        gameEntityType.GetProperty("Script")?.SetValue(gameEntity, scriptInstance);
                                        gameEntityType.GetProperty("Tag")?.SetValue(gameEntity, prefabData.Tag ?? "Default");
                                        gameEntityType.GetProperty("HitboxOffset")?.SetValue(gameEntity, new Vector2(prefabData.HitboxOffsetX, prefabData.HitboxOffsetY));
                                        gameEntityType.GetProperty("HitboxSize")?.SetValue(gameEntity, new Vector2(prefabData.HitboxWidth, prefabData.HitboxHeight));
                                    }

                                    // Initialize script
                                    if (scriptInstance != null && gameEntity != null)
                                    {
                                        try
                                        {
                                            scriptInstance.GetType().GetProperty("Entity")?.SetValue(scriptInstance, gameEntity);
                                            scriptInstance.GetType().GetProperty("Properties")?.SetValue(scriptInstance, mergedProps);
                                            scriptInstance.GetType().GetMethod("Awake")?.Invoke(scriptInstance, null);
                                        }
                                        catch (Exception ex)
                                        {
                                            GlobalState.Log($"Error calling script Awake on {inst.prefabName}: {ex.Message}");
                                        }
                                    }

                                    if (targetList != null)
                                    {
                                        targetList.Add(gameEntity);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Transition from play to edit: reload scene to restore positions
            if (!GlobalState.IsPlaying && _wasPlaying)
            {
                if (GlobalState.CurrentProjectPath != null && GlobalState.SelectedResourcePath != null)
                {
                    string absolutePath = Path.Combine(GlobalState.CurrentProjectPath, GlobalState.SelectedResourcePath);
                    ResourceEditors.ReloadScene(absolutePath);
                }
            }

            _wasPlaying = GlobalState.IsPlaying;

            if (GlobalState.IsPlaying)
            {
                // Run target project's EntityManager.Update via reflection
                Type? entityManagerType = AssemblyReloader.LoadedAssembly?.GetType("MonoGameMaker.Runtime.EntityManager");
                if (entityManagerType != null)
                {
                    var updateMethod = entityManagerType.GetMethod("Update", BindingFlags.Public | BindingFlags.Static);
                    if (updateMethod != null)
                    {
                        try
                        {
                            updateMethod.Invoke(null, new object[] { gameTime });
                        }
                        catch (Exception ex)
                        {
                            GlobalState.Log($"Error executing simulation loop: {ex.Message}");
                        }
                    }
                }
            }

            UpdateEditor(gameTime);
            base.Update(gameTime);
        }

        private void UpdateEditor(GameTime gameTime)
        {
            if (_isLayoutReady) return;

            // Process input events and begin ImGui frame
            _imGuiRenderer.BeforeLayout(gameTime);

            // Render Fullscreen Dockspace
            DrawDockspace();

            // Render Menu Bar Popups
            DrawPopups();

            // Render Subwindows
            ToolbarWindow();
            ProjectExplorer.Draw();
            ResourceEditors.DrawPropertiesWindow();
            ResourceEditors.DrawDocumentWindows();
            ConsoleLogsWindow();

            // End main dockspace wrapper window
            ImGui.End();

            _isLayoutReady = true;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(25, 25, 25));

            if (_isLayoutReady)
            {
                // Render ImGui to Screen
                _imGuiRenderer.AfterLayout();
                _isLayoutReady = false;
            }

            base.Draw(gameTime);
        }

        private void DrawDockspace()
        {
            ImGuiViewportPtr viewport = ImGui.GetMainViewport();
            ImGui.SetNextWindowPos(viewport.WorkPos);
            ImGui.SetNextWindowSize(viewport.WorkSize);
            ImGui.SetNextWindowViewport(viewport.ID);
            
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(0.0f, 0.0f));

            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoDocking;
            windowFlags |= ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
            windowFlags |= ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;

            ImGui.Begin("MainDockspaceWindow", windowFlags);
            ImGui.PopStyleVar(3);

            // Dockspace declaration
            uint dockspaceId = ImGui.GetID("MyDockSpace");
            ImGui.DockSpace(dockspaceId, new System.Numerics.Vector2(0.0f, 0.0f), ImGuiDockNodeFlags.None);

            if (_isFirstFrame)
            {
                _isFirstFrame = false;
                if (!File.Exists("imgui.ini"))
                {
                    _resetLayout = true;
                    _layoutType = 0;
                }
            }

            if (_resetLayout)
            {
                _resetLayout = false;
                
                igDockBuilderRemoveNode(dockspaceId);
                igDockBuilderAddNode(dockspaceId, 0);
                igDockBuilderSetNodeSize(dockspaceId, viewport.WorkSize);

                float toolbarHeight = 0.08f;
                float explorerWidth = 0.20f;
                float inspectorWidth = 0.25f;
                float consoleHeight = 0.30f;

                if (_layoutType == 1) // Wide Console
                {
                    explorerWidth = 0.15f;
                    inspectorWidth = 0.15f;
                    consoleHeight = 0.60f;
                }
                else if (_layoutType == 2) // Focused Editor
                {
                    explorerWidth = 0.12f;
                    inspectorWidth = 0.12f;
                    consoleHeight = 0.15f;
                }

                uint dock_id_top;
                uint dock_id_remaining;
                igDockBuilderSplitNode(dockspaceId, (int)ImGuiDir.Up, toolbarHeight, out dock_id_top, out dock_id_remaining);

                uint dock_id_left;
                uint dock_id_right_and_center;
                igDockBuilderSplitNode(dock_id_remaining, (int)ImGuiDir.Left, explorerWidth, out dock_id_left, out dock_id_right_and_center);

                uint dock_id_right;
                uint dock_id_center;
                igDockBuilderSplitNode(dock_id_right_and_center, (int)ImGuiDir.Right, inspectorWidth, out dock_id_right, out dock_id_center);

                uint dock_id_bottom;
                uint dock_id_center_top;
                igDockBuilderSplitNode(dock_id_center, (int)ImGuiDir.Down, consoleHeight, out dock_id_bottom, out dock_id_center_top);

                igDockBuilderDockWindow("Toolbar", dock_id_top);
                igDockBuilderDockWindow("Project Explorer", dock_id_left);
                igDockBuilderDockWindow("Properties", dock_id_right);
                igDockBuilderDockWindow("Console Output", dock_id_bottom);

                igDockBuilderFinish(dockspaceId);
            }

            // Menu Bar
            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("New Project"))
                    {
                        _newProjectPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MonoGameProjects");
                        _showNewProjectPopup = true;
                    }
                    if (ImGui.MenuItem("Open Project"))
                    {
                        _showOpenProjectPopup = true;
                    }
                    ImGui.Separator();
                    if (ImGui.MenuItem("Exit"))
                    {
                        Exit();
                    }
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("VIEW"))
                {
                    if (ImGui.MenuItem("Default Layout"))
                    {
                        _resetLayout = true;
                        _layoutType = 0;
                    }
                    if (ImGui.MenuItem("Wide Console Layout"))
                    {
                        _resetLayout = true;
                        _layoutType = 1;
                    }
                    if (ImGui.MenuItem("Focused Editor Layout"))
                    {
                        _resetLayout = true;
                        _layoutType = 2;
                    }
                    ImGui.Separator();
                    if (ImGui.MenuItem("Save Current Layout"))
                    {
                        ImGui.SaveIniSettingsToDisk("imgui.ini");
                        GlobalState.Log("Layout configuration saved to disk.");
                    }
                    ImGui.EndMenu();
                }

                ImGui.EndMenuBar();
            }
        }

        private void ToolbarWindow()
        {
            ImGui.Begin("Toolbar", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
            if (!string.IsNullOrEmpty(GlobalState.CurrentProjectPath))
            {
                if (!GlobalState.IsGameRunning)
                {
                    // Green Play Button
                    ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.18f, 0.64f, 0.31f, 1.0f));
                    if (ImGui.Button("▶ Play", new System.Numerics.Vector2(100, 0)))
                    {
                        _ = RunGameAsync();
                    }
                    ImGui.PopStyleColor();
                }
                else
                {
                    // Red Stop Button
                    ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.83f, 0.18f, 0.18f, 1.0f));
                    if (ImGui.Button("■ Stop", new System.Numerics.Vector2(100, 0)))
                    {
                        StopRunningGame();
                    }
                    ImGui.PopStyleColor();
                }

                ImGui.SameLine();

                // Simulation mode toggle
                if (IsPlaying)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.85f, 0.45f, 0.0f, 1.0f));
                    if (ImGui.Button("Pause Simulation", new System.Numerics.Vector2(140, 0)))
                    {
                        IsPlaying = false;
                        GlobalState.Log("Simulation paused. Editor updates resumed.");
                    }
                    ImGui.PopStyleColor();
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.18f, 0.45f, 0.85f, 1.0f));
                    if (ImGui.Button("Start Simulation", new System.Numerics.Vector2(140, 0)))
                    {
                        IsPlaying = true;
                        GlobalState.Log("Simulation started. Editor updates suspended.");
                    }
                    ImGui.PopStyleColor();
                }

                ImGui.SameLine();
                ImGui.Text($"Active Project: {GlobalState.CurrentProjectName} | Path: {GlobalState.CurrentProjectPath}");
            }
            else
            {
                ImGui.TextColored(new System.Numerics.Vector4(0f, 0.62f, 1f, 1f), "Create or Open a project under the 'File' menu to begin.");
            }
            ImGui.End();
        }

        private void ConsoleLogsWindow()
        {
            ImGui.Begin("Console Output");
            if (ImGui.Button("Clear Console"))
            {
                GlobalState.ClearLogs();
            }
            ImGui.SameLine();
            if (ImGui.Button("Copy Console"))
            {
                ImGui.OpenPopup("CopyConsolePopup");
            }

            if (ImGui.BeginPopup("CopyConsolePopup"))
            {
                if (ImGui.Selectable("Copy All"))
                {
                    CopyConsoleLines(-1);
                    ImGui.CloseCurrentPopup();
                }
                if (ImGui.Selectable("Copy Last 50 Lines"))
                {
                    CopyConsoleLines(50);
                    ImGui.CloseCurrentPopup();
                }
                if (ImGui.Selectable("Copy Last 100 Lines"))
                {
                    CopyConsoleLines(100);
                    ImGui.CloseCurrentPopup();
                }
                if (ImGui.Selectable("Copy Last 500 Lines"))
                {
                    CopyConsoleLines(500);
                    ImGui.CloseCurrentPopup();
                }
                
                ImGui.Separator();
                ImGui.Text("Custom number of lines:");
                ImGui.SetNextItemWidth(100);
                ImGui.InputInt("##CustomLinesInput", ref _customCopyLines);
                if (_customCopyLines < 1) _customCopyLines = 1;
                
                if (ImGui.Button("Copy Custom"))
                {
                    CopyConsoleLines(_customCopyLines);
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }

            ImGui.Separator();

            ImGui.BeginChild("LogScrollRegion", new System.Numerics.Vector2(0, 0), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar);
            lock (GlobalState.ConsoleLogs)
            {
                foreach (var log in GlobalState.ConsoleLogs)
                {
                    if (log.Contains("[GAME-STDERR]") || log.Contains("[ERROR]"))
                    {
                        ImGui.TextColored(new System.Numerics.Vector4(1.0f, 0.4f, 0.4f, 1.0f), log);
                    }
                    else if (log.Contains("[GAME-STDOUT]"))
                    {
                        ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.8f, 1.0f, 1.0f), log);
                    }
                    else if (log.Contains("Build succeeded") || log.Contains("successfully"))
                    {
                        ImGui.TextColored(new System.Numerics.Vector4(0.4f, 1.0f, 0.4f, 1.0f), log);
                    }
                    else
                    {
                        ImGui.TextUnformatted(log);
                    }
                }
            }

            // Keep scrolling down
            if (ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
            {
                ImGui.SetScrollHereY(1.0f);
            }
            ImGui.EndChild();
            ImGui.End();
        }

        private void CopyConsoleLines(int count)
        {
            lock (GlobalState.ConsoleLogs)
            {
                if (GlobalState.ConsoleLogs.Count == 0) return;
                
                var linesToCopy = new List<string>();
                if (count == -1 || count >= GlobalState.ConsoleLogs.Count)
                {
                    linesToCopy.AddRange(GlobalState.ConsoleLogs);
                }
                else
                {
                    int startIndex = GlobalState.ConsoleLogs.Count - count;
                    for (int i = startIndex; i < GlobalState.ConsoleLogs.Count; i++)
                    {
                        linesToCopy.Add(GlobalState.ConsoleLogs[i]);
                    }
                }
                
                string text = string.Join(Environment.NewLine, linesToCopy);
                ImGui.SetClipboardText(text);
            }
        }

        private void DrawPopups()
        {
            if (_showNewProjectPopup)
            {
                ImGui.OpenPopup("Create New Project");
                _showNewProjectPopup = false;
            }

            if (_showOpenProjectPopup)
            {
                ImGui.OpenPopup("Open Project");
                _showOpenProjectPopup = false;
            }

            // New Project Modal Window
            bool modalNew = true;
            if (ImGui.BeginPopupModal("Create New Project", ref modalNew, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.InputText("Project Name", ref _newProjectName, 64);
                ImGui.InputText("Folder Path", ref _newProjectPath, 512);

                if (ImGui.Button("Scaffold Project", new System.Numerics.Vector2(120, 0)))
                {
                    string name = _newProjectName.Trim();
                    string target = _newProjectPath.Trim();
                    
                    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(target))
                    {
                        GlobalState.Log("Error: Name and Target fields cannot be empty.");
                    }
                    else
                    {
                        string projectFolder = Path.Combine(target, name);
                        GlobalState.Log($"Starting scaffolding for {name} in {projectFolder}...");
                        
                        Task.Run(async () =>
                        {
                            bool success = await TemplateEngine.ScaffoldProjectAsync(projectFolder, name, GlobalState.Log);
                            if (success)
                            {
                                GlobalState.CurrentProjectPath = projectFolder;
                                GlobalState.CurrentProjectName = name;
                                GlobalState.OpenResources.Clear();
                                TextureCache.UnloadAll();
                                GlobalState.CurrentProjectCache?.Dispose();
                                GlobalState.CurrentProjectCache = new FileSystemCache(projectFolder, GlobalState.Log);
                                GlobalState.Log("Project scaffolded successfully!");
                            }
                            else
                            {
                                GlobalState.Log("Error scaffolding project.");
                            }
                        });
                        
                        ImGui.CloseCurrentPopup();
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel", new System.Numerics.Vector2(80, 0)))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }

            // Open Project Modal Window
            bool modalOpen = true;
            if (ImGui.BeginPopupModal("Open Project", ref modalOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text("Target Directory:");
                ImGui.InputText("##OpenProjectPath", ref _openProjectPath, 512);
                ImGui.SameLine();
                if (ImGui.Button("Browse..."))
                {
                    var result = NativeFileDialogSharp.Dialog.FolderPicker(Directory.Exists(_openProjectPath) ? _openProjectPath : null);
                    if (result.IsOk)
                    {
                        _openProjectPath = result.Path;
                        _openProjectError = "";
                    }
                }

                if (!string.IsNullOrEmpty(_openProjectError))
                {
                    ImGui.TextColored(new System.Numerics.Vector4(0.9f, 0.3f, 0.3f, 1f), _openProjectError);
                }

                ImGui.Dummy(new System.Numerics.Vector2(0, 5));

                if (ImGui.Button("Open", new System.Numerics.Vector2(100, 0)))
                {
                    string path = _openProjectPath.Trim();
                    if (Directory.Exists(path))
                    {
                        var csprojFiles = Directory.GetFiles(path, "*.csproj");
                        if (csprojFiles.Length > 0)
                        {
                            string name = Path.GetFileNameWithoutExtension(csprojFiles[0]);
                            GlobalState.CurrentProjectPath = path;
                            GlobalState.CurrentProjectName = name;
                            GlobalState.OpenResources.Clear();
                            TextureCache.UnloadAll();
                            GlobalState.CurrentProjectCache?.Dispose();
                            GlobalState.CurrentProjectCache = new FileSystemCache(path, GlobalState.Log);
                            GlobalState.Log($"Loaded project {name} from {path}");
                            _openProjectError = "";
                            ImGui.CloseCurrentPopup();
                        }
                        else
                        {
                            _openProjectError = "Error: No .csproj found in directory.";
                            GlobalState.Log(_openProjectError);
                        }
                    }
                    else
                    {
                        _openProjectError = "Error: Directory does not exist.";
                        GlobalState.Log(_openProjectError);
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel", new System.Numerics.Vector2(80, 0)))
                {
                    _openProjectError = "";
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
        }

        private async Task RunGameAsync()
        {
            if (string.IsNullOrEmpty(GlobalState.CurrentProjectPath) || string.IsNullOrEmpty(GlobalState.CurrentProjectName)) return;

            string dotnetPath = TemplateEngine.GetDotnetPath();
            string csprojPath = Path.Combine(GlobalState.CurrentProjectPath, $"{GlobalState.CurrentProjectName}.csproj");

            GlobalState.Log("Compiling project assets and binaries...");
            
            // Build process start
            bool buildSuccess = await Task.Run(() =>
            {
                var psi = new ProcessStartInfo
                {
                    FileName = dotnetPath,
                    Arguments = $"build \"{csprojPath}\" -p:CopyLocalLockFileAssemblies=true",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                TemplateEngine.ConfigureDotnetPath(psi);

                using (var process = new Process { StartInfo = psi })
                {
                    process.Start();
                    
                    while (!process.StandardOutput.EndOfStream)
                    {
                        string? line = process.StandardOutput.ReadLine();
                        if (line != null) GlobalState.Log(line);
                    }
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(error)) GlobalState.Log($"[ERROR] {error}");
                    return process.ExitCode == 0;
                }
            });

            if (!buildSuccess)
            {
                GlobalState.Log("Build FAILED. Review errors in console logs.");
                return;
            }

            GlobalState.Log("Build succeeded! Starting game window...");

            // Run process start
            string targetDll = Path.Combine(GlobalState.CurrentProjectPath, "bin", "Debug", "net8.0", $"{GlobalState.CurrentProjectName}.dll");
            string? workingDir = Path.GetDirectoryName(targetDll);

            var runPsi = new ProcessStartInfo
            {
                FileName = dotnetPath,
                Arguments = $"\"{targetDll}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.Combine(GlobalState.CurrentProjectPath, "bin", "Debug", "net8.0")
            };
            TemplateEngine.ConfigureDotnetPath(runPsi);

            // Clean environmental variables starting with DOTNET_ or COREHOST_ to isolate execution context
            var envsToRemove = new List<string>();
            foreach (string key in runPsi.EnvironmentVariables.Keys)
            {
                if (key.StartsWith("DOTNET_", StringComparison.OrdinalIgnoreCase) || 
                    key.StartsWith("COREHOST_", StringComparison.OrdinalIgnoreCase))
                {
                    envsToRemove.Add(key);
                }
            }
            foreach (var key in envsToRemove)
            {
                runPsi.EnvironmentVariables.Remove(key);
            }

            try
            {
                var runProcess = new Process { StartInfo = runPsi, EnableRaisingEvents = true };
                runProcess.OutputDataReceived += (s, ev) =>
                {
                    if (ev.Data != null) GlobalState.Log($"[GAME-STDOUT] {ev.Data}");
                };
                runProcess.ErrorDataReceived += (s, ev) =>
                {
                    if (ev.Data != null) GlobalState.Log($"[GAME-STDERR] {ev.Data}");
                };
                runProcess.Exited += (s, ev) =>
                {
                    GlobalState.Log("Game process exited.");
                    GlobalState.RunningGameProcess = null;
                };

                runProcess.Start();
                runProcess.BeginOutputReadLine();
                runProcess.BeginErrorReadLine();

                GlobalState.RunningGameProcess = runProcess;
                GlobalState.Log("Game process launched.");
            }
            catch (Exception ex)
            {
                GlobalState.Log($"Failed to run game process: {ex.Message}");
            }
        }

        private void StopRunningGame()
        {
            if (GlobalState.RunningGameProcess != null)
            {
                try
                {
                    if (!GlobalState.RunningGameProcess.HasExited)
                    {
                        GlobalState.Log("Stopping game process...");
                        GlobalState.RunningGameProcess.Kill(entireProcessTree: true);
                    }
                }
                catch (Exception ex)
                {
                    GlobalState.Log($"Error stopping game: {ex.Message}");
                }
            }
        }

        private void OnFileDrop(object? sender, FileDropEventArgs e)
        {
            if (IsPlaying)
            {
                GlobalState.Log("Error: Cannot import files while simulation is active.");
                return;
            }

            if (string.IsNullOrEmpty(GlobalState.CurrentProjectPath))
            {
                GlobalState.Log("Error: Open a project before dragging files in.");
                return;
            }

            if (e.Files == null || e.Files.Length == 0) return;

            foreach (var filePath in e.Files)
            {
                if (!File.Exists(filePath)) continue;

                string extension = Path.GetExtension(filePath).ToLower();
                string fileName = Path.GetFileName(filePath);

                try
                {
                    if (extension == ".png" || extension == ".jpg" || extension == ".jpeg")
                    {
                        _ = Task.Run(async () =>
                        {
                            bool success = await AssetPipelineSynchronizer.RegisterAsset(GlobalState.CurrentProjectPath, filePath, "Textures", GlobalState.Log);
                            if (success) GlobalState.Log($"Successfully imported texture: {fileName}");
                        });
                    }
                    else if (extension == ".wav" || extension == ".mp3")
                    {
                        _ = Task.Run(async () =>
                        {
                            bool success = await AssetPipelineSynchronizer.RegisterAsset(GlobalState.CurrentProjectPath, filePath, "Audio", GlobalState.Log);
                            if (success) GlobalState.Log($"Successfully imported audio: {fileName}");
                        });
                    }
                    else if (extension == ".fbx" || extension == ".obj")
                    {
                        _ = Task.Run(async () =>
                        {
                            bool success = await AssetPipelineSynchronizer.RegisterAsset(GlobalState.CurrentProjectPath, filePath, "Models", GlobalState.Log);
                            if (success) GlobalState.Log($"Successfully imported model: {fileName}");
                        });
                    }
                    else if (extension == ".cs")
                    {
                        string destDir = Path.Combine(GlobalState.CurrentProjectPath, "Scripts");
                        if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
                        string destPath = Path.Combine(destDir, fileName);
                        File.Copy(filePath, destPath, overwrite: true);
                        GlobalState.Log($"Successfully copied script: {fileName}");
                    }
                    else if (extension == ".json")
                    {
                        string destDir = Path.Combine(GlobalState.CurrentProjectPath, "Content", "Scenes");
                        if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
                        string destPath = Path.Combine(destDir, fileName);
                        File.Copy(filePath, destPath, overwrite: true);
                        GlobalState.Log($"Successfully copied scene layout: {fileName}");
                    }
                    else
                    {
                        GlobalState.Log($"Unsupported file type dropped: {extension}");
                    }
                }
                catch (Exception ex)
                {
                    GlobalState.Log($"Error importing dropped file {fileName}: {ex.Message}");
                }
            }
        }

        // --- Native cimgui DockBuilder P/Invoke Mappings ---
        [System.Runtime.InteropServices.DllImport("cimgui", CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        private static extern void igDockBuilderRemoveNode(uint node_id);

        [System.Runtime.InteropServices.DllImport("cimgui", CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        private static extern uint igDockBuilderAddNode(uint node_id, int flags);

        [System.Runtime.InteropServices.DllImport("cimgui", CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        private static extern void igDockBuilderSetNodeSize(uint node_id, System.Numerics.Vector2 size);

        [System.Runtime.InteropServices.DllImport("cimgui", CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        private static extern uint igDockBuilderSplitNode(uint node_id, int split_dir, float size_ratio_for_child_at_dir, out uint out_id_at_dir, out uint out_id_at_opposite_dir);

        [System.Runtime.InteropServices.DllImport("cimgui", CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        private static extern void igDockBuilderDockWindow([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStr)] string window_name, uint node_id);

        [System.Runtime.InteropServices.DllImport("cimgui", CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        private static extern void igDockBuilderFinish(uint node_id);
    }
}
