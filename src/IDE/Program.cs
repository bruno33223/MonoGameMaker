using System;

namespace MonoGameMaker.IDE
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            using (var game = new ToolEngine())
            {
                game.Run();
            }
        }
    }
}
