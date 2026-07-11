using System;
using System.Runtime.InteropServices;
using ImGuiNET;
using System.IO;
using MonoGameMaker.IDE.Core;
using MonoGameMaker.Runtime;

namespace MonoGameMaker.IDE.Windows
{
    public static class ProjectExplorer
    {
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
                if (GlobalState.SelectedResourcePath == relativePath)
                {
                    flags |= ImGuiTreeNodeFlags.Selected;
                }

                ImGui.TreeNodeEx(node.Name, flags);

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
                    GlobalState.SelectedResourcePath = relativePath;
                }

                string ext = Path.GetExtension(relativePath).ToLower();
                bool isEditable = ext == ".cs" || ext == ".json";
                if (isEditable && ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    GlobalState.OpenResources.Add(relativePath);
                }
            }
            else
            {
                ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None;
                if (GlobalState.SelectedResourcePath == relativePath)
                {
                    flags |= ImGuiTreeNodeFlags.Selected;
                }

                if (node.FullPath == GlobalState.CurrentProjectPath)
                {
                    flags |= ImGuiTreeNodeFlags.DefaultOpen;
                }

                bool opened = ImGui.TreeNodeEx(node.Name, flags);

                if (ImGui.BeginPopupContextItem($"FolderContext##{relativePath}"))
                {
                    string folderName = Path.GetFileName(relativePath);
                    if (folderName == "Prefabs")
                    {
                        if (ToolEngine.IsPlaying)
                        {
                            ImGui.TextDisabled("Cannot create prefabs during simulation");
                        }
                        else if (ImGui.MenuItem("Create New Prefab"))
                        {
                            GlobalState.SelectedResourcePath = relativePath;
                            CreateNewPrefab(node.FullPath);
                        }
                    }
                    ImGui.EndPopup();
                }
                
                if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                {
                    GlobalState.SelectedResourcePath = relativePath;
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

        private static void CreateNewPrefab(string fullPath)
        {
            try
            {
                int index = 1;
                string newName = $"NewObject_{index}.prefab";
                string dest = Path.Combine(fullPath, newName);
                while (File.Exists(dest))
                {
                    index++;
                    newName = $"NewObject_{index}.prefab";
                    dest = Path.Combine(fullPath, newName);
                }

                var defaultPrefab = new PrefabData();
                PrefabSerializer.SavePrefab(dest, defaultPrefab, GlobalState.Log);
                GlobalState.Log($"Created new prefab file: {newName}");
            }
            catch (Exception ex)
            {
                GlobalState.Log($"Error creating prefab: {ex.Message}");
            }
        }
    }
}
