using System;
using System.IO;
using System.Collections.Generic;
using System.Timers;
using System.Threading.Tasks;

namespace MonoGameMaker.IDE.Core
{
    public class FileSystemCache : IDisposable
    {
        private readonly string _projectRoot;
        private readonly Action<string> _logCallback;
        private FileSystemWatcher? _watcher;
        private Timer? _debounceTimer;
        
        private readonly object _syncObj = new();
        private FileTreeNode? _currentTree;

        public FileSystemCache(string projectRoot, Action<string> logCallback)
        {
            _projectRoot = projectRoot;
            _logCallback = logCallback;

            // Setup debounce timer (150ms window)
            _debounceTimer = new Timer(150);
            _debounceTimer.AutoReset = false;
            _debounceTimer.Elapsed += (s, e) => TriggerRebuild();

            // Initialize FileSystemWatcher
            InitializeWatcher();

            // Perform initial tree population
            TriggerRebuild();
        }

        private void InitializeWatcher()
        {
            try
            {
                if (!Directory.Exists(_projectRoot)) return;

                _watcher = new FileSystemWatcher(_projectRoot)
                {
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite,
                    EnableRaisingEvents = true
                };

                _watcher.Created += OnFileSystemChanged;
                _watcher.Deleted += OnFileSystemChanged;
                _watcher.Changed += OnFileSystemChanged;
                _watcher.Renamed += OnFileSystemChanged;

                _logCallback("FileSystemWatcher initialized for project root.");
            }
            catch (Exception ex)
            {
                _logCallback($"Error initializing FileSystemWatcher: {ex.Message}");
            }
        }

        private void OnFileSystemChanged(object? sender, FileSystemEventArgs e)
        {
            if (e.FullPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                Task.Run(() => AssemblyReloader.CompileAndLoad(GlobalState.CurrentProjectPath, GlobalState.Log));
            }

            if (_debounceTimer == null) return;
            if (AssetPipelineSynchronizer.IsProcessing) return;
            if (ToolEngine.IsPlaying) return;

            // Restart the debounce window
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        public void ForceRebuild()
        {
            TriggerRebuild();
        }

        private void TriggerRebuild()
        {
            Task.Run(() => RebuildTree());
        }

        private void RebuildTree()
        {
            try
            {
                if (!Directory.Exists(_projectRoot)) return;

                var newTree = BuildNode(_projectRoot);
                lock (_syncObj)
                {
                    _currentTree = newTree;
                }
            }
            catch (Exception ex)
            {
                _logCallback($"Error scanning directories: {ex.Message}");
            }
        }

        private FileTreeNode BuildNode(string fullPath)
        {
            string name = Path.GetFileName(fullPath);
            if (string.IsNullOrEmpty(name))
            {
                name = fullPath; // root folder fallback
            }

            bool isDirectory = Directory.Exists(fullPath);
            var children = new List<FileTreeNode>();

            if (isDirectory)
            {
                try
                {
                    // Scan subdirectories
                    foreach (var dir in Directory.GetDirectories(fullPath))
                    {
                        string dirName = Path.GetFileName(dir);
                        // Filter standard output directories and settings
                        if (dirName == "bin" || dirName == "obj" || dirName == ".git" || dirName == ".vscode" || dirName == ".config")
                        {
                            continue;
                        }
                        children.Add(BuildNode(dir));
                    }

                    // Scan files
                    foreach (var file in Directory.GetFiles(fullPath))
                    {
                        string ext = Path.GetExtension(file).ToLower();
                        // Ignore binaries in the IDE project tree
                        if (ext == ".dll" || ext == ".pdb" || ext == ".exe")
                        {
                            continue;
                        }
                        children.Add(new FileTreeNode(Path.GetFileName(file), file, false, null));
                    }
                }
                catch (Exception)
                {
                    // Swallowing access violations or folder deletion races during scan
                }
            }

            return new FileTreeNode(name, fullPath, isDirectory, children.AsReadOnly());
        }

        public FileTreeNode? GetSnapshot()
        {
            lock (_syncObj)
            {
                return _currentTree;
            }
        }

        public void Dispose()
        {
            if (_watcher != null)
            {
                try
                {
                    _watcher.EnableRaisingEvents = false;
                    _watcher.Created -= OnFileSystemChanged;
                    _watcher.Deleted -= OnFileSystemChanged;
                    _watcher.Changed -= OnFileSystemChanged;
                    _watcher.Renamed -= OnFileSystemChanged;
                    _watcher.Dispose();
                }
                catch
                {
                    // Swallowing disposal exceptions
                }
                _watcher = null;
            }

            if (_debounceTimer != null)
            {
                _debounceTimer.Dispose();
                _debounceTimer = null;
            }
        }
    }
}
