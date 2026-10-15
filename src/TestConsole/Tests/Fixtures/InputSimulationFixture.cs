using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace TestConsole.Tests.Fixtures
{
    /// <summary>
    /// Fixture para injeção programática de estados de mouse, teclado e gamepad.
    /// </summary>
    public class InputSimulationFixture
    {
        public Point SimulatedMouseScreenPos { get; set; }
        public Vector2 SimulatedThumbstickLeft { get; set; }
        public List<Keys> SimulatedDownKeys { get; } = new List<Keys>();

        public void InjectKeys(params Keys[] keys)
        {
            SimulatedDownKeys.Clear();
            SimulatedDownKeys.AddRange(keys);
        }

        public void Clear()
        {
            SimulatedDownKeys.Clear();
            SimulatedThumbstickLeft = Vector2.Zero;
            SimulatedMouseScreenPos = Point.Zero;
        }
    }
}
