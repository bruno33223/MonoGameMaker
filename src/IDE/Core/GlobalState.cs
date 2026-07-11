using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameMaker.IDE.Core
{
    public static class GlobalState
    {
        public static GraphicsDevice? GraphicsDevice { get; set; }
        public static Texture2D? PixelTexture { get; set; }
        public static string? CurrentProjectPath { get; set; }
        public static string? CurrentProjectName { get; set; }
        public static string? SelectedResourcePath { get; set; }
        public static FileSystemCache? CurrentProjectCache { get; set; }
        public static HashSet<string> OpenResources { get; } = new();
        
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
