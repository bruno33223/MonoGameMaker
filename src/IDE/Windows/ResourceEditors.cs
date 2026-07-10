using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using ImGuiNET;
using ImGuiColorTextEditNet;
using MonoGameMaker.IDE.Core;

namespace MonoGameMaker.IDE.Windows
{
    public static class ResourceEditors
    {
        private static readonly Dictionary<string, string> _importPaths = new();
        private static readonly Dictionary<string, TextEditor> _scriptEditors = new();
        private static readonly Dictionary<string, RoomEditorState> _roomStates = new();
        
        private static string _newScriptName = "Script1";

        private class RoomEditorState
        {
            public List<RoomSerializer.RoomInstance> Instances = new();
            public int SelectedIndex = -1;
            public string InstSpriteName = "";
            public int InstX = 100;
            public int InstY = 100;
        }

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
                DrawFileProperties(res, absolutePath);
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
                    
                    if (_roomStates.ContainsKey(absolutePath))
                    {
                        _roomStates.Remove(absolutePath);
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

            if (folderName == "Sprites" || folderName == "Backgrounds" || folderName == "Sounds")
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
                    string filter = folderName == "Sounds" ? "wav,mp3" : "png,jpg,jpeg";
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

namespace {GlobalState.CurrentProjectName}.Scripts
{{
    public static class {cleanName}
    {{
        public static void Execute()
        {{
            // Custom game/behavior logic
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
            else if (folderName == "Rooms")
            {
                ImGui.Text("Create New Room Configuration");
                if (ImGui.Button("Create Default room_init.json", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 30)))
                {
                    string dest = Path.Combine(absolutePath, "room_init.json");
                    if (File.Exists(dest))
                    {
                        GlobalState.Log("Room configuration already exists.");
                    }
                    else
                    {
                        File.WriteAllText(dest, "[]");
                        GlobalState.Log("Created room_init.json.");
                    }
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

            if (relativePath.StartsWith("Content/Sprites/") || 
                relativePath.StartsWith("Content/Backgrounds/") || 
                relativePath.StartsWith("Content/Sounds/"))
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
            else if (relativePath.StartsWith("Content/Rooms/") && relativePath.EndsWith(".json"))
            {
                if (ImGui.Button("Delete Room Config", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 30)))
                {
                    try
                    {
                        if (_roomStates.ContainsKey(absolutePath))
                        {
                            _roomStates.Remove(absolutePath);
                        }
                        File.Delete(absolutePath);
                        GlobalState.SelectedResourcePath = null;
                        GlobalState.OpenResources.Remove(relativePath);
                        GlobalState.Log($"Deleted room config {fileName}.");
                    }
                    catch (Exception ex)
                    {
                        GlobalState.Log($"Error deleting room config: {ex.Message}");
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
            else if (relativePath.StartsWith("Content/Rooms/") && relativePath.EndsWith(".json"))
            {
                DrawRoomEditor(relativePath, absolutePath);
            }
            else
            {
                ImGui.Text($"Editing properties of {fileName} inside the right-side 'Properties' panel.");
            }
        }

        private static void DrawRoomEditor(string relativePath, string absolutePath)
        {
            if (!_roomStates.TryGetValue(absolutePath, out var state))
            {
                state = new RoomEditorState();
                state.Instances = RoomSerializer.LoadRoomPath(absolutePath, GlobalState.Log);
                _roomStates[absolutePath] = state;
            }

            ImGui.Text("Room Layout Coordinates Editor");
            ImGui.Separator();

            string spritesDir = Path.Combine(GlobalState.CurrentProjectPath!, "Content", "Sprites");
            var availableSprites = new List<string>();
            if (Directory.Exists(spritesDir))
            {
                foreach (var file in Directory.GetFiles(spritesDir))
                {
                    availableSprites.Add(Path.GetFileNameWithoutExtension(file));
                }
            }

            ImGui.Text($"Instances in Room: {state.Instances.Count}");
            
            ImGui.BeginChild($"InstancesList##{absolutePath}", new System.Numerics.Vector2(-1, 200), ImGuiChildFlags.Borders);
            for (int i = 0; i < state.Instances.Count; i++)
            {
                var inst = state.Instances[i];
                string label = $"{i}: Sprite '{inst.spriteName}' at ({inst.x}, {inst.y})";
                if (ImGui.Selectable(label, state.SelectedIndex == i))
                {
                    state.SelectedIndex = i;
                    state.InstSpriteName = inst.spriteName;
                    state.InstX = (int)inst.x;
                    state.InstY = (int)inst.y;
                }
            }
            ImGui.EndChild();

            ImGui.Separator();
            ImGui.Dummy(new System.Numerics.Vector2(0, 5));

            if (state.SelectedIndex >= 0 && state.SelectedIndex < state.Instances.Count)
            {
                ImGui.TextColored(new System.Numerics.Vector4(0f, 0.8f, 0.8f, 1f), "Edit Instance");

                DrawSpriteComboBox(availableSprites, ref state.InstSpriteName, absolutePath);

                ImGui.SetNextItemWidth(-1);
                ImGui.InputInt("Position X", ref state.InstX);
                ImGui.SetNextItemWidth(-1);
                ImGui.InputInt("Position Y", ref state.InstY);

                float spacing = ImGui.GetStyle().ItemSpacing.X;
                float halfWidth = (ImGui.GetContentRegionAvail().X - spacing) / 2;

                if (ImGui.Button("Update Instance", new System.Numerics.Vector2(halfWidth, 30)))
                {
                    var inst = state.Instances[state.SelectedIndex];
                    inst.spriteName = state.InstSpriteName;
                    inst.x = state.InstX;
                    inst.y = state.InstY;
                    GlobalState.Log($"Updated instance {state.SelectedIndex} to ({state.InstX}, {state.InstY}).");
                }
                
                ImGui.SameLine();
                if (ImGui.Button("Remove Instance", new System.Numerics.Vector2(halfWidth, 30)))
                {
                    state.Instances.RemoveAt(state.SelectedIndex);
                    GlobalState.Log($"Removed instance {state.SelectedIndex}.");
                    state.SelectedIndex = -1;
                }
            }
            else
            {
                ImGui.TextColored(new System.Numerics.Vector4(0f, 0.8f, 0f, 1f), "Add New Instance");

                if (string.IsNullOrEmpty(state.InstSpriteName) && availableSprites.Count > 0)
                {
                    state.InstSpriteName = availableSprites[0];
                }

                DrawSpriteComboBox(availableSprites, ref state.InstSpriteName, absolutePath);

                ImGui.SetNextItemWidth(-1);
                ImGui.InputInt("Position X", ref state.InstX);
                ImGui.SetNextItemWidth(-1);
                ImGui.InputInt("Position Y", ref state.InstY);

                if (ImGui.Button("Add Instance", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 30)))
                {
                    if (string.IsNullOrEmpty(state.InstSpriteName))
                    {
                        GlobalState.Log("Error: Add a sprite to the project first.");
                    }
                    else
                    {
                        state.Instances.Add(new RoomSerializer.RoomInstance
                        {
                            spriteName = state.InstSpriteName,
                            x = state.InstX,
                            y = state.InstY
                        });
                        GlobalState.Log($"Added instance of '{state.InstSpriteName}' at ({state.InstX}, {state.InstY}).");
                    }
                }
            }

            ImGui.Dummy(new System.Numerics.Vector2(0, 10));
            if (ImGui.Button("Save Room Configuration", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 35)))
            {
                bool success = RoomSerializer.SaveRoomPath(absolutePath, state.Instances, GlobalState.Log);
                if (success)
                {
                    GlobalState.Log($"Successfully saved room layout file: {Path.GetFileName(absolutePath)}");
                }
            }
        }

        private static void DrawSpriteComboBox(List<string> availableSprites, ref string selectedSpriteName, string absolutePath)
        {
            if (availableSprites.Count == 0)
            {
                ImGui.TextColored(new System.Numerics.Vector4(0.8f, 0.3f, 0.3f, 1f), "No sprites available in project!");
                return;
            }

            if (!availableSprites.Contains(selectedSpriteName))
            {
                selectedSpriteName = availableSprites[0];
            }

            ImGui.SetNextItemWidth(-1);
            if (ImGui.BeginCombo($"##SpriteCombo_{absolutePath}", selectedSpriteName))
            {
                foreach (var sprite in availableSprites)
                {
                    bool isSelected = (selectedSpriteName == sprite);
                    if (ImGui.Selectable(sprite, isSelected))
                    {
                        selectedSpriteName = sprite;
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
