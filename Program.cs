using System;
using MonoKuratko.Logic;

namespace MonoKuratko
{
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            new Les(5).NaplnMapu("C:\\dev\\opengl_kuratko\\res\\xmlova.tmx");
            
            using (var game = new Game1())
            game.Run();
        }
    }
}
