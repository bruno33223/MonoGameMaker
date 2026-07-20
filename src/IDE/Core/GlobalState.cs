using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;
using MonoGameMaker.Runtime;

namespace MonoGameMaker.IDE.Core
{
    public static class GlobalState
    {
        public static GraphicsDevice? GraphicsDevice { get; set; }
        public static Texture2D? PixelTexture { get; set; }
        public static string? CurrentProjectPath { get; set; }
        public static string? CurrentProjectName { get; set; }
        
        public static SelectionContext SelectionContext { get; } = new SelectionContext();
        public static CommandManager CommandManager { get; } = new CommandManager();

        [Obsolete("Use SelectionContext diretamente")]
        public static string? SelectedResourcePath
        {
            get => SelectionContext.SelectedResourcePath;
            set => CommandManager.ExecuteCommand(new SelectResourceCommand(SelectionContext, value));
        }

        public static FileSystemCache? CurrentProjectCache { get; set; }
        public static HashSet<string> OpenResources { get; } = new();
        public enum SimState { Edit, Playing, Paused }
        public static SimState CurrentSimState { get; set; } = SimState.Edit;
        public static bool TriggerSingleFrame { get; set; } = false;
        public static bool IsViewportFocused { get; set; } = false;
        public static Microsoft.Xna.Framework.Vector2 ViewportMousePosition { get; set; } = Microsoft.Xna.Framework.Vector2.Zero;

        public static bool IsPlaying
        {
            get => CurrentSimState != SimState.Edit;
            set => CurrentSimState = value ? SimState.Playing : SimState.Edit;
        }

        [Obsolete("Use SelectionContext diretamente")]
        public static SceneSerializer.EntityInstance? SelectedNode
        {
            get
            {
                if (SelectionContext.SelectedEntityId == Guid.Empty) return null;
                if (CurrentProjectPath == null || SelectedResourcePath == null) return null;
                if (SelectedResourcePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    string absolutePath = System.IO.Path.Combine(CurrentProjectPath, SelectedResourcePath);
                    var activeScene = Windows.ResourceEditors.GetSceneData(absolutePath);
                    return activeScene?.Instances.Find(i => i.Id == SelectionContext.SelectedEntityId);
                }
                return null;
            }
            set => CommandManager.ExecuteCommand(new SelectNodeCommand(SelectionContext, value));
        }

        public static List<GameEntity> SimEntities { get; } = new();
        
        public static readonly List<string> ConsoleLogs = new();
        public static Process? RunningGameProcess { get; set; }
        
        public static bool IsGameRunning => RunningGameProcess != null && !RunningGameProcess.HasExited;

        public static void Log(string message)
        {
            lock (ConsoleLogs)
            {
                string log = $"[{DateTime.Now:HH:mm:ss}] {message}";
                ConsoleLogs.Add(log);
                Console.WriteLine(log); // Also output to system stdout
            }
        }

        public static void ClearLogs()
        {
            lock (ConsoleLogs)
            {
                ConsoleLogs.Clear();
            }
        }
    }
}
