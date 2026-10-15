using System;

namespace TestConsole.Tests.Fixtures
{
    /// <summary>
    /// Contexto de áudio headless que simula relógio temporal virtual sem acoplar drivers de som.
    /// </summary>
    public static class HeadlessAudioContext
    {
        public static double VirtualTime { get; set; } = 0.0;

        public static void AdvanceTime(double deltaSeconds)
        {
            VirtualTime += deltaSeconds;
        }

        public static void Reset()
        {
            VirtualTime = 0.0;
        }
    }
}
