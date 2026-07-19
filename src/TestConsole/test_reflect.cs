using System;
using System.Reflection;
using Microsoft.Xna.Framework.Input;

namespace TestReflect {
    class Program {
        static void Main() {
            var asm = Assembly.LoadFrom(@"C:\Users\f70432d\Documents\MonoGameProjects\FlappyBird_V2\bin\Debug\net8.0\FlappyBird_V2.dll");
            var kbType = asm.GetType("MonoGameMaker.Runtime.Keyboard");
            Console.WriteLine("Keyboard Type: " + (kbType != null));
            var m = kbType?.GetMethod("SetState", BindingFlags.Public | BindingFlags.Static);
            Console.WriteLine("Method: " + (m != null));
            if (m != null) {
                var state = new KeyboardState(Keys.Space);
                m.Invoke(null, new object[] { state });
                Console.WriteLine("Invoke successful!");
                
                var getState = kbType.GetMethod("GetState", BindingFlags.Public | BindingFlags.Static);
                var res = (KeyboardState)getState.Invoke(null, null);
                Console.WriteLine("Is Space Down: " + res.IsKeyDown(Keys.Space));
            }
        }
    }
}
