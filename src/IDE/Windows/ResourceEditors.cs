using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using ImGuiNET;
using ImGuiColorTextEditNet;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameMaker.IDE.Core;

namespace MonoGameMaker.IDE.Windows
{
    public static class ResourceEditors
    {
        private static readonly Dictionary<string, string> _importPaths = new();
        private static readonly Dictionary<string, TextEditor> _scriptEditors = new();
        
        private class SceneEditorState
        {
            public SceneSerializer.SceneData Scene = new SceneSerializer.SceneData();
            public int SelectedIndex = -1;
            public string InstPrefabName = "";
            public int InstX = 100;
            public int InstY = 100;

            public RenderTarget2D? RenderTarget;
            public IntPtr RenderTargetId = IntPtr.Zero;
            public SpriteBatch? SpriteBatch;
            public Texture2D? FallbackTexture;
        }

        private static readonly Dictionary<string, SceneEditorState> _sceneStates = new();
        
        private static string _newScriptName = "Script1";
        private static string _newPrefabName = "NewObject1";
        private static string _newPropKey = "";
        private static string _newPropValue = "";

        public static void DrawPropertiesWindow()
        {
            ImGui.Begin("Properties");

            if (string.IsNullOrEmpty(GlobalState.CurrentProjectPath))
            {
                ImGui.Text("No project loaded.");
                ImGui.End();
                return;
            }

            string? res = GlobalState.SelectedResourcePath;
            if (string.IsNullOrEmpty(res))
            {
                ImGui.TextColored(new System.Numerics.Vector4(0.6f, 0.6f, 0.6f, 1f), "Select a resource in the Project Explorer.");
                ImGui.End();
                return;
            }

            string absolutePath = Path.Combine(GlobalState.CurrentProjectPath, res);
            bool isDir = Directory.Exists(absolutePath);
            bool isFile = File.Exists(absolutePath);

            if (!isDir && !isFile)
            {
                ImGui.TextColored(new System.Numerics.Vector4(0.8f, 0.3f, 0.3f, 1f), $"Selected path does not exist on disk:\n{res}");
                ImGui.End();
                return;
            }

            if (isDir)
            {
                DrawDirectoryProperties(res, absolutePath);
            }
            else
            {
                if (res.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    DrawPrefabProperties(res, absolutePath);
                }
                else
                {
                    DrawFileProperties(res, absolutePath);
                }
            }

            ImGui.End();
        }

        public static void DrawDocumentWindows()
        {
            var resourcesToDraw = GlobalState.OpenResources.ToList();
            
            foreach (var res in resourcesToDraw)
            {
                string fileName = Path.GetFileName(res);
                if (string.IsNullOrEmpty(fileName)) fileName = res;

                bool isOpen = true;
                ImGui.Begin($"Document: {fileName}##{res}", ref isOpen);

                if (!isOpen)
                {
                    string absolutePath = Path.Combine(GlobalState.CurrentProjectPath!, res);
                    TextureCache.Unload(absolutePath);
                    
                    if (_scriptEditors.ContainsKey(absolutePath))
                    {
                        _scriptEditors.Remove(absolutePath);
                    }
                    
                    if (_sceneStates.TryGetValue(absolutePath, out var sState))
                    {
                        if (sState.RenderTargetId != IntPtr.Zero)
                        {
                            TextureCache.UnbindRenderTarget(sState.RenderTargetId);
                        }
                        sState.RenderTarget?.Dispose();
                        sState.FallbackTexture?.Dispose();
                        sState.SpriteBatch?.Dispose();
                        _sceneStates.Remove(absolutePath);
                    }

                    GlobalState.OpenResources.Remove(res);
                    ImGui.End();
                    continue;
                }

                DrawDocumentEditor(res);
                ImGui.End();
            }
        }

        private static void DrawDirectoryProperties(string relativePath, string absolutePath)
        {
            string folderName = Path.GetFileName(relativePath);
            ImGui.TextColored(new System.Numerics.Vector4(0f, 0.6f, 1f, 1f), $"Folder: {folderName}");
            ImGui.Separator();
            ImGui.Dummy(new System.Numerics.Vector2(0, 10));

            if (folderName == "Textures" || folderName == "Audio" || folderName == "Models")
            {
                ImGui.Text("Import New Asset");

                if (!_importPaths.TryGetValue(absolutePath, out var importPath))
                {
                    importPath = "";
                    _importPaths[absolutePath] = importPath;
                }

                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 100);
                if (ImGui.InputText("##ImportPath", ref importPath, 512))
                {
                    _importPaths[absolutePath] = importPath;
                }

                ImGui.SameLine();
                if (ImGui.Button("Browse...", new System.Numerics.Vector2(92, 0)))
                {
                    string filter = folderName == "Audio" ? "wav,mp3" : (folderName == "Models" ? "fbx,obj" : "png,jpg,jpeg");
                    var dialogResult = NativeFileDialogSharp.Dialog.FileOpen(filter);
                    if (dialogResult.IsOk)
                    {
                        importPath = dialogResult.Path;
                        _importPaths[absolutePath] = importPath;
                    }
                }

                ImGui.TextColored(new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 1f), "Enter absolute path or click 'Browse...'");
                ImGui.Dummy(new System.Numerics.Vector2(0, 5));

                if (ImGui.Button("Import Asset", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 30)))
                {
                    if (string.IsNullOrEmpty(importPath) || !File.Exists(importPath))
                    {
                        GlobalState.Log("Error: Specify a valid absolute path to import.");
                    }
                    else
                    {
                        bool success = AssetPipelineSynchronizer.RegisterAsset(GlobalState.CurrentProjectPath!, importPath, folderName, GlobalState.Log);
                        if (success)
                        {
                            GlobalState.Log($"Successfully imported asset into {folderName}.");
                            _importPaths[absolutePath] = ""; // Clear path
                        }
                    }
                }
            }
            else if (folderName == "Scripts")
            {
                ImGui.Text("Create New Script");
                ImGui.SetNextItemWidth(-1);
                ImGui.InputText("##NewScriptName", ref _newScriptName, 64);

                ImGui.Dummy(new System.Numerics.Vector2(0, 5));

                if (ImGui.Button("Create Script File", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 30)))
                {
                    string scriptName = _newScriptName.Trim();
                    if (string.IsNullOrEmpty(scriptName))
                    {
                        GlobalState.Log("Error: Script name cannot be empty.");
                    }
                    else
                    {
                        if (!scriptName.EndsWith(".cs")) scriptName += ".cs";
                        string destPath = Path.Combine(absolutePath, scriptName);
                        if (File.Exists(destPath))
                        {
                            GlobalState.Log($"Error: Script {scriptName} already exists.");
                        }
                        else
                        {
                            string cleanName = Path.GetFileNameWithoutExtension(scriptName);
                            string template = $@"using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameMaker.Runtime;

namespace {GlobalState.CurrentProjectName}.Scripts
{{
    public class {cleanName} : IEntityScript
    {{
        public void Initialize(GameEntity entity, System.Collections.Generic.Dictionary<string, string> properties)
        {{
            // Custom initialization behavior logic
        }}

        public void Update(GameTime gameTime)
        {{
            // Custom update behavior logic
        }}

        public void Draw(SpriteBatch spriteBatch)
        {{
            // Custom draw behavior logic (if needed, otherwise default texture draws)
        }}
    }}
}}
";
                            File.WriteAllText(destPath, template);
                            GlobalState.Log($"Scaffolded new script: {scriptName}");
                            _newScriptName = "Script_" + (Directory.GetFiles(absolutePath, "*.cs").Length + 1);
                        }
                    }
                }
            }
            else if (folderName == "Scenes")
            {
                ImGui.Text("Create New Scene Configuration");
                if (ImGui.Button("Create Default scene_init.json", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 30)))
                {
                    string dest = Path.Combine(absolutePath, "scene_init.json");
                    if (File.Exists(dest))
                    {
                        GlobalState.Log("Scene configuration already exists.");
                    }
                    else
                    {
                        var defaultScene = new SceneSerializer.SceneData();
                        bool success = SceneSerializer.SaveScenePath(dest, defaultScene, GlobalState.Log);
                        if (success)
                        {
                            GlobalState.Log("Created scene_init.json.");
                        }
                    }
                }
            }
            else if (folderName == "Prefabs")
            {
                ImGui.Text("Create New Prefab Object");
                ImGui.SetNextItemWidth(-1);
                ImGui.InputText("##NewPrefabName", ref _newPrefabName, 64);

                ImGui.Dummy(new System.Numerics.Vector2(0, 5));

                if (ImGui.Button("Create Prefab File", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 30)))
                {
                    string prefabName = _newPrefabName.Trim();
                    if (string.IsNullOrEmpty(prefabName))
                    {
                        GlobalState.Log("Error: Prefab name cannot be empty.");
                    }
                    else
                    {
                        if (!prefabName.EndsWith(".prefab")) prefabName += ".prefab";
                        string destPath = Path.Combine(absolutePath, prefabName);
                        if (File.Exists(destPath))
                        {
                            GlobalState.Log($"Error: Prefab {prefabName} already exists.");
                        }
                        else
                        {
                            var defaultPrefab = new PrefabData();
                            PrefabSerializer.SavePrefab(destPath, defaultPrefab, GlobalState.Log);
                            GlobalState.Log($"Created new prefab: {prefabName}");
                            _newPrefabName = "NewObject_" + (Directory.GetFiles(absolutePath, "*.prefab").Length + 1);
                        }
                    }
                }
            }
        }

        private static void DrawPrefabProperties(string relativePath, string absolutePath)
        {
            string fileName = Path.GetFileName(relativePath);
            ImGui.TextColored(new System.Numerics.Vector4(0f, 0.8f, 0.8f, 1f), $"Prefab: {fileName}");
            ImGui.Text($"Relative Path: {relativePath}");
            ImGui.Separator();
            ImGui.Dummy(new System.Numerics.Vector2(0, 5));

            var prefab = PrefabCache.GetPrefab(absolutePath);

            // Fetch available textures in project
            string texturesDir = Path.Combine(GlobalState.CurrentProjectPath!, "Content", "Textures");
            var availableTextures = new List<string>();
            if (Directory.Exists(texturesDir))
            {
                foreach (var file in Directory.GetFiles(texturesDir))
                {
                    availableTextures.Add(Path.GetFileNameWithoutExtension(file));
                }
            }

            // Texture Selection Combo
            string currentTex = prefab.TextureName;
            ImGui.Text("Texture Asset:");
            DrawBackgroundImageComboBox(availableTextures, ref currentTex, absolutePath);
            prefab.TextureName = currentTex;

            // Script Name Input
            string currentScript = prefab.ScriptName;
            ImGui.Text("Script Class Name:");
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputText("##PrefabScriptName", ref currentScript, 128))
            {
                prefab.ScriptName = currentScript;
            }

            // Tag Input
            string currentTag = prefab.Tag;
            ImGui.Text("Tag:");
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputText("##PrefabTag", ref currentTag, 64))
            {
                prefab.Tag = currentTag;
            }

            // Custom Properties Header
            if (ImGui.CollapsingHeader("Custom Properties"))
            {
                if (prefab.CustomProperties == null) prefab.CustomProperties = new Dictionary<string, string>();

                var keysToRemove = new List<string>();

                if (ImGui.BeginTable($"PrefabPropsTable_{absolutePath}", 3, ImGuiTableFlags.Resizable))
                {
                    ImGui.TableSetupColumn("Key", ImGuiTableColumnFlags.WidthStretch, 0.4f);
                    ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch, 0.5f);
                    ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed, 30f);

                    foreach (var kv in prefab.CustomProperties)
                    {
                        ImGui.TableNextRow();
                        
                        // Column 1: Key name
                        ImGui.TableSetColumnIndex(0);
                        ImGui.AlignTextToFramePadding();
                        ImGui.Text(kv.Key);

                        // Column 2: Value Input
                        ImGui.TableSetColumnIndex(1);
                        string val = kv.Value;
                        ImGui.SetNextItemWidth(-1);
                        if (ImGui.InputText($"##Val_{kv.Key}_{absolutePath}", ref val, 128))
                        {
                            prefab.CustomProperties[kv.Key] = val;
                        }

                        // Column 3: Delete action
                        ImGui.TableSetColumnIndex(2);
                        if (ImGui.Button($"X##Del_{kv.Key}_{absolutePath}", new System.Numerics.Vector2(-1, 0)))
                        {
                            keysToRemove.Add(kv.Key);
                        }
                    }
                    ImGui.EndTable();
                }

                foreach (var key in keysToRemove)
                {
                    prefab.CustomProperties.Remove(key);
                }

                ImGui.Text("Add Property:");
                
                ImGui.Text("Key:");
                ImGui.SetNextItemWidth(-1);
                ImGui.InputText($"##NewKey_{absolutePath}", ref _newPropKey, 64);
                
                ImGui.Text("Value:");
                ImGui.SetNextItemWidth(-1);
                ImGui.InputText($"##NewVal_{absolutePath}", ref _newPropValue, 128);
                
                ImGui.Dummy(new System.Numerics.Vector2(0, 2));
                if (ImGui.Button($"Add Property##Add_{absolutePath}", new System.Numerics.Vector2(-1, 30)))
                {
                    string k = _newPropKey.Trim();
                    if (!string.IsNullOrEmpty(k))
                    {
                        prefab.CustomProperties[k] = _newPropValue;
                        _newPropKey = "";
                        _newPropValue = "";
                    }
                }
            }

            ImGui.Dummy(new System.Numerics.Vector2(0, 10));

            // Texture Preview
            if (!string.IsNullOrEmpty(prefab.TextureName))
            {
                string texFilePath = Path.Combine(texturesDir, prefab.TextureName + ".png");
                if (!File.Exists(texFilePath)) texFilePath = Path.Combine(texturesDir, prefab.TextureName + ".jpg");
                if (!File.Exists(texFilePath)) texFilePath = Path.Combine(texturesDir, prefab.TextureName + ".jpeg");

                if (File.Exists(texFilePath))
                {
                    int imgW, imgH;
                    IntPtr imguiId = TextureCache.GetPreview(texFilePath, out imgW, out imgH);
                    if (imguiId != IntPtr.Zero)
                    {
                        float w = imgW;
                        float h = imgH;
                        float maxWidth = ImGui.GetContentRegionAvail().X;
                        if (maxWidth > 200) maxWidth = 200;
                        if (w > maxWidth)
                        {
                            float ratio = maxWidth / w;
                            w = maxWidth;
                            h = h * ratio;
                        }
                        ImGui.Text("Texture Preview:");
                        ImGui.Image(imguiId, new System.Numerics.Vector2(w, h));
                        ImGui.Dummy(new System.Numerics.Vector2(0, 10));
                    }
                }
                else
                {
                    ImGui.TextColored(new System.Numerics.Vector4(0.8f, 0.5f, 0f, 1f), "(Texture file not found in Content/Textures)");
                }
            }

            float spacing = ImGui.GetStyle().ItemSpacing.X;
            float halfWidth = (ImGui.GetContentRegionAvail().X - spacing) / 2;

            if (ImGui.Button("Save Prefab", new System.Numerics.Vector2(halfWidth, 30)))
            {
                bool success = PrefabSerializer.SavePrefab(absolutePath, prefab, GlobalState.Log);
                if (success)
                {
                    PrefabCache.Invalidate(absolutePath);
                    GlobalState.Log($"Saved changes to prefab {fileName}");
                }
            }

            ImGui.SameLine();
            if (ImGui.Button("Delete Prefab", new System.Numerics.Vector2(halfWidth, 30)))
            {
                try
                {
                    PrefabCache.Invalidate(absolutePath);
                    File.Delete(absolutePath);
                    GlobalState.SelectedResourcePath = null;
                    GlobalState.Log($"Deleted prefab file {fileName}.");
                }
                catch (Exception ex)
                {
                    GlobalState.Log($"Error deleting prefab: {ex.Message}");
                }
            }
        }

        private static void DrawFileProperties(string relativePath, string absolutePath)
        {
            string fileName = Path.GetFileName(relativePath);

            string ext = Path.GetExtension(relativePath).ToLower();
            bool isImage = ext == ".png" || ext == ".jpg" || ext == ".jpeg";

            if (isImage)
            {
                int imgW, imgH;
                IntPtr imguiId = TextureCache.GetPreview(absolutePath, out imgW, out imgH);
                if (imguiId != IntPtr.Zero)
                {
                    float w = imgW;
                    float h = imgH;
                    float maxWidth = ImGui.GetContentRegionAvail().X;
                    if (maxWidth > 300) maxWidth = 300;
                    if (w > maxWidth)
                    {
                        float ratio = maxWidth / w;
                        w = maxWidth;
                        h = h * ratio;
                    }
                    ImGui.Text("Texture Preview:");
                    ImGui.Image(imguiId, new System.Numerics.Vector2(w, h));
                    ImGui.Dummy(new System.Numerics.Vector2(0, 10));
                }
            }

            ImGui.TextColored(new System.Numerics.Vector4(0.8f, 0.8f, 0f, 1f), $"Resource: {fileName}");
            ImGui.Text($"Relative Path: {relativePath}");
            
            long size = new FileInfo(absolutePath).Length;
            ImGui.Text($"File Size: {size / 1024.0:F2} KB");
            ImGui.Separator();
            ImGui.Dummy(new System.Numerics.Vector2(0, 10));

            if (relativePath.StartsWith("Content/Textures/") || 
                relativePath.StartsWith("Content/Audio/") || 
                relativePath.StartsWith("Content/Models/"))
            {
                if (ImGui.Button("Delete Asset From Project", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 30)))
                {
                    TextureCache.Unload(absolutePath);
                    bool success = AssetPipelineSynchronizer.UnregisterAsset(GlobalState.CurrentProjectPath!, relativePath, GlobalState.Log);
                    if (success)
                    {
                        GlobalState.SelectedResourcePath = null;
                        GlobalState.OpenResources.Remove(relativePath);
                        GlobalState.Log($"Asset {fileName} removed.");
                    }
                }
            }
            else if (relativePath.StartsWith("Scripts/"))
            {
                if (ImGui.Button("Delete Script", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 30)))
                {
                    try
                    {
                        if (_scriptEditors.ContainsKey(absolutePath))
                        {
                            _scriptEditors.Remove(absolutePath);
                        }
                        File.Delete(absolutePath);
                        GlobalState.SelectedResourcePath = null;
                        GlobalState.OpenResources.Remove(relativePath);
                        GlobalState.Log($"Deleted script {fileName}.");
                    }
                    catch (Exception ex)
                    {
                        GlobalState.Log($"Error deleting script: {ex.Message}");
                    }
                }
            }
            else if (relativePath.StartsWith("Content/Scenes/") && relativePath.EndsWith(".json"))
            {
                if (ImGui.Button("Delete Scene Config", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 30)))
                {
                    try
                    {
                        if (_sceneStates.TryGetValue(absolutePath, out var sState))
                        {
                            if (sState.RenderTargetId != IntPtr.Zero)
                            {
                                TextureCache.UnbindRenderTarget(sState.RenderTargetId);
                            }
                            sState.RenderTarget?.Dispose();
                            sState.FallbackTexture?.Dispose();
                            sState.SpriteBatch?.Dispose();
                            _sceneStates.Remove(absolutePath);
                        }
                        File.Delete(absolutePath);
                        GlobalState.SelectedResourcePath = null;
                        GlobalState.OpenResources.Remove(relativePath);
                        GlobalState.Log($"Deleted scene config {fileName}.");
                    }
                    catch (Exception ex)
                    {
                        GlobalState.Log($"Error deleting scene config: {ex.Message}");
                    }
                }
            }
        }

        private static void DrawDocumentEditor(string relativePath)
        {
            string absolutePath = Path.Combine(GlobalState.CurrentProjectPath!, relativePath);
            string fileName = Path.GetFileName(relativePath);

            if (relativePath.StartsWith("Scripts/"))
            {
                if (!_scriptEditors.TryGetValue(absolutePath, out var editor))
                {
                    editor = new TextEditor();
                    editor.SyntaxHighlighter = new ImGuiColorTextEditNet.Syntax.CStyleHighlighter(useCpp: true);
                    try
                    {
                        if (File.Exists(absolutePath))
                        {
                            editor.AllText = File.ReadAllText(absolutePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        GlobalState.Log($"Error reading script: {ex.Message}");
                    }
                    _scriptEditors[absolutePath] = editor;
                }

                float spacing = ImGui.GetStyle().ItemSpacing.X;
                float halfWidth = (ImGui.GetContentRegionAvail().X - spacing) / 2;

                bool ctrlS = ImGui.GetIO().KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.S);
                if (ImGui.Button("Save Changes", new System.Numerics.Vector2(halfWidth, 30)) || ctrlS)
                {
                    try
                    {
                        File.WriteAllText(absolutePath, editor.AllText);
                        GlobalState.Log($"Saved script changes to {fileName}");
                    }
                    catch (Exception ex)
                    {
                        GlobalState.Log($"Error saving script: {ex.Message}");
                    }
                }

                ImGui.SameLine();
                if (ImGui.Button("Open External", new System.Numerics.Vector2(halfWidth, 30)))
                {
                    try
                    {
                        var psi = new ProcessStartInfo { FileName = absolutePath, UseShellExecute = true };
                        Process.Start(psi);
                        GlobalState.Log($"Opened script {fileName} in OS editor.");
                    }
                    catch (Exception ex)
                    {
                        GlobalState.Log($"Error opening script: {ex.Message}");
                    }
                }

                ImGui.Separator();
                ImGui.Dummy(new System.Numerics.Vector2(0, 5));

                editor.Render($"Editor##{absolutePath}", ImGui.GetContentRegionAvail());
            }
            else if (relativePath.StartsWith("Content/Scenes/") && relativePath.EndsWith(".json"))
            {
                DrawSceneEditor(relativePath, absolutePath);
            }
            else
            {
                ImGui.Text($"Editing properties of {fileName} inside the right-side 'Properties' panel.");
            }
        }

        private static void DrawSceneEditor(string relativePath, string absolutePath)
        {
            if (!_sceneStates.TryGetValue(absolutePath, out var state))
            {
                state = new SceneEditorState();
                state.Scene = SceneSerializer.LoadScenePath(absolutePath, GlobalState.Log);
                state.SpriteBatch = new SpriteBatch(GlobalState.GraphicsDevice!);
                
                state.FallbackTexture = new Texture2D(GlobalState.GraphicsDevice!, 1, 1);
                state.FallbackTexture.SetData(new[] { Microsoft.Xna.Framework.Color.Magenta });

                _sceneStates[absolutePath] = state;
            }

            ImGui.Text("Scene Layout & Visual Viewport Editor");
            ImGui.Separator();

            // Find available textures
            string texturesDir = Path.Combine(GlobalState.CurrentProjectPath!, "Content", "Textures");
            var availableTextures = new List<string>();
            if (Directory.Exists(texturesDir))
            {
                foreach (var file in Directory.GetFiles(texturesDir))
                {
                    availableTextures.Add(Path.GetFileNameWithoutExtension(file));
                }
            }

            // Find available prefabs
            string prefabsDir = Path.Combine(GlobalState.CurrentProjectPath!, "Prefabs");
            var availablePrefabs = new List<string>();
            if (Directory.Exists(prefabsDir))
            {
                foreach (var file in Directory.GetFiles(prefabsDir, "*.prefab"))
                {
                    availablePrefabs.Add(Path.GetFileNameWithoutExtension(file));
                }
            }

            // Set up 2-column Resizable ImGui Table
            if (ImGui.BeginTable($"SceneEditorTable##{absolutePath}", 2, ImGuiTableFlags.Resizable))
            {
                ImGui.TableSetupColumn("HierarchyColumn", ImGuiTableColumnFlags.WidthFixed, 300f);
                ImGui.TableSetupColumn("ViewportColumn", ImGuiTableColumnFlags.WidthStretch);
                
                ImGui.TableNextRow();
                
                // --- COLUMN 1: Settings / Hierarchy ---
                ImGui.TableSetColumnIndex(0);
                
                ImGui.BeginChild($"SceneEditorLeftChild##{absolutePath}", new System.Numerics.Vector2(-1, -1), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar);

                if (ImGui.BeginTabBar($"SceneEditorTabs##{absolutePath}"))
                {
                    if (ImGui.BeginTabItem("Settings"))
                    {
                        ImGui.TextColored(new System.Numerics.Vector4(0f, 0.8f, 0.8f, 1f), "Global Scene Settings");
                        ImGui.Dummy(new System.Numerics.Vector2(0, 5));

                        int w = state.Scene.Width;
                        ImGui.Text("Width (px)");
                        ImGui.SetNextItemWidth(-1);
                        if (ImGui.InputInt("##SceneWidth", ref w))
                        {
                            if (w < 100) w = 100;
                            state.Scene.Width = w;
                        }

                        int h = state.Scene.Height;
                        ImGui.Text("Height (px)");
                        ImGui.SetNextItemWidth(-1);
                        if (ImGui.InputInt("##SceneHeight", ref h))
                        {
                            if (h < 100) h = 100;
                            state.Scene.Height = h;
                        }

                        System.Numerics.Vector3 bgCol = state.Scene.BackgroundColor;
                        ImGui.Text("Background Color");
                        if (ImGui.ColorEdit3("##BgColor", ref bgCol))
                        {
                            state.Scene.BackgroundColor = bgCol;
                        }

                        string bgImage = state.Scene.BackgroundImage;
                        ImGui.Text("Background Image");
                        DrawBackgroundImageComboBox(availableTextures, ref bgImage, absolutePath);
                        state.Scene.BackgroundImage = bgImage;

                        ImGui.Dummy(new System.Numerics.Vector2(0, 10));
                        if (ImGui.Button("Save Scene", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 35)))
                        {
                            bool success = SceneSerializer.SaveScenePath(absolutePath, state.Scene, GlobalState.Log);
                            if (success)
                            {
                                GlobalState.Log($"Successfully saved scene configuration: {Path.GetFileName(absolutePath)}");
                            }
                        }

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Hierarchy"))
                    {
                        ImGui.TextColored(new System.Numerics.Vector4(0f, 0.8f, 0.8f, 1f), "Entities Hierarchy");
                        ImGui.Dummy(new System.Numerics.Vector2(0, 5));

                        ImGui.Text($"Entities in Scene: {state.Scene.Instances.Count}");
                        
                        ImGui.BeginChild($"EntitiesList##{absolutePath}", new System.Numerics.Vector2(-1, 200), ImGuiChildFlags.Borders);
                        for (int i = 0; i < state.Scene.Instances.Count; i++)
                        {
                            var inst = state.Scene.Instances[i];
                            string label = $"{i}: '{inst.prefabName}' at ({inst.x:F0}, {inst.y:F0})";
                            if (ImGui.Selectable(label, state.SelectedIndex == i))
                            {
                                state.SelectedIndex = i;
                                state.InstPrefabName = inst.prefabName;
                                state.InstX = (int)inst.x;
                                state.InstY = (int)inst.y;
                            }
                        }
                        ImGui.EndChild();

                        ImGui.Separator();
                        ImGui.Dummy(new System.Numerics.Vector2(0, 5));

                        if (state.SelectedIndex >= 0 && state.SelectedIndex < state.Scene.Instances.Count)
                        {
                            ImGui.TextColored(new System.Numerics.Vector4(0f, 0.8f, 0.8f, 1f), "Edit Selected Entity");

                            DrawPrefabComboBox(availablePrefabs, ref state.InstPrefabName, absolutePath);

                            ImGui.SetNextItemWidth(-1);
                            ImGui.InputInt("Position X", ref state.InstX);
                            ImGui.SetNextItemWidth(-1);
                            ImGui.InputInt("Position Y", ref state.InstY);

                            var inst = state.Scene.Instances[state.SelectedIndex];
                            string pPath = Path.Combine(GlobalState.CurrentProjectPath!, "Prefabs", inst.prefabName + ".prefab");
                            PrefabData basePrefab = PrefabCache.GetPrefab(pPath);

                            if (ImGui.CollapsingHeader("Custom Properties Overrides"))
                            {
                                if (inst.CustomProperties == null) inst.CustomProperties = new Dictionary<string, string>();

                                var keysToRemove = new List<string>();
                                var baseKeys = basePrefab.CustomProperties?.Keys.ToList() ?? new List<string>();

                                if (ImGui.BeginTable($"InstPropsTable_{absolutePath}", 3, ImGuiTableFlags.Resizable))
                                {
                                    ImGui.TableSetupColumn("Key", ImGuiTableColumnFlags.WidthStretch, 0.4f);
                                    ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch, 0.5f);
                                    ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed, 30f);

                                    foreach (var kv in inst.CustomProperties)
                                    {
                                        ImGui.TableNextRow();
                                        
                                        // Column 1: Key name with yellow override check
                                        ImGui.TableSetColumnIndex(0);
                                        ImGui.AlignTextToFramePadding();
                                        bool isOverride = baseKeys.Contains(kv.Key);
                                        if (isOverride)
                                        {
                                            ImGui.TextColored(new System.Numerics.Vector4(1f, 1f, 0f, 1f), $"{kv.Key} (Override)");
                                        }
                                        else
                                        {
                                            ImGui.Text(kv.Key);
                                        }

                                        // Column 2: Value Input
                                        ImGui.TableSetColumnIndex(1);
                                        string val = kv.Value;
                                        ImGui.SetNextItemWidth(-1);
                                        if (ImGui.InputText($"##InstVal_{kv.Key}_{absolutePath}", ref val, 128))
                                        {
                                            inst.CustomProperties[kv.Key] = val;
                                        }

                                        // Column 3: Delete action
                                        ImGui.TableSetColumnIndex(2);
                                        if (ImGui.Button($"X##InstDel_{kv.Key}_{absolutePath}", new System.Numerics.Vector2(-1, 0)))
                                        {
                                            keysToRemove.Add(kv.Key);
                                        }
                                    }
                                    ImGui.EndTable();
                                }

                                foreach (var key in keysToRemove)
                                {
                                    inst.CustomProperties.Remove(key);
                                }

                                if (basePrefab.CustomProperties != null)
                                {
                                    bool hasUnoverridden = basePrefab.CustomProperties.Any(bk => !inst.CustomProperties.ContainsKey(bk.Key));
                                    if (hasUnoverridden)
                                    {
                                        ImGui.Dummy(new System.Numerics.Vector2(0, 5));
                                        ImGui.TextColored(new System.Numerics.Vector4(0f, 0.8f, 0.8f, 1f), "Available Prefab Defaults:");
                                        
                                        if (ImGui.BeginTable($"UnoverriddenProps_{absolutePath}", 2, ImGuiTableFlags.Resizable))
                                        {
                                            ImGui.TableSetupColumn("DefaultKey", ImGuiTableColumnFlags.WidthStretch, 0.7f);
                                            ImGui.TableSetupColumn("ActionOverride", ImGuiTableColumnFlags.WidthFixed, 80f);

                                            foreach (var baseKv in basePrefab.CustomProperties)
                                            {
                                                if (!inst.CustomProperties.ContainsKey(baseKv.Key))
                                                {
                                                    ImGui.TableNextRow();
                                                    
                                                    ImGui.TableSetColumnIndex(0);
                                                    ImGui.AlignTextToFramePadding();
                                                    ImGui.TextColored(new System.Numerics.Vector4(0.6f, 0.6f, 0.6f, 1f), $"{baseKv.Key}: \"{baseKv.Value}\"");

                                                    ImGui.TableSetColumnIndex(1);
                                                    if (ImGui.Button($"Override##Ovr_{baseKv.Key}_{absolutePath}", new System.Numerics.Vector2(-1, 0)))
                                                    {
                                                        inst.CustomProperties[baseKv.Key] = baseKv.Value;
                                                    }
                                                }
                                            }
                                            ImGui.EndTable();
                                        }
                                    }
                                }

                                ImGui.Text("Add Custom Override:");
                                
                                ImGui.Text("Key:");
                                ImGui.SetNextItemWidth(-1);
                                ImGui.InputText($"##InstNewKey_{absolutePath}", ref _newPropKey, 64);
                                
                                ImGui.Text("Value:");
                                ImGui.SetNextItemWidth(-1);
                                ImGui.InputText($"##InstNewVal_{absolutePath}", ref _newPropValue, 128);
                                
                                ImGui.Dummy(new System.Numerics.Vector2(0, 2));
                                if (ImGui.Button($"Add Override##InstAdd_{absolutePath}", new System.Numerics.Vector2(-1, 30)))
                                {
                                    string k = _newPropKey.Trim();
                                    if (!string.IsNullOrEmpty(k))
                                    {
                                        inst.CustomProperties[k] = _newPropValue;
                                        _newPropKey = "";
                                        _newPropValue = "";
                                    }
                                }
                            }

                            float spacing = ImGui.GetStyle().ItemSpacing.X;
                            float halfWidth = (ImGui.GetContentRegionAvail().X - spacing) / 2;

                            if (ImGui.Button("Update Pos", new System.Numerics.Vector2(halfWidth, 30)))
                            {
                                inst.prefabName = state.InstPrefabName;
                                inst.x = state.InstX;
                                inst.y = state.InstY;
                                GlobalState.Log($"Updated entity {state.SelectedIndex} properties.");
                            }
                            
                            ImGui.SameLine();
                            if (ImGui.Button("Delete Entity", new System.Numerics.Vector2(halfWidth, 30)))
                            {
                                state.Scene.Instances.RemoveAt(state.SelectedIndex);
                                GlobalState.Log($"Removed entity {state.SelectedIndex}.");
                                state.SelectedIndex = -1;
                            }
                        }
                        else
                        {
                            ImGui.TextColored(new System.Numerics.Vector4(0f, 0.8f, 0f, 1f), "Add New Entity");

                            if (string.IsNullOrEmpty(state.InstPrefabName) && availablePrefabs.Count > 0)
                            {
                                state.InstPrefabName = availablePrefabs[0];
                            }

                            DrawPrefabComboBox(availablePrefabs, ref state.InstPrefabName, absolutePath);

                            ImGui.SetNextItemWidth(-1);
                            ImGui.InputInt("Position X", ref state.InstX);
                            ImGui.SetNextItemWidth(-1);
                            ImGui.InputInt("Position Y", ref state.InstY);

                            if (ImGui.Button("Add Entity", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 30)))
                            {
                                if (string.IsNullOrEmpty(state.InstPrefabName))
                                {
                                    GlobalState.Log("Error: Create a prefab in the project first.");
                                }
                                else
                                {
                                    state.Scene.Instances.Add(new SceneSerializer.EntityInstance
                                    {
                                        prefabName = state.InstPrefabName,
                                        x = state.InstX,
                                        y = state.InstY
                                    });
                                    GlobalState.Log($"Added entity '{state.InstPrefabName}' at ({state.InstX}, {state.InstY}).");
                                }
                            }
                        }

                        ImGui.Dummy(new System.Numerics.Vector2(0, 10));
                        if (ImGui.Button("Save Scene Config", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 35)))
                        {
                            bool success = SceneSerializer.SaveScenePath(absolutePath, state.Scene, GlobalState.Log);
                            if (success)
                            {
                                GlobalState.Log($"Successfully saved scene configuration: {Path.GetFileName(absolutePath)}");
                            }
                        }

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }

                ImGui.EndChild();

                // --- COLUMN 2: Visual Viewport Canvas ---
                ImGui.TableSetColumnIndex(1);

                ImGui.BeginChild($"SceneEditorViewportChild##{absolutePath}", new System.Numerics.Vector2(-1, -1), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar);

                int desiredW = state.Scene.Width;
                int desiredH = state.Scene.Height;

                if (state.RenderTarget == null || state.RenderTarget.Width != desiredW || state.RenderTarget.Height != desiredH)
                {
                    if (state.RenderTargetId != IntPtr.Zero)
                    {
                        TextureCache.UnbindRenderTarget(state.RenderTargetId);
                    }
                    state.RenderTarget?.Dispose();

                    state.RenderTarget = new RenderTarget2D(GlobalState.GraphicsDevice!, desiredW, desiredH);
                    state.RenderTargetId = TextureCache.BindRenderTarget(state.RenderTarget);
                }

                // Render to target
                var oldTargets = GlobalState.GraphicsDevice!.GetRenderTargets();
                GlobalState.GraphicsDevice.SetRenderTarget(state.RenderTarget);
                
                // Clear using BackgroundColor property from SceneData
                var clearColor = new Color(state.Scene.BackgroundColor.X, state.Scene.BackgroundColor.Y, state.Scene.BackgroundColor.Z);
                GlobalState.GraphicsDevice.Clear(clearColor);

                state.SpriteBatch!.Begin();

                // 1. Draw static background image (if set)
                if (!string.IsNullOrEmpty(state.Scene.BackgroundImage))
                {
                    string bgPath = Path.Combine(GlobalState.CurrentProjectPath!, "Content", "Textures", state.Scene.BackgroundImage + ".png");
                    if (!File.Exists(bgPath)) bgPath = Path.Combine(GlobalState.CurrentProjectPath!, "Content", "Textures", state.Scene.BackgroundImage + ".jpg");
                    if (!File.Exists(bgPath)) bgPath = Path.Combine(GlobalState.CurrentProjectPath!, "Content", "Textures", state.Scene.BackgroundImage + ".jpeg");

                    Texture2D? bgTex = TextureCache.GetTexture(bgPath);
                    if (bgTex != null)
                    {
                        state.SpriteBatch.Draw(bgTex, new Rectangle(0, 0, desiredW, desiredH), Color.White);
                    }
                }

                // 2. Draw background Grid (spacing = 64px)
                int gridSpacing = 64;
                if (GlobalState.PixelTexture != null)
                {
                    for (int gx = 0; gx <= desiredW; gx += gridSpacing)
                    {
                        state.SpriteBatch.Draw(GlobalState.PixelTexture, new Rectangle(gx, 0, 1, desiredH), Color.White * 0.1f);
                    }
                    for (int gy = 0; gy <= desiredH; gy += gridSpacing)
                    {
                        state.SpriteBatch.Draw(GlobalState.PixelTexture, new Rectangle(0, gy, desiredW, 1), Color.White * 0.1f);
                    }
                }

                // 3. Draw instances
                for (int i = 0; i < state.Scene.Instances.Count; i++)
                {
                    var inst = state.Scene.Instances[i];
                    
                    // Look up prefab properties
                    string pPath = Path.Combine(GlobalState.CurrentProjectPath!, "Prefabs", inst.prefabName + ".prefab");
                    PrefabData pData = PrefabCache.GetPrefab(pPath);

                    string texPath = Path.Combine(GlobalState.CurrentProjectPath!, "Content", "Textures", pData.TextureName + ".png");
                    if (!File.Exists(texPath)) texPath = Path.Combine(GlobalState.CurrentProjectPath!, "Content", "Textures", pData.TextureName + ".jpg");
                    if (!File.Exists(texPath)) texPath = Path.Combine(GlobalState.CurrentProjectPath!, "Content", "Textures", pData.TextureName + ".jpeg");

                    Texture2D? texture = TextureCache.GetTexture(texPath);
                    
                    float drawW = 64f;
                    float drawH = 64f;
                    if (texture != null)
                    {
                        drawW = texture.Width;
                        drawH = texture.Height;
                        state.SpriteBatch.Draw(texture, new Rectangle((int)inst.x, (int)inst.y, (int)drawW, (int)drawH), Color.White);
                    }
                    else
                    {
                        state.SpriteBatch.Draw(state.FallbackTexture!, new Rectangle((int)inst.x, (int)inst.y, (int)drawW, (int)drawH), Color.White);
                    }

                    // 4. Draw selection Bounding Box visual highlight (yellow)
                    if (state.SelectedIndex == i && GlobalState.PixelTexture != null)
                    {
                        int borderThickness = 2;
                        Color borderCol = Color.Yellow;

                        // Top border
                        state.SpriteBatch.Draw(GlobalState.PixelTexture, new Rectangle((int)inst.x, (int)inst.y, (int)drawW, borderThickness), borderCol);
                        // Bottom border
                        state.SpriteBatch.Draw(GlobalState.PixelTexture, new Rectangle((int)inst.x, (int)inst.y + (int)drawH - borderThickness, (int)drawW, borderThickness), borderCol);
                        // Left border
                        state.SpriteBatch.Draw(GlobalState.PixelTexture, new Rectangle((int)inst.x, (int)inst.y, borderThickness, (int)drawH), borderCol);
                        // Right border
                        state.SpriteBatch.Draw(GlobalState.PixelTexture, new Rectangle((int)inst.x + (int)drawW - borderThickness, (int)inst.y, borderThickness, (int)drawH), borderCol);
                    }
                }

                state.SpriteBatch.End();
                GlobalState.GraphicsDevice.SetRenderTargets(oldTargets);

                // Viewport mouse and coordinate translations
                System.Numerics.Vector2 canvasSize = new System.Numerics.Vector2(desiredW, desiredH);
                System.Numerics.Vector2 canvasPos = ImGui.GetCursorScreenPos();
                ImGui.ImageButton($"ViewportCanvas##{absolutePath}", state.RenderTargetId, canvasSize, System.Numerics.Vector2.Zero, System.Numerics.Vector2.One);

                // Mouse coordinates translation from window space to local scene space (since it's drawn 1:1, it's 1:1!)
                System.Numerics.Vector2 mousePos = ImGui.GetMousePos();
                float scaledX = mousePos.X - canvasPos.X;
                float scaledY = mousePos.Y - canvasPos.Y;

                // Mouse Picking logic (Z-order reverse search topmost first)
                if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    int clickedIndex = -1;
                    for (int i = state.Scene.Instances.Count - 1; i >= 0; i--)
                    {
                        var inst = state.Scene.Instances[i];
                        float w = 64f;
                        float h = 64f;

                        string pPath = Path.Combine(GlobalState.CurrentProjectPath!, "Prefabs", inst.prefabName + ".prefab");
                        PrefabData pData = PrefabCache.GetPrefab(pPath);

                        string texPath = Path.Combine(GlobalState.CurrentProjectPath!, "Content", "Textures", pData.TextureName + ".png");
                        if (!File.Exists(texPath)) texPath = Path.Combine(GlobalState.CurrentProjectPath!, "Content", "Textures", pData.TextureName + ".jpg");
                        if (!File.Exists(texPath)) texPath = Path.Combine(GlobalState.CurrentProjectPath!, "Content", "Textures", pData.TextureName + ".jpeg");

                        Texture2D? texture = TextureCache.GetTexture(texPath);
                        if (texture != null)
                        {
                            w = texture.Width;
                            h = texture.Height;
                        }

                        if (scaledX >= inst.x && scaledX <= inst.x + w &&
                            scaledY >= inst.y && scaledY <= inst.y + h)
                        {
                            clickedIndex = i;
                            break; // Find topmost first
                        }
                    }

                    state.SelectedIndex = clickedIndex;
                    if (clickedIndex >= 0)
                    {
                        var inst = state.Scene.Instances[clickedIndex];
                        state.InstPrefabName = inst.prefabName;
                        state.InstX = (int)inst.x;
                        state.InstY = (int)inst.y;
                    }
                }

                // Mouse Dragging logic (1:1 dragging delta)
                if (state.SelectedIndex >= 0 && state.SelectedIndex < state.Scene.Instances.Count)
                {
                    if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                    {
                        System.Numerics.Vector2 dragDelta = ImGui.GetIO().MouseDelta;
                        var inst = state.Scene.Instances[state.SelectedIndex];
                        inst.x += dragDelta.X;
                        inst.y += dragDelta.Y;

                        state.InstX = (int)inst.x;
                        state.InstY = (int)inst.y;
                    }
                }

                // Drag & Drop zones (1:1 drop coordinates, accepting ONLY .prefab files)
                if (ImGui.BeginDragDropTarget())
                {
                    var payload = ImGui.AcceptDragDropPayload("EXPLORER_ASSET");
                    unsafe
                    {
                        if (payload.NativePtr != null)
                        {
                            byte[] data = new byte[payload.DataSize];
                            System.Runtime.InteropServices.Marshal.Copy((IntPtr)payload.Data, data, 0, payload.DataSize);
                            string draggedPath = System.Text.Encoding.UTF8.GetString(data);

                            if (draggedPath.StartsWith("Prefabs/") && draggedPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                            {
                                string prefabName = Path.GetFileNameWithoutExtension(draggedPath);
                                
                                float dropScaledX = mousePos.X - canvasPos.X;
                                float dropScaledY = mousePos.Y - canvasPos.Y;

                                state.Scene.Instances.Add(new SceneSerializer.EntityInstance
                                {
                                    prefabName = prefabName,
                                    x = dropScaledX,
                                    y = dropScaledY
                                });
                                GlobalState.Log($"Dropped prefab '{prefabName}' onto scene at ({dropScaledX:F0}, {dropScaledY:F0})");
                            }
                            else
                            {
                                GlobalState.Log("Rejected drop: Only .prefab files from the Prefabs folder can be dropped onto the scene.");
                            }
                        }
                    }
                    ImGui.EndDragDropTarget();
                }

                ImGui.EndChild();

                ImGui.EndTable();
            }
        }

        private static void DrawTextureComboBox(List<string> availableTextures, ref string selectedTextureName, string absolutePath)
        {
            if (availableTextures.Count == 0)
            {
                ImGui.TextColored(new System.Numerics.Vector4(0.8f, 0.3f, 0.3f, 1f), "No textures available in project!");
                return;
            }

            if (!availableTextures.Contains(selectedTextureName))
            {
                selectedTextureName = availableTextures[0];
            }

            ImGui.SetNextItemWidth(-1);
            if (ImGui.BeginCombo($"##TextureCombo_{absolutePath}", selectedTextureName))
            {
                foreach (var tex in availableTextures)
                {
                    bool isSelected = (selectedTextureName == tex);
                    if (ImGui.Selectable(tex, isSelected))
                    {
                        selectedTextureName = tex;
                    }
                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }
                ImGui.EndCombo();
            }
        }

        private static void DrawPrefabComboBox(List<string> availablePrefabs, ref string selectedPrefabName, string absolutePath)
        {
            if (availablePrefabs.Count == 0)
            {
                ImGui.TextColored(new System.Numerics.Vector4(0.8f, 0.3f, 0.3f, 1f), "No prefabs available in project!");
                return;
            }

            if (!availablePrefabs.Contains(selectedPrefabName))
            {
                selectedPrefabName = availablePrefabs[0];
            }

            ImGui.SetNextItemWidth(-1);
            if (ImGui.BeginCombo($"##PrefabCombo_{absolutePath}", selectedPrefabName))
            {
                foreach (var prefab in availablePrefabs)
                {
                    bool isSelected = (selectedPrefabName == prefab);
                    if (ImGui.Selectable(prefab, isSelected))
                    {
                        selectedPrefabName = prefab;
                    }
                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }
                ImGui.EndCombo();
            }
        }

        private static void DrawBackgroundImageComboBox(List<string> availableTextures, ref string selectedBgName, string absolutePath)
        {
            string previewText = string.IsNullOrEmpty(selectedBgName) ? "(None)" : selectedBgName;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.BeginCombo($"##BgCombo_{absolutePath}", previewText))
            {
                if (ImGui.Selectable("(None)", string.IsNullOrEmpty(selectedBgName)))
                {
                    selectedBgName = string.Empty;
                }

                foreach (var tex in availableTextures)
                {
                    bool isSelected = (selectedBgName == tex);
                    if (ImGui.Selectable(tex, isSelected))
                    {
                        selectedBgName = tex;
                    }
                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }
                ImGui.EndCombo();
            }
        }
    }
}
