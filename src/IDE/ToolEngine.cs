using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ImGuiNET;
using MonoGameMaker.IDE.Core;
using MonoGameMaker.IDE.Windows;

namespace MonoGameMaker.IDE
{
    public class ToolEngine : Game
    {
        private GraphicsDeviceManager _graphics;
        private ImGuiRenderer _imGuiRenderer = null!;

        // Popups states
        private bool _showNewProjectPopup;
        private bool _showOpenProjectPopup;

        // Layout States
        private bool _isFirstFrame = true;
        private bool _resetLayout;
        private int _layoutType;

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

            // Initialize preview texture cache
            TextureCache.Initialize(GraphicsDevice, _imGuiRenderer);

            // Subscribe to drag & drop events
            Window.FileDrop += OnFileDrop;

            GlobalState.Log("Tool Engine initialized.");
            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(25, 25, 25));

            // Start ImGui Layout
            _imGuiRenderer.BeforeLayout(gameTime);

            // Render Fullscreen Dockspace
            DrawDockspace();

            // Render Menu Bar Popups
            DrawPopups();

            // Render Subwindows
            ToolbarWindow();
            ProjectExplorer.Draw();
            ResourceEditors.DrawAll();
            ConsoleLogsWindow();

            // End main dockspace wrapper window
            ImGui.End();

            // Render ImGui to Screen
            _imGuiRenderer.AfterLayout();

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
                igDockBuilderDockWindow("Inspector", dock_id_right);
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
                    Arguments = $"build \"{csprojPath}\"",
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
            var runPsi = new ProcessStartInfo
            {
                FileName = dotnetPath,
                Arguments = $"run --project \"{csprojPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = GlobalState.CurrentProjectPath
            };
            TemplateEngine.ConfigureDotnetPath(runPsi);

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
                        bool success = AssetPipelineSynchronizer.RegisterAsset(GlobalState.CurrentProjectPath, filePath, "Sprites", GlobalState.Log);
                        if (success) GlobalState.Log($"Successfully imported sprite: {fileName}");
                    }
                    else if (extension == ".wav" || extension == ".mp3")
                    {
                        bool success = AssetPipelineSynchronizer.RegisterAsset(GlobalState.CurrentProjectPath, filePath, "Sounds", GlobalState.Log);
                        if (success) GlobalState.Log($"Successfully imported sound: {fileName}");
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
                        string destDir = Path.Combine(GlobalState.CurrentProjectPath, "Content", "Rooms");
                        if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
                        string destPath = Path.Combine(destDir, fileName);
                        File.Copy(filePath, destPath, overwrite: true);
                        GlobalState.Log($"Successfully copied room layout: {fileName}");
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
