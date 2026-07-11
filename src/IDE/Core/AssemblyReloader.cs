using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

namespace MonoGameMaker.IDE.Core
{
    public static class AssemblyReloader
    {
        private static CollectibleAssemblyLoadContext? _currentContext;
        private static Assembly? _loadedAssembly;

        public static Assembly? LoadedAssembly => _loadedAssembly;

        public static bool CompileAndLoad(string? projectRoot, Action<string> logCallback)
        {
            if (string.IsNullOrEmpty(projectRoot) || !Directory.Exists(projectRoot))
            {
                logCallback("No project directory open, compile skipped.");
                return false;
            }

            try
            {
                // 1. Locate csproj
                string[] csprojs = Directory.GetFiles(projectRoot, "*.csproj");
                if (csprojs.Length == 0)
                {
                    logCallback("Error: No .csproj found in project directory.");
                    return false;
                }
                string csprojPath = csprojs[0];
                string projectName = Path.GetFileNameWithoutExtension(csprojPath);

                logCallback($"Building project {projectName} in background...");

                // Sync core runtime templates automatically to apply engine bugfixes without recreation
                TemplateEngine.SyncRuntimeFiles(projectRoot);

                // 2. Run dotnet build
                string dotnetPath = TemplateEngine.GetDotnetPath();
                var psi = new ProcessStartInfo
                {
                    FileName = dotnetPath,
                    Arguments = $"build \"{csprojPath}\" --configuration Debug",
                    WorkingDirectory = projectRoot,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                TemplateEngine.ConfigureDotnetPath(psi);

                using var process = Process.Start(psi);
                if (process == null)
                {
                    logCallback("Error: Failed to start MSBuild build process.");
                    return false;
                }

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    logCallback("=== BUILD COMPILATION FAILED ===");
                    if (!string.IsNullOrEmpty(output)) logCallback(output);
                    if (!string.IsNullOrEmpty(error)) logCallback(error);
                    return false;
                }

                logCallback("Build compilation succeeded. Loading assembly...");

                // 3. Locate compiled DLL
                string dllPath = Path.Combine(projectRoot, "bin", "Debug", "net8.0", $"{projectName}.dll");
                string pdbPath = Path.Combine(projectRoot, "bin", "Debug", "net8.0", $"{projectName}.pdb");

                if (!File.Exists(dllPath))
                {
                    logCallback($"Error: Target DLL not found at {dllPath}");
                    return false;
                }

                // 4. Load Assembly in Collectible ALC
                // Unload old context first
                if (_currentContext != null)
                {
                    try
                    {
                        _currentContext.Unload();
                    }
                    catch (Exception ex)
                    {
                        logCallback($"Warning during Assembly Load Context unload: {ex.Message}");
                    }
                    _currentContext = null;
                    _loadedAssembly = null;

                    // Force collection of old assembly to release memory
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }

                _currentContext = new CollectibleAssemblyLoadContext();
                
                // Load from memory streams to avoid file locking on target DLL
                byte[] dllBytes = File.ReadAllBytes(dllPath);
                byte[]? pdbBytes = File.Exists(pdbPath) ? File.ReadAllBytes(pdbPath) : null;

                using var dllStream = new MemoryStream(dllBytes);
                using var pdbStream = pdbBytes != null ? new MemoryStream(pdbBytes) : null;

                _loadedAssembly = _currentContext.LoadFromStream(dllStream, pdbStream);
                logCallback($"Successfully loaded assembly {projectName}.dll into memory.");
                return true;
            }
            catch (Exception ex)
            {
                logCallback($"Error during CompileAndLoad: {ex.Message}");
                return false;
            }
        }
    }

    public class CollectibleAssemblyLoadContext : AssemblyLoadContext
    {
        public CollectibleAssemblyLoadContext() : base(isCollectible: true)
        {
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            // Do not override system/IDE assemblies, resolve them normally
            return null;
        }
    }
}
