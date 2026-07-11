using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MonoGameMaker.IDE.Core
{
    public static class AssetPipelineSynchronizer
    {
        private static bool _isProcessing;
        private static readonly object _lock = new();

        public static bool IsProcessing
        {
            get
            {
                lock (_lock)
                {
                    return _isProcessing;
                }
            }
            private set
            {
                lock (_lock)
                {
                    _isProcessing = value;
                }
            }
        }

        public static Task<bool> RegisterAsset(string projectRoot, string sourceFilePath, string assetType, Action<string> logCallback)
        {
            return Task.Run(() =>
            {
                IsProcessing = true;
                try
                {
                    string fileName = Path.GetFileName(sourceFilePath);
                    string normalizedType = assetType.ToLower() switch
                    {
                        "sprites" or "backgrounds" or "textures" => "Textures",
                        "sounds" or "audio" => "Audio",
                        "models" => "Models",
                        "fonts" => "Fonts",
                        _ => throw new ArgumentException($"Unknown asset type: {assetType}")
                    };

                    string destDir = Path.Combine(projectRoot, "Content", normalizedType);
                    if (!Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    string destFilePath = Path.Combine(destDir, fileName);
                    
                    // Copy the file physically if they are different paths
                    if (string.Compare(Path.GetFullPath(sourceFilePath), Path.GetFullPath(destFilePath), StringComparison.OrdinalIgnoreCase) != 0)
                    {
                        File.Copy(sourceFilePath, destFilePath, overwrite: true);
                        logCallback($"Copied asset to physical path: {destFilePath}");
                    }
                    else
                    {
                        logCallback($"Asset already in project destination path: {destFilePath}");
                    }

                    // Register in MGCB
                    string mgcbPath = Path.Combine(projectRoot, "Content", "Content.mgcb");
                    if (!File.Exists(mgcbPath))
                    {
                        logCallback($"Warning: Content.mgcb not found at {mgcbPath}. Skipping registration.");
                        RunMgcbCompile(projectRoot, logCallback);
                        return true;
                    }

                    // Determine importer and processor
                    string ext = Path.GetExtension(fileName).ToLower();
                    string importer = "";
                    string processor = "";
                    
                    if (normalizedType == "Textures")
                    {
                        importer = "TextureImporter";
                        processor = "TextureProcessor";
                    }
                    else if (normalizedType == "Audio")
                    {
                        if (ext == ".wav")
                        {
                            importer = "WavImporter";
                            processor = "SoundEffectProcessor";
                        }
                        else if (ext == ".mp3")
                        {
                            importer = "Mp3Importer";
                            processor = "SongProcessor";
                        }
                        else
                        {
                            importer = "WavImporter";
                            processor = "SoundEffectProcessor";
                        }
                    }
                    else if (normalizedType == "Models")
                    {
                        importer = "ModelImporter";
                        processor = "ModelProcessor";
                    }
                    else if (normalizedType == "Fonts" || ext == ".spritefont")
                    {
                        importer = "FontDescriptionImporter";
                        processor = "FontDescriptionProcessor";
                    }

                    // Relative path inside Content/ for MGCB
                    string mgcbRelativePath = $"{normalizedType}/{fileName}".Replace("\\", "/");

                    string mgcbContent = File.ReadAllText(mgcbPath);
                    
                    // Remove if already registered to avoid duplicates
                    mgcbContent = RemoveAssetFromMgcbString(mgcbContent, mgcbRelativePath);

                    // Build the directive block
                    var sb = new StringBuilder();
                    sb.AppendLine($"#begin {mgcbRelativePath}");
                    sb.AppendLine($"/importer:{importer}");
                    sb.AppendLine($"/processor:{processor}");
                    sb.AppendLine($"/build:{mgcbRelativePath}");
                    sb.AppendLine($"#end {mgcbRelativePath}");
                    sb.AppendLine();

                    // Append to Content
                    mgcbContent += sb.ToString();
                    File.WriteAllText(mgcbPath, mgcbContent);
                    logCallback($"Registered asset in Content.mgcb: {mgcbRelativePath}");

                    // Compile compiled asset
                    RunMgcbCompile(projectRoot, logCallback);

                    return true;
                }
                catch (Exception ex)
                {
                    logCallback($"Error registering asset: {ex.Message}");
                    return false;
                }
                finally
                {
                    IsProcessing = false;
                    GlobalState.CurrentProjectCache?.ForceRebuild();
                }
            });
        }

        public static Task<bool> UnregisterAsset(string projectRoot, string relativePath, Action<string> logCallback)
        {
            return Task.Run(() =>
            {
                IsProcessing = true;
                try
                {
                    // physical delete
                    string physicalPath = Path.Combine(projectRoot, "Content", relativePath);
                    if (File.Exists(physicalPath))
                    {
                        File.Delete(physicalPath);
                        logCallback($"Deleted physical asset: {physicalPath}");
                    }

                    // mgcb delete
                    string mgcbPath = Path.Combine(projectRoot, "Content", "Content.mgcb");
                    if (File.Exists(mgcbPath))
                    {
                        string mgcbRelativePath = relativePath.Replace("\\", "/");
                        string mgcbContent = File.ReadAllText(mgcbPath);
                        mgcbContent = RemoveAssetFromMgcbString(mgcbContent, mgcbRelativePath);
                        File.WriteAllText(mgcbPath, mgcbContent);
                        logCallback($"Unregistered asset from Content.mgcb: {mgcbRelativePath}");
                    }

                    // Compile asset removal (re-build MGCB contents)
                    RunMgcbCompile(projectRoot, logCallback);

                    return true;
                }
                catch (Exception ex)
                {
                    logCallback($"Error unregistering asset: {ex.Message}");
                    return false;
                }
                finally
                {
                    IsProcessing = false;
                    GlobalState.CurrentProjectCache?.ForceRebuild();
                }
            });
        }

        private static void RunMgcbCompile(string projectRoot, Action<string> logCallback)
        {
            try
            {
                string dotnetPath = TemplateEngine.GetDotnetPath();
                string mgcbPath = Path.Combine(projectRoot, "Content", "Content.mgcb");
                if (!File.Exists(mgcbPath))
                {
                    logCallback("MGCB file not found, skipping compilation.");
                    return;
                }

                logCallback("Running MGCB asset compilation in background...");

                var psi = new ProcessStartInfo
                {
                    FileName = dotnetPath,
                    Arguments = "mgcb /@:Content.mgcb",
                    WorkingDirectory = Path.Combine(projectRoot, "Content"),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                TemplateEngine.ConfigureDotnetPath(psi);

                using var process = Process.Start(psi);
                if (process == null)
                {
                    logCallback("Failed to start MGCB compilation process.");
                    return;
                }

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    logCallback("MGCB compilation completed successfully.");
                }
                else
                {
                    logCallback($"MGCB compilation failed with exit code {process.ExitCode}.");
                    if (!string.IsNullOrEmpty(output)) logCallback($"MGCB output: {output}");
                    if (!string.IsNullOrEmpty(error)) logCallback($"MGCB error: {error}");
                }
            }
            catch (Exception ex)
            {
                logCallback($"Error during MGCB compilation: {ex.Message}");
            }
        }

        private static string RemoveAssetFromMgcbString(string mgcbContent, string mgcbRelativePath)
        {
            string startTag = $"#begin {mgcbRelativePath}";
            string endTag = $"#end {mgcbRelativePath}";

            int startIndex = mgcbContent.IndexOf(startTag);
            if (startIndex >= 0)
            {
                int endIndex = mgcbContent.IndexOf(endTag, startIndex);
                if (endIndex >= 0)
                {
                    endIndex += endTag.Length;
                    // consume trailing newlines if any
                    while (endIndex < mgcbContent.Length && (mgcbContent[endIndex] == '\r' || mgcbContent[endIndex] == '\n'))
                    {
                        endIndex++;
                    }
                    mgcbContent = mgcbContent.Remove(startIndex, endIndex - startIndex);
                }
            }
            return mgcbContent;
        }
    }
}
