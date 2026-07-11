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
    }
}
