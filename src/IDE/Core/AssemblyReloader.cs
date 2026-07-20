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

            ProjectMigrator.Shift(projectRoot, logCallback);

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

                // Terminate any active standalone game processes to prevent file locks
                try
                {
                    var runningProcesses = Process.GetProcessesByName(projectName);
                    foreach (var p in runningProcesses)
                    {
                        try
                        {
                            p.Kill();
                            p.WaitForExit(2000);
                            logCallback($"Stopped running instance of {projectName} (PID {p.Id}) to release executable file locks.");
                        }
                        catch (Exception ex)
                        {
                            logCallback($"Warning: Could not terminate process {projectName} (PID {p.Id}): {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    logCallback($"Warning: Error scanning for running {projectName} processes: {ex.Message}");
                }

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
                    if (_loadedAssembly != null)
                    {
                        TeardownLoadedAssembly(_loadedAssembly, logCallback);
                        try
                        {
                            Type? entityManagerType = _loadedAssembly.GetType("MonoGameMaker.Runtime.EntityManager");
                            if (entityManagerType != null)
                            {
                                var purgeMethod = entityManagerType.GetMethod("PurgeAllScripts", BindingFlags.Public | BindingFlags.Static);
                                if (purgeMethod != null)
                                {
                                    purgeMethod.Invoke(null, null);
                                    logCallback("Deterministic script teardown: Purged all scripts from the old assembly.");
                                }
                                else
                                {
                                    logCallback("Warning: PurgeAllScripts method not found in LoadedAssembly's EntityManager.");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            logCallback($"Warning during PurgeAllScripts invocation: {ex.Message}");
                        }
                    }

                    WeakReference weakContext = new WeakReference(_currentContext);

                    // Annull old pointers before unload and collection to prevent keeping strong roots
                    _loadedAssembly = null;

                    try
                    {
                        _currentContext.Unload();
                    }
                    catch (Exception ex)
                    {
                        logCallback($"Warning during Assembly Load Context unload: {ex.Message}");
                    }

                    _currentContext = null;

                    // Execute aggressive garbage collection
                    for (int i = 0; i < 10; i++)
                    {
                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                        GC.WaitForPendingFinalizers();
                        if (!weakContext.IsAlive)
                        {
                            logCallback($"Collectible AssemblyLoadContext collected successfully after {i + 1} iterations.");
                            break;
                        }
                    }

                    if (weakContext.IsAlive)
                    {
                        string warnMsg = "Warning: Collectible AssemblyLoadContext could not be fully collected (memory leak detected).";
                        logCallback(warnMsg);
                        GlobalState.Log(warnMsg);
                    }
                }

                _currentContext = new CollectibleAssemblyLoadContext(dllPath);
                
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

        private static void TeardownLoadedAssembly(Assembly assembly, Action<string> logCallback)
        {
            try
            {
                logCallback("Executing generic assembly Teardown...");
                
                // Purge all static fields of delegate/event type in the assembly's types to clear potential event handler leaks
                foreach (Type type in assembly.GetTypes())
                {
                    try
                    {
                        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        foreach (var field in fields)
                        {
                            if (typeof(Delegate).IsAssignableFrom(field.FieldType))
                            {
                                try
                                {
                                    field.SetValue(null, null);
                                    logCallback($"Cleared static event handler field {type.FullName}.{field.Name}");
                                }
                                catch (Exception ex)
                                {
                                    logCallback($"Warning: could not clear field {type.FullName}.{field.Name}: {ex.Message}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logCallback($"Warning: could not process fields for type {type.FullName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                logCallback($"Error during assembly teardown: {ex.Message}");
            }
        }
    }

    public class CollectibleAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver? _resolver;

        public CollectibleAssemblyLoadContext(string? assemblyPath = null) : base(isCollectible: true)
        {
            if (assemblyPath != null)
            {
                _resolver = new AssemblyDependencyResolver(assemblyPath);
            }
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (_resolver != null)
            {
                string? path = _resolver.ResolveAssemblyToPath(assemblyName);
                if (path != null)
                {
                    return LoadFromAssemblyPath(path);
                }
            }
            return null;
        }
    }
}
