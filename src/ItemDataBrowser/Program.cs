using ItemDataBrowser.Commands.Base;
using ItemDataBrowser.Core;
using static System.String;
using static ItemDataBrowser.Core.Extensions.ConsoleExtensions;

namespace ItemDataBrowser
{
    internal class Program
    {
        internal static readonly CommandEngine Command = new();
        internal static readonly SettingsProvider Provider = new();
        internal static readonly DataProvider Data = new();
        internal static readonly FilterBuilder Filters = new();
        
        static void Main(string[] args)
        {
            Console.Title = "TERA ItemData Browser (v 0.3)";
            Provider.Load();

            if (Provider.Options.AutoFullScreen)
                SetFullscreen();

            if (!Command.Initialize())
            {
                Console.ReadLine();
                return;
            }

            Data.Load();
            MainLoop();
        }
        
        static void MainLoop()
        {
            do
            {
                Console.WriteLine();
                Console.WriteLine("Enter command:");
                
                var command = Console.ReadLine() ?? Empty;

                if (command == "exit") 
                    return;

                Command.Execute(command);
            } while (true);
        }
    }
}