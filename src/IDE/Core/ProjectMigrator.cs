using System;
using System.IO;
using System.Text.RegularExpressions;
using MonoGameMaker.IDE.Core;

namespace MonoGameMaker.IDE
{
    public static class ProjectMigrator
    {
        public static void Shift(string projectRoot, Action<string> logCallback)
        {
            Migrate(projectRoot, logCallback);
        }

        public static void Migrate(string projectRoot, Action<string> logCallback)
        {
            try
            {
                logCallback($"Starting project migration check for: {projectRoot}");

                // Locate csproj
                string[] csprojs = Directory.GetFiles(projectRoot, "*.csproj");
                if (csprojs.Length == 0)
                {
                    logCallback("Migration: No .csproj file found. Skipping csproj migration.");
                }
                else
                {
                    string csprojPath = csprojs[0];
                    MigrateCsproj(csprojPath, logCallback);
                }

                // Update IA manifests
                string projectName = csprojs.Length > 0 ? Path.GetFileNameWithoutExtension(csprojs[0]) : "Project";
                MigrateAiManifests(projectRoot, projectName, logCallback);

                // Migrate Game1.cs to use SafeContentManager
                MigrateGame1(projectRoot, logCallback);

                // Sanitize and repair corrupted spritefonts
                SanitizeSpriteFonts(projectRoot, logCallback);

                // Migrate legacy scripts
                MigrateScripts(projectRoot, logCallback);

                logCallback("Migration process completed.");
            }
            catch (Exception ex)
            {
                logCallback($"Migration failed: {ex.Message}");
            }
        }

        private static void MigrateCsproj(string csprojPath, Action<string> logCallback)
        {
            try
            {
                string content = File.ReadAllText(csprojPath);
                bool modified = false;

                // 1. Check <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
                if (!content.Contains("<AllowUnsafeBlocks>true</AllowUnsafeBlocks>", StringComparison.OrdinalIgnoreCase))
                {
                    // Find the first <PropertyGroup> and insert it
                    int index = content.IndexOf("<PropertyGroup>", StringComparison.OrdinalIgnoreCase);
                    if (index >= 0)
                    {
                        int insertIndex = index + "<PropertyGroup>".Length;
                        content = content.Insert(insertIndex, "\n    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>");
                        modified = true;
                        logCallback("Migration: Injected <AllowUnsafeBlocks>true</AllowUnsafeBlocks> into csproj.");
                    }
                }

                // 2. Check <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
                if (!content.Contains("<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>", StringComparison.OrdinalIgnoreCase))
                {
                    // Find the first <PropertyGroup> and insert it
                    int index = content.IndexOf("<PropertyGroup>", StringComparison.OrdinalIgnoreCase);
                    if (index >= 0)
                    {
                        int insertIndex = index + "<PropertyGroup>".Length;
                        content = content.Insert(insertIndex, "\n    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>");
                        modified = true;
                        logCallback("Migration: Injected <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies> into csproj.");
                    }
                }

                // 3. Check for ImGui.NET PackageReference
                if (!content.Contains("PackageReference Include=\"ImGui.NET\"", StringComparison.OrdinalIgnoreCase))
                {
                    // Find the first <ItemGroup> or create one
                    int index = content.IndexOf("<ItemGroup>", StringComparison.OrdinalIgnoreCase);
                    if (index >= 0)
                    {
                        int insertIndex = index + "<ItemGroup>".Length;
                        content = content.Insert(insertIndex, "\n    <PackageReference Include=\"ImGui.NET\" Version=\"1.91.6.1\" />");
                        modified = true;
                        logCallback("Migration: Injected ImGui.NET package reference into csproj.");
                    }
                    else
                    {
                        // No <ItemGroup> found, let's add one before the final </Project> tag
                        int projectEndIndex = content.LastIndexOf("</Project>", StringComparison.OrdinalIgnoreCase);
                        if (projectEndIndex >= 0)
                        {
                            content = content.Insert(projectEndIndex, "  <ItemGroup>\n    <PackageReference Include=\"ImGui.NET\" Version=\"1.91.6.1\" />\n  </ItemGroup>\n");
                            modified = true;
                            logCallback("Migration: Created <ItemGroup> with ImGui.NET package reference in csproj.");
                        }
                    }
                }

                if (modified)
                {
                    File.WriteAllText(csprojPath, content);
                }
            }
            catch (Exception ex)
            {
                logCallback($"Migration Error: Failed to migrate .csproj: {ex.Message}");
            }
        }

        private static void MigrateAiManifests(string projectRoot, string projectName, Action<string> logCallback)
        {
            try
            {
                string manifestTemplate = TemplateEngine.GetAiManifestTemplate(projectName);
                
                string cursorRulesPath = Path.Combine(projectRoot, ".cursorrules");
                File.WriteAllText(cursorRulesPath, manifestTemplate);
                logCallback("Migration: Overwrote .cursorrules manifest.");

                string aiArchPath = Path.Combine(projectRoot, "AI_ARCHITECTURE.md");
                File.WriteAllText(aiArchPath, manifestTemplate);
                logCallback("Migration: Overwrote AI_ARCHITECTURE.md manifest.");
            }
            catch (Exception ex)
            {
                logCallback($"Migration Error: Failed to overwrite IA manifests: {ex.Message}");
            }
        }

        private static void MigrateScripts(string projectRoot, Action<string> logCallback)
        {
            try
            {
                string scriptsDir = Path.Combine(projectRoot, "Scripts");
                if (!Directory.Exists(scriptsDir))
                {
                    return;
                }

                string[] csFiles = Directory.GetFiles(scriptsDir, "*.cs", SearchOption.AllDirectories);
                foreach (string file in csFiles)
                {
                    try
                    {
                        string content = File.ReadAllText(file);
                        bool modified = false;

                        // Replace legacy IEntityScript with EntityBehavior
                        if (content.Contains(": IEntityScript", StringComparison.Ordinal))
                        {
                            content = content.Replace(": IEntityScript", ": EntityBehavior");
                            modified = true;
                            logCallback($"Migration: Replaced IEntityScript inheritance in script: {Path.GetFileName(file)}");
                        }

                        // Replace using statements if they refer to runtime namespace (usually namespace is already correct, but let's check)
                        if (!content.Contains("using MonoGameMaker.Runtime;", StringComparison.Ordinal) && content.Contains("EntityBehavior", StringComparison.Ordinal))
                        {
                            content = "using MonoGameMaker.Runtime;\n" + content;
                            modified = true;
                        }

                        // Replace public void Initialize(GameEntity entity, Dictionary<string, string> properties) with public override void Awake()
                        // And adjust internal references.
                        // Let's use a regex to match:
                        // public void Initialize\s*\(\s*GameEntity\s+(\w+)\s*,\s*Dictionary\s*<\s*string\s*,\s*string\s*>\s+(\w+)\s*\)
                        string pattern = @"public\s+void\s+Initialize\s*\(\s*GameEntity\s+(\w+)\s*,\s*Dictionary\s*<\s*string\s*,\s*string\s*>\s+(\w+)\s*\)";
                        var match = Regex.Match(content, pattern);
                        if (match.Success)
                        {
                            string entityVarName = match.Groups[1].Value;
                            string propertiesVarName = match.Groups[2].Value;

                            // 1. Replace the signature with public override void Awake()
                            content = Regex.Replace(content, pattern, "public override void Awake()");

                            // 2. Since Entity and Properties are now native fields of EntityBehavior (base.Entity and base.Properties),
                            // we need to replace occurrences of the parameter variables:
                            // Replace entityVarName with Entity, but only when it is used as a standalone word (to avoid matching e.g. playerEntity)
                            // Except if the var name is already Entity.
                            if (entityVarName != "Entity")
                            {
                                content = Regex.Replace(content, @"\b" + entityVarName + @"\b", "Entity");
                            }
                            if (propertiesVarName != "Properties")
                            {
                                content = Regex.Replace(content, @"\b" + propertiesVarName + @"\b", "Properties");
                            }

                            modified = true;
                            logCallback($"Migration: Refactored legacy Initialize method to Awake() in: {Path.GetFileName(file)}");
                        }

                        if (modified)
                        {
                            File.WriteAllText(file, content);
                        }
                    }
                    catch (Exception ex)
                    {
                        logCallback($"Migration Warning: Failed to process script {Path.GetFileName(file)}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                logCallback($"Migration Error: Failed to scan or process scripts: {ex.Message}");
            }
        }

        private static void MigrateGame1(string projectRoot, Action<string> logCallback)
        {
            try
            {
                string game1Path = Path.Combine(projectRoot, "Game1.cs");
                if (!File.Exists(game1Path))
                {
                    return;
                }

                string content = File.ReadAllText(game1Path);
                bool modified = false;

                // 1. If it doesn't contain SafeContentManager, append it to the end before the last closing brace
                if (!content.Contains("class SafeContentManager", StringComparison.OrdinalIgnoreCase))
                {
                    int lastBrace = content.LastIndexOf('}');
                    if (lastBrace >= 0)
                    {
                        string safeContentManagerClass = @"
    public class SafeContentManager : Microsoft.Xna.Framework.Content.ContentManager
    {
        public SafeContentManager(System.IServiceProvider serviceProvider, string rootDirectory) 
            : base(serviceProvider, rootDirectory)
        {
        }

        protected override System.IO.Stream OpenStream(string assetName)
        {
            string baseDir = System.AppDomain.CurrentDomain.BaseDirectory;
            string path = System.IO.Path.Combine(baseDir, RootDirectory, assetName) + "".xnb"";
            if (!System.IO.File.Exists(path))
            {
                return base.OpenStream(assetName);
            }
            try
            {
                byte[] bytes = System.IO.File.ReadAllBytes(path);
                return new System.IO.MemoryStream(bytes);
            }
            catch
            {
                return base.OpenStream(assetName);
            }
        }
    }
";
                        content = content.Insert(lastBrace, safeContentManagerClass);
                        modified = true;
                        logCallback("Migration: Appended SafeContentManager helper to Game1.cs.");
                    }
                }

                // 2. If it contains Content.RootDirectory = "Content"; or similar, replace it with Content = new SafeContentManager(Services, "Content");
                if (content.Contains("Content.RootDirectory = \"Content\";") || content.Contains("Content.RootDirectory = \"\"Content\"\";"))
                {
                    content = content.Replace("Content.RootDirectory = \"Content\";", "Content = new SafeContentManager(Services, \"Content\");");
                    content = content.Replace("Content.RootDirectory = \"\"Content\"\";", "Content = new SafeContentManager(Services, \"\"Content\"\");");
                    modified = true;
                    logCallback("Migration: Updated Game1 constructor to instantiate SafeContentManager.");
                }
                else if (content.Contains("Content.RootDirectory = ") && !content.Contains("new SafeContentManager"))
                {
                    content = Regex.Replace(content, @"Content\.RootDirectory\s*=\s*""Content"";", "Content = new SafeContentManager(Services, \"Content\");");
                    content = Regex.Replace(content, @"Content\.RootDirectory\s*=\s*""""Content"""";", "Content = new SafeContentManager(Services, \"Content\");");
                    modified = true;
                    logCallback("Migration: Updated Game1 constructor to instantiate SafeContentManager.");
                }

                if (modified)
                {
                    File.WriteAllText(game1Path, content);
                }
            }
            catch (Exception ex)
            {
                logCallback($"Migration Error: Failed to migrate Game1.cs: {ex.Message}");
            }
        }

        private static void SanitizeSpriteFonts(string projectRoot, Action<string> logCallback)
        {
            try
            {
                string contentDir = Path.Combine(projectRoot, "Content");
                if (!Directory.Exists(contentDir)) return;

                string[] spriteFontFiles = Directory.GetFiles(contentDir, "*.spritefont", SearchOption.AllDirectories);
                foreach (string file in spriteFontFiles)
                {
                    try
                    {
                        string xml = File.ReadAllText(file);
                        bool modified = false;

                        // Repair <Start> tag if it contains literal space or is empty/whitespace
                        var startMatch = Regex.Match(xml, @"<Start>(.*?)</Start>");
                        if (startMatch.Success)
                        {
                            string val = startMatch.Groups[1].Value;
                            if (string.IsNullOrWhiteSpace(val) || val == " ")
                            {
                                xml = Regex.Replace(xml, @"<Start>.*?</Start>", "<Start>&#32;</Start>");
                                modified = true;
                            }
                        }

                        // Repair <End> tag if it is empty/whitespace
                        var endMatch = Regex.Match(xml, @"<End>(.*?)</End>");
                        if (endMatch.Success)
                        {
                            string val = endMatch.Groups[1].Value;
                            if (string.IsNullOrWhiteSpace(val))
                            {
                                xml = Regex.Replace(xml, @"<End>.*?</End>", "<End>&#126;</End>");
                                modified = true;
                            }
                        }

                        if (modified)
                        {
                            File.WriteAllText(file, xml);
                            logCallback($"Migration: Sanitized and repaired corrupted spritefont XML: {Path.GetFileName(file)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        logCallback($"Migration Warning: Failed to sanitize spritefont {Path.GetFileName(file)}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                logCallback($"Migration Error: Failed to scan or sanitize spritefonts: {ex.Message}");
            }
        }
    }
}
