using System;
using System.Runtime.InteropServices;
using ImGuiNET;
using System.IO;
using System.Threading.Tasks;
using MonoGameMaker.IDE.Core;
using MonoGameMaker.Runtime;

namespace MonoGameMaker.IDE.Windows
{
    public static class ProjectExplorer
    {
        private static string _creationTargetFolder = "";
        private static string _creationType = ""; // "Folder", "Script", "Prefab", "Scene"
        private static string _inputName = "";
        private static bool _showCreationPopup = false;

        private static string _deleteTargetPath = "";
        private static bool _showDeletePopup = false;

        public static void Draw()
        {
            ImGui.Begin("Project Explorer");

            if (string.IsNullOrEmpty(GlobalState.CurrentProjectPath))
            {
                ImGui.TextColored(new System.Numerics.Vector4(0.8f, 0.8f, 0.8f, 1f), "No project loaded.");
                ImGui.End();
                return;
            }

            if (AssetPipelineSynchronizer.IsProcessing)
            {
                ImGui.TextColored(new System.Numerics.Vector4(1.0f, 0.8f, 0.2f, 1f), "⚡ Compiling assets (MGCB)...");
                ImGui.Separator();
            }

            if (GlobalState.CurrentProjectCache == null)
            {
                ImGui.Text("Loading project tree...");
                ImGui.End();
                return;
            }

            FileTreeNode? root = GlobalState.CurrentProjectCache.GetSnapshot();
            if (root == null)
            {
                ImGui.Text("Initializing file system cache...");
                ImGui.End();
                return;
            }

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new System.Numerics.Vector2(4, 4));

            DrawNode(root);

            ImGui.PopStyleVar();

            // Render modal dialogs outside the recursive tree loop to prevent ImGui ID conflicts
            if (_showCreationPopup)
            {
                ImGui.OpenPopup("Create Item");
                _showCreationPopup = false;
            }

            if (_showDeletePopup)
            {
                ImGui.OpenPopup("Delete Confirmation");
                _showDeletePopup = false;
            }

            DrawCreationModal();
            DrawDeleteModal();

            ImGui.End();
        }

        private static void DrawNode(FileTreeNode node)
        {
            string relativePath = node.FullPath == GlobalState.CurrentProjectPath
                ? ""
                : Path.GetRelativePath(GlobalState.CurrentProjectPath!, node.FullPath).Replace("\\", "/");

            if (!node.IsDirectory)
            {
                ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
                if (GlobalState.SelectionContext.SelectedResourcePath == relativePath)
                {
                    flags |= ImGuiTreeNodeFlags.Selected;
                }

                ImGui.TreeNodeEx(node.Name, flags);

                // Right-click context menu for deleting files
                if (ImGui.BeginPopupContextItem($"FileContext##{relativePath}"))
                {
                    if (ImGui.MenuItem("Delete File"))
                    {
                        _deleteTargetPath = node.FullPath;
                        _showDeletePopup = true;
                    }
                    ImGui.EndPopup();
                }

                if (ImGui.BeginDragDropSource())
                {
                    byte[] pathBytes = System.Text.Encoding.UTF8.GetBytes(relativePath);
                    GCHandle handle = GCHandle.Alloc(pathBytes, GCHandleType.Pinned);
                    try
                    {
                        IntPtr ptr = handle.AddrOfPinnedObject();
                        ImGui.SetDragDropPayload("EXPLORER_ASSET", ptr, (uint)pathBytes.Length);
                    }
                    finally
                    {
                        handle.Free();
                    }
                    ImGui.Text(node.Name);
                    ImGui.EndDragDropSource();
                }
                
                if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                {
                    GlobalState.CommandManager.ExecuteCommand(new SelectResourceCommand(GlobalState.SelectionContext, relativePath));
                }

                string ext = Path.GetExtension(relativePath).ToLower();
                bool isEditable = ext == ".cs" || ext == ".json" || ext == ".spritefont";
                if (isEditable && ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    GlobalState.OpenResources.Add(relativePath);
                }
            }
            else
            {
                ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None;
                if (GlobalState.SelectionContext.SelectedResourcePath == relativePath)
                {
                    flags |= ImGuiTreeNodeFlags.Selected;
                }

                if (node.FullPath == GlobalState.CurrentProjectPath)
                {
                    flags |= ImGuiTreeNodeFlags.DefaultOpen;
                }

                bool opened = ImGui.TreeNodeEx(node.Name, flags);

                // Right-click context menu for folders
                if (ImGui.BeginPopupContextItem($"FolderContext##{relativePath}"))
                {
                    if (ImGui.MenuItem("New Folder"))
                    {
                        _creationTargetFolder = node.FullPath;
                        _creationType = "Folder";
                        _inputName = "NewFolder";
                        _showCreationPopup = true;
                    }

                    bool isScriptsFolder = relativePath.Equals("Scripts", StringComparison.OrdinalIgnoreCase) || 
                                           relativePath.StartsWith("Scripts/", StringComparison.OrdinalIgnoreCase);
                                           
                    bool isPrefabsFolder = relativePath.Equals("Prefabs", StringComparison.OrdinalIgnoreCase) || 
                                           relativePath.StartsWith("Prefabs/", StringComparison.OrdinalIgnoreCase);

                    bool isScenesFolder = relativePath.Equals("Content/Scenes", StringComparison.OrdinalIgnoreCase) || 
                                          relativePath.StartsWith("Content/Scenes/", StringComparison.OrdinalIgnoreCase);

                    bool isFontsFolder = relativePath.Equals("Content/Fonts", StringComparison.OrdinalIgnoreCase) || 
                                         relativePath.StartsWith("Content/Fonts/", StringComparison.OrdinalIgnoreCase);

                    if (isScriptsFolder)
                    {
                        if (ImGui.MenuItem("Create EntityBehavior Script"))
                        {
                            _creationTargetFolder = node.FullPath;
                            _creationType = "Script";
                            _inputName = "PlayerController";
                            _showCreationPopup = true;
                        }
                    }

                    if (isPrefabsFolder)
                    {
                        if (ToolEngine.IsPlaying)
                        {
                            ImGui.TextDisabled("Cannot create prefabs during simulation");
                        }
                        else if (ImGui.MenuItem("Create New Object Prefab"))
                        {
                            _creationTargetFolder = node.FullPath;
                            _creationType = "Prefab";
                            _inputName = "NewPrefab";
                            _showCreationPopup = true;
                        }
                    }

                    if (isScenesFolder)
                    {
                        if (ImGui.MenuItem("Create New Scene Layout"))
                        {
                            _creationTargetFolder = node.FullPath;
                            _creationType = "Scene";
                            _inputName = "level_1";
                            _showCreationPopup = true;
                        }
                    }

                    if (isFontsFolder)
                    {
                        if (ImGui.MenuItem("Create New SpriteFont"))
                        {
                            _creationTargetFolder = node.FullPath;
                            _creationType = "Font";
                            _inputName = "NewFont";
                            _showCreationPopup = true;
                        }
                    }

                    ImGui.EndPopup();
                }
                
                if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                {
                    GlobalState.CommandManager.ExecuteCommand(new SelectResourceCommand(GlobalState.SelectionContext, relativePath));
                }

                if (opened)
                {
                    foreach (var child in node.Children)
                    {
                        DrawNode(child);
                    }
                    ImGui.TreePop();
                }
            }
        }

        private static void DrawCreationModal()
        {
            bool open = true;
            if (ImGui.BeginPopupModal("Create Item", ref open, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text($"Create New {_creationType} under:\n{Path.GetFileName(_creationTargetFolder)}");
                ImGui.Separator();
                
                ImGui.Text("Name:");
                ImGui.InputText("##CreationNameInput", ref _inputName, 64);
                
                ImGui.Dummy(new System.Numerics.Vector2(0, 5));

                if (ImGui.Button("Create", new System.Numerics.Vector2(120, 0)))
                {
                    string name = _inputName.Trim();
                    if (string.IsNullOrEmpty(name))
                    {
                        GlobalState.Log("Error: Name cannot be empty.");
                    }
                    else
                    {
                        ExecuteCreation(name);
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
        }

        private static void DrawDeleteModal()
        {
            bool open = true;
            if (ImGui.BeginPopupModal("Delete Confirmation", ref open, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text($"Are you sure you want to delete this file?\n{Path.GetFileName(_deleteTargetPath)}");
                ImGui.Separator();
                
                ImGui.Dummy(new System.Numerics.Vector2(0, 5));

                if (ImGui.Button("Yes, Delete", new System.Numerics.Vector2(120, 0)))
                {
                    ExecuteDeletion();
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel", new System.Numerics.Vector2(80, 0)))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
        }

        private static void ExecuteCreation(string name)
        {
            try
            {
                string destPath = Path.Combine(_creationTargetFolder, name);
                
                switch (_creationType)
                {
                    case "Folder":
                        if (Directory.Exists(destPath))
                        {
                            GlobalState.Log($"Error: Folder '{name}' already exists.");
                        }
                        else
                        {
                            Directory.CreateDirectory(destPath);
                            GlobalState.Log($"Created folder: {name}");
                        }
                        break;

                    case "Script":
                        if (!name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                        {
                            name += ".cs";
                            destPath = Path.Combine(_creationTargetFolder, name);
                        }
                        if (File.Exists(destPath))
                        {
                            GlobalState.Log($"Error: Script '{name}' already exists.");
                        }
                        else
                        {
                            string className = Path.GetFileNameWithoutExtension(name);
                            string scriptContent = TemplateEngine.GetNewScriptTemplate(GlobalState.CurrentProjectName ?? "MyGame", className);
                            File.WriteAllText(destPath, scriptContent);
                            GlobalState.Log($"Created script: {name}");
                        }
                        break;

                    case "Prefab":
                        if (!name.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                        {
                            name += ".prefab";
                            destPath = Path.Combine(_creationTargetFolder, name);
                        }
                        if (File.Exists(destPath))
                        {
                            GlobalState.Log($"Error: Prefab '{name}' already exists.");
                        }
                        else
                        {
                            var defaultPrefab = new PrefabData();
                            PrefabSerializer.SavePrefab(destPath, defaultPrefab, GlobalState.Log);
                            GlobalState.Log($"Created prefab: {name}");
                        }
                        break;

                    case "Scene":
                        if (!name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                        {
                            name += ".json";
                            destPath = Path.Combine(_creationTargetFolder, name);
                        }
                        if (File.Exists(destPath))
                        {
                            GlobalState.Log($"Error: Scene '{name}' already exists.");
                        }
                        else
                        {
                            string sceneJson = "{\n  \"Width\": 1280,\n  \"Height\": 720,\n  \"BackgroundColor\": {\n    \"X\": 0.1,\n    \"Y\": 0.1,\n    \"Z\": 0.2\n  },\n  \"BackgroundImage\": \"\",\n  \"Instances\": []\n}";
                            File.WriteAllText(destPath, sceneJson);
                            GlobalState.Log($"Created scene layout: {name}");
                        }
                        break;

                    case "Font":
                        if (!name.EndsWith(".spritefont", StringComparison.OrdinalIgnoreCase))
                        {
                            name += ".spritefont";
                            destPath = Path.Combine(_creationTargetFolder, name);
                        }
                        if (File.Exists(destPath))
                        {
                            GlobalState.Log($"Error: Font file '{name}' already exists.");
                        }
                        else
                        {
                            string fontXml = TemplateEngine.GetDefaultSpriteFontCode();
                            File.WriteAllText(destPath, fontXml);
                            GlobalState.Log($"Created spritefont file: {name}");
                            
                            // Trigger background compilation and registration
                            _ = Task.Run(async () =>
                            {
                                bool success = await AssetPipelineSynchronizer.RegisterAsset(GlobalState.CurrentProjectPath!, destPath, "Fonts", GlobalState.Log);
                                if (success)
                                {
                                    GlobalState.Log($"Successfully compiled spritefont: {Path.GetFileNameWithoutExtension(destPath)}");
                                }
                            });
                        }
                        break;
                }
                
                GlobalState.CurrentProjectCache?.ForceRebuild();
            }
            catch (Exception ex)
            {
                GlobalState.Log($"Error creating asset: {ex.Message}");
            }
        }

        private static void ExecuteDeletion()
        {
            try
            {
                if (File.Exists(_deleteTargetPath))
                {
                    string relativeToProject = Path.GetRelativePath(GlobalState.CurrentProjectPath!, _deleteTargetPath).Replace("\\", "/");
                    if (relativeToProject.StartsWith("Content/", StringComparison.OrdinalIgnoreCase))
                    {
                        string contentRelativePath = Path.GetRelativePath(Path.Combine(GlobalState.CurrentProjectPath!, "Content"), _deleteTargetPath).Replace("\\", "/");
                        _ = AssetPipelineSynchronizer.UnregisterAsset(GlobalState.CurrentProjectPath!, contentRelativePath, GlobalState.Log);
                    }
                    else
                    {
                        File.Delete(_deleteTargetPath);
                        GlobalState.Log($"Deleted file: {Path.GetFileName(_deleteTargetPath)}");
                        GlobalState.CurrentProjectCache?.ForceRebuild();
                    }
                }
            }
            catch (Exception ex)
            {
                GlobalState.Log($"Error deleting file: {ex.Message}");
            }
        }
    }
}
