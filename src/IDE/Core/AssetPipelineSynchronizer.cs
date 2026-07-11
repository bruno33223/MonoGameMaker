using System;
using System.IO;
using System.Text;

namespace MonoGameMaker.IDE.Core
{
    public static class AssetPipelineSynchronizer
    {
        public static bool RegisterAsset(string projectRoot, string sourceFilePath, string assetType, Action<string> logCallback)
        {
            try
            {
                string fileName = Path.GetFileName(sourceFilePath);
                string normalizedType = assetType.ToLower() switch
                {
                    "sprites" or "backgrounds" or "textures" => "Textures",
                    "sounds" or "audio" => "Audio",
                    "models" => "Models",
                    _ => throw new ArgumentException($"Unknown asset type: {assetType}")
                };

                string destDir = Path.Combine(projectRoot, "Content", normalizedType);
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                string destFilePath = Path.Combine(destDir, fileName);
                
                // Copy the file physically
                File.Copy(sourceFilePath, destFilePath, overwrite: true);
                logCallback($"Copied asset to physical path: {destFilePath}");

                // Register in MGCB
                string mgcbPath = Path.Combine(projectRoot, "Content", "Content.mgcb");
                if (!File.Exists(mgcbPath))
                {
                    logCallback($"Warning: Content.mgcb not found at {mgcbPath}. Skipping registration.");
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

                return true;
            }
            catch (Exception ex)
            {
                logCallback($"Error registering asset: {ex.Message}");
                return false;
            }
        }

        public static bool UnregisterAsset(string projectRoot, string relativePath, Action<string> logCallback)
        {
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

                return true;
            }
            catch (Exception ex)
            {
                logCallback($"Error unregistering asset: {ex.Message}");
                return false;
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
