using System;
using Microsoft.Xna.Framework.Graphics;

namespace TestConsole.Tests.Fixtures
{
    /// <summary>
    /// Fixture para testes em modo puramente headless, fornecendo Viewports de referência
    /// e validação de execução sem contexto de GPU física.
    /// </summary>
    public static class HeadlessGraphicsDeviceFixture
    {
        public static readonly Viewport Virtual1080pViewport = new Viewport(0, 0, 1920, 1080);
        public static readonly Viewport Letterbox4_3Viewport = new Viewport(0, 135, 1440, 810);
        public static readonly Viewport Ultrawide21_9Viewport = new Viewport(320, 0, 1920, 1080);

        public static void AssertSafeHeadlessExecution(Action action, string scenarioName)
        {
            try
            {
                action();
            }
            catch (NullReferenceException ex)
            {
                throw new InvalidOperationException(
                    $"[FALHA HEADLESS] O cenário '{scenarioName}' tentou acessar contexto de GPU físico: {ex.Message}");
            }
        }
    }
}
