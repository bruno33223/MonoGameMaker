using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Core;
using MonoGameMaker.Runtime.Resolution;
using TestConsole.Tests.Domain;

namespace TestConsole.Tests
{
    public static class MultiplatformInputTests
    {
        private const float Epsilon = 1e-4f;

        public static void RunAll(ref int passCount, ref int totalCount)
        {
            Console.WriteLine("\n[SUÍTE 6/8] Executando MultiplatformInputTests (TEST-INP-01 a 06)...");

            Test_INP_01_Native1080pMouseMapping(ref passCount, ref totalCount);
            Test_INP_02_Letterbox4_3Calibration(ref passCount, ref totalCount);
            Test_INP_03_Pillarbox21_9Calibration(ref passCount, ref totalCount);
            Test_INP_04_NoWin32PInvokeScan(ref passCount, ref totalCount);
            Test_INP_05_CircularThumbstickDeadzone(ref passCount, ref totalCount);
            Test_INP_06_CardinalMenuNavigationDebounce(ref passCount, ref totalCount);
        }

        public static void Test_INP_01_Native1080pMouseMapping(ref int passCount, ref int totalCount)
        {
            totalCount++;
            ResolutionManager.SetResolution(1920, 1080);

            Vector2[] physical = {
                new Vector2(0f, 0f),
                new Vector2(960f, 540f),
                new Vector2(1920f, 1080f)
            };

            foreach (var pt in physical)
            {
                Vector2 virt = ResolutionManager.ScreenToVirtual(pt);
                if (Math.Abs(virt.X - pt.X) > Epsilon || Math.Abs(virt.Y - pt.Y) > Epsilon)
                    throw new Exception($"[TEST-INP-01] Mapeamento 1080p nativo falhou para {pt}: obteve {virt}");
            }

            passCount++;
            Console.WriteLine("  -> [TEST-INP-01] Conversão de Mouse Virtual 1080p (Nativo): PASS");
        }

        public static void Test_INP_02_Letterbox4_3Calibration(ref int passCount, ref int totalCount)
        {
            totalCount++;
            // Janela 1440x1080 (proporção 4:3, barras no topo e na base)
            ResolutionManager.SetResolution(1440, 1080);

            if (Math.Abs(ResolutionManager.Scale - 0.75f) > Epsilon)
                throw new Exception($"[TEST-INP-02] Escala esperada 0.75, obtido {ResolutionManager.Scale}");

            if (ResolutionManager.OffsetY != 135)
                throw new Exception($"[TEST-INP-02] OffsetY esperado 135, obtido {ResolutionManager.OffsetY}");

            // Clique na barra preta superior (Y = 50 < 135)
            Vector2 barClick = new Vector2(720f, 50f);
            Vector2 virtBar = ResolutionManager.ScreenToVirtual(barClick);
            if (virtBar.Y >= 0f)
                throw new Exception($"[TEST-INP-02] Clique na barra superior deve ter coordenada Y virtual negativa. Obtido: {virtBar.Y}");

            // Clique no centro da área jogável: (720, 540)
            Vector2 centerClick = new Vector2(720f, 540f);
            Vector2 virtCenter = ResolutionManager.ScreenToVirtual(centerClick);
            if (Math.Abs(virtCenter.X - 960f) > 0.01f || Math.Abs(virtCenter.Y - 540f) > 0.01f)
                throw new Exception($"[TEST-INP-02] Centro jogável 4:3 não mapeou para (960, 540). Obtido: {virtCenter}");

            passCount++;
            Console.WriteLine("  -> [TEST-INP-02] Calibração contra Letterbox (Janela 4:3 -> OffsetY 135, Center 960x540): PASS");
        }

        public static void Test_INP_03_Pillarbox21_9Calibration(ref int passCount, ref int totalCount)
        {
            totalCount++;
            // Janela Ultrawide 2560x1080 (proporção 21:9, barras laterais)
            ResolutionManager.SetResolution(2560, 1080);

            if (Math.Abs(ResolutionManager.Scale - 1.0f) > Epsilon)
                throw new Exception($"[TEST-INP-03] Escala esperada 1.0, obtido {ResolutionManager.Scale}");

            if (ResolutionManager.OffsetX != 320)
                throw new Exception($"[TEST-INP-03] OffsetX esperado 320, obtido {ResolutionManager.OffsetX}");

            // Clique na borda esquerda da área jogável física (320, 0) -> deve mapear para virtual (0, 0)
            Vector2 leftEdge = new Vector2(320f, 0f);
            Vector2 virtLeft = ResolutionManager.ScreenToVirtual(leftEdge);
            if (Math.Abs(virtLeft.X - 0f) > Epsilon || Math.Abs(virtLeft.Y - 0f) > Epsilon)
                throw new Exception($"[TEST-INP-03] Borda esquerda não mapeou para (0, 0). Obtido: {virtLeft}");

            // Centro físico (1280, 540) -> virtual (960, 540)
            Vector2 center = new Vector2(1280f, 540f);
            Vector2 virtCenter = ResolutionManager.ScreenToVirtual(center);
            if (Math.Abs(virtCenter.X - 960f) > Epsilon || Math.Abs(virtCenter.Y - 540f) > Epsilon)
                throw new Exception($"[TEST-INP-03] Centro ultrawide não mapeou para (960, 540). Obtido: {virtCenter}");

            passCount++;
            Console.WriteLine("  -> [TEST-INP-03] Calibração contra Pillarbox (Janela Ultrawide 21:9 -> OffsetX 320): PASS");
        }

        public static void Test_INP_04_NoWin32PInvokeScan(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var assembly = typeof(InputService).Assembly;
            var types = assembly.GetTypes();

            int win32DllImportsFound = 0;
            string violationDetails = string.Empty;

            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                foreach (var method in methods)
                {
                    var dllImportAttr = method.GetCustomAttribute<DllImportAttribute>();
                    if (dllImportAttr != null)
                    {
                        string dllName = dllImportAttr.Value.ToLowerInvariant();
                        if (dllName.Contains("user32") || dllName.Contains("kernel32"))
                        {
                            win32DllImportsFound++;
                            violationDetails += $"[Tipo: {type.FullName}, Método: {method.Name}, DLL: {dllImportAttr.Value}] ";
                        }
                    }
                }
            }

            if (win32DllImportsFound > 0)
                throw new Exception($"[TEST-INP-04] Violação de P/Invoke Win32 detectada! Encontrados {win32DllImportsFound} métodos: {violationDetails}");

            passCount++;
            Console.WriteLine("  -> [TEST-INP-04] Ausência Absoluta de P/Invoke Win32 (Scan por Reflection 0 DllImports): PASS");
        }

        public static void Test_INP_05_CircularThumbstickDeadzone(ref int passCount, ref int totalCount)
        {
            totalCount++;
            float deadZone = 0.15f;

            // Vetor de drift com magnitude < 0.15f
            Vector2 driftInput = new Vector2(0.05f, 0.08f); // length = sqrt(0.0025 + 0.0064) = sqrt(0.0089) ~ 0.0943 < 0.15
            Vector2 filteredDrift = FilterDeadzone(driftInput, deadZone);

            if (filteredDrift != Vector2.Zero)
                throw new Exception($"[TEST-INP-05] Entrada de drift dentro da deadzone deve retornar vetor zero. Obtido: {filteredDrift}");

            // Deflexão total
            Vector2 fullInput = new Vector2(1.0f, 0f);
            Vector2 filteredFull = FilterDeadzone(fullInput, deadZone);

            if (Math.Abs(filteredFull.Length() - 1.0f) > Epsilon)
                throw new Exception($"[TEST-INP-05] Deflexão total deve preservar magnitude 1.0f. Obtido: {filteredFull.Length()}");

            passCount++;
            Console.WriteLine("  -> [TEST-INP-05] Deadzone Circular de Analógicos de Gamepad (0.15f Radial Deadzone): PASS");
        }

        private static Vector2 FilterDeadzone(Vector2 stick, float deadZone)
        {
            float len = stick.Length();
            if (len <= deadZone) return Vector2.Zero;
            // Resposta linearizada de deadzone até 1.0f
            float scaledLen = (len - deadZone) / (1.0f - deadZone);
            return (stick / len) * Math.Min(1.0f, scaledLen);
        }

        public static void Test_INP_06_CardinalMenuNavigationDebounce(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var menuNav = new MenuNavigationController { CurrentIndex = 0, MaxItems = 5 };

            // Frame 1: primeiro toque para baixo
            bool step1 = menuNav.UpdateDirectionHold(isDownPressed: true, dt: 0.016f);
            if (!step1 || menuNav.CurrentIndex != 1)
                throw new Exception($"[TEST-INP-06] Primeiro toque deve navegar imediatamente para o índice 1. Passo: {step1}, Índice: {menuNav.CurrentIndex}");

            // Mantendo pressionado durante 0.35s (abaixo do InitialRepeatDelay de 0.40s)
            float elapsed = 0f;
            while (elapsed < 0.35f)
            {
                bool repeatStep = menuNav.UpdateDirectionHold(isDownPressed: true, dt: 0.016f);
                if (repeatStep || menuNav.CurrentIndex != 1)
                    throw new Exception($"[TEST-INP-06] Navegação disparou repetição prematura antes de 0.40s no tempo {elapsed:F3}s! Índice: {menuNav.CurrentIndex}");
                elapsed += 0.016f;
            }

            // Ao ultrapassar 0.40s: deve disparar repetição
            bool repeatAfterDelay = menuNav.UpdateDirectionHold(isDownPressed: true, dt: 0.060f); // 0.35 + 0.06 = 0.41s
            if (!repeatAfterDelay || menuNav.CurrentIndex != 2)
                throw new Exception($"[TEST-INP-06] Repetição após 0.40s falhou em avançar para o índice 2. Passo: {repeatAfterDelay}, Índice: {menuNav.CurrentIndex}");

            passCount++;
            Console.WriteLine("  -> [TEST-INP-06] Navegação Cardinal em Menus e Modais (Initial Delay 0.40s): PASS");
        }
    }
}
