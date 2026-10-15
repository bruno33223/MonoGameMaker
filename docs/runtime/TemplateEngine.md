# `TemplateEngine` (Engine Scaffolding & Subsystem Synchronization)

O `TemplateEngine` é o subsistema de scaffolding e sincronização de projetos da engine MonoGameMaker. Ele é responsável por instanciar novos projetos MonoGame DesktopGL (.NET 8.0) completamente configurados de fábrica, injetando todos os subsistemas de simulação, particionamento espacial, pathfinding por campos de fluxo, visão/Fog of War, áudio DSP, shaders HLSL, renderização instanciada em lote e documentação atômica do framework.

---

## 1. Descrição de Alto Nível

Em arquiteturas extensíveis orientadas a agentes e desenvolvimento assistido por IA, projetos recém-criados frequentemente nasciam sem o ecossistema de bibliotecas ou quebravam compilações em sistemas Unix/Linux devido a dependências implícitas de fontes ou assets não instalados no sistema operacional.

O `TemplateEngine` resolve esse problema de forma definitiva e desacoplada:
1. **Descoberta Dinâmica da Engine (`FindEngineRootDirectory`)**: Localiza recursivamente a raiz de desenvolvimento da engine a partir do diretório base de execução, variáveis de ambiente ou diretório corrente de trabalho.
2. **Scaffolding Completo de Fábrica (`ScaffoldProjectAsync`)**: Invoca a CLI do .NET (`dotnet new mgdesktopgl`), reconfigura o `.csproj` com pacotes essenciais (`MonoGame.Framework.DesktopGL`, `ImGui.NET`, `NLayer`), diretivas de compilação sem unsafe/GC e cópia de shaders e layouts.
3. **Imunidade a Falhas de Build no Linux**: Não registra descritores de fonte externos (`default.spritefont` com Arial) no `Content.mgcb` por padrão, evitando `System.IO.FileNotFoundException: Could not find "Arial" font file` no pipeline MGCB. Em vez disso, conecta transparentemente o `BitmapFontRenderer` procedural embutido.
4. **Replicação Integral de Subsistemas e Conhecimento**: Clona automaticamente toda a árvore de runtime (`Runtime/Spatial`, `Runtime/Pathfinding`, `Runtime/Simulation`, `Runtime/Vision`, `Runtime/Selection`, `Runtime/Graphics3D`, `Runtime/Audio`, `Runtime/Pooling`, `Runtime/Resolution`, `Runtime/Typography`, `Runtime/Combat`, `Runtime/Shaders`), a base completa de documentação atômica (`docs/`) e as regras de governança para agentes de IA (`.agents/rules/monogamerules.md`).
5. **Sincronização Ativa em Tempo Real (`SyncRuntimeFiles`)**: Permite que o editor ou pipelines de CI/CD atualizem projetos existentes com as últimas assinaturas de runtime da engine sem intervenção manual.

---

## 2. Assinaturas Completas da API

```csharp
namespace MonoGameMaker.IDE.Core
{
    public static class TemplateEngine
    {
        /// <summary>
        /// Localiza recursivamente o diretório raiz da engine MonoGameMaker.
        /// Retorna a string do caminho absoluto se encontrado, ou null se inacessível.
        /// </summary>
        public static string? FindEngineRootDirectory();

        /// <summary>
        /// Copia recursivamente uma árvore de diretórios com suporte a filtros de exclusão.
        /// </summary>
        public static void CopyDirectoryRecursive(string sourceDir, string targetDir, HashSet<string>? excludeNames = null);

        /// <summary>
        /// Executa o scaffolding assíncrono completo de um novo projeto pronto para compilação.
        /// </summary>
        public static Task<bool> ScaffoldProjectAsync(string targetDirectory, string projectName, Action<string> logCallback);

        /// <summary>
        /// Sincroniza e atualiza os arquivos de runtime, módulos, shaders e documentação de um projeto existente.
        /// </summary>
        public static void SyncRuntimeFiles(string targetDirectory);

        /// <summary>
        /// Retorna o executável dotnet adequado para o ambiente de execução corrente.
        /// </summary>
        public static string GetDotnetPath();

        /// <summary>
        /// Configura a variável de ambiente PATH para o processo disparado a partir da CLI do dotnet.
        /// </summary>
        public static void ConfigureDotnetPath(ProcessStartInfo psi);

        /// <summary>
        /// Retorna o template C# padrão para novos scripts de entidade (EntityBehavior).
        /// </summary>
        public static string GetNewScriptTemplate(string projectName, string className);

        /// <summary>
        /// Retorna o manifesto arquitetural de instruções para agentes de inteligência artificial (.cursorrules / AI_ARCHITECTURE.md).
        /// </summary>
        public static string GetAiManifestTemplate(string projectName);
    }
}
```

---

## 3. Casos de Uso (Contexto para IAs)

- **Criação de Novos Jogos no IDE**: Invocado automaticamente pelo modal de criação de projetos (`ToolEngine`) para gerar um jogo 100% autossuficiente e funcional out-of-the-box.
- **Sessão Zerada de IA para Jogos Complexos (ex: RTS / Starcraft)**: Uma IA que abre a pasta de um projeto recém-criado já encontra todos os módulos (`SpatialHashGrid`, `FlowFieldManager`, `FogOfWarSystem`, `RTSSelectionManager`, `InstancedUnitRenderer`), a documentação completa em `docs/` e o `.agents/rules/monogamerules.md`, permitindo começar imediatamente o desenvolvimento de regras de negócio sem precisar reimplementar o framework ou rodar scripts de migração.
- **Automação de CI / Testes Headless**: Utilizado por rotinas de teste para instanciar projetos limpos temporários em sandbox, compilar via CLI e validar ausência de regressões no pipeline.

---

## 4. Exemplo Prático e Compilável

O exemplo a seguir demonstra a invocação programática do `TemplateEngine` para criar um novo projeto, sincronizar seus subsistemas e compilar em modo Release com verificação de sucesso:

```csharp
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using MonoGameMaker.IDE.Core;

namespace ScaffoldingExample
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            string projectFolder = Path.Combine(Path.GetTempPath(), "OutconquerRTSGame");
            string projectName = "Outconquer";

            Console.WriteLine($"[1/3] Criando projeto '{projectName}' em '{projectFolder}'...");
            bool scaffolded = await TemplateEngine.ScaffoldProjectAsync(
                projectFolder, 
                projectName, 
                msg => Console.WriteLine($"  -> {msg}")
            );

            if (!scaffolded)
            {
                Console.WriteLine("ERRO: Falha ao executar ScaffoldProjectAsync.");
                return;
            }

            Console.WriteLine("[2/3] Sincronizando subsistemas adicionais de runtime...");
            TemplateEngine.SyncRuntimeFiles(projectFolder);

            Console.WriteLine("[3/3] Compilando projeto scaffolded via dotnet CLI...");
            string csprojPath = Path.Combine(projectFolder, $"{projectName}.csproj");
            var psi = new ProcessStartInfo
            {
                FileName = TemplateEngine.GetDotnetPath(),
                Arguments = $"build \"{csprojPath}\" -c Release",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            TemplateEngine.ConfigureDotnetPath(psi);

            using var process = Process.Start(psi)!;
            string stdout = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                Console.WriteLine("SUCESSO: Projeto criado, sincronizado e compilado com 0 erros e 0 avisos!");
            }
            else
            {
                Console.WriteLine($"FALHA: Erro de compilação (Código {process.ExitCode}):\n{stdout}");
            }
        }
    }
}
```
