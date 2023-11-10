using System.Runtime.InteropServices;
using static System.String;

namespace ItemDataBrowser.Core.Extensions
{
    internal static class ConsoleExtensions
    {
        public static bool AskYesNo(string? message = null)
        {
            message ??= "Proceed?";

            Console.WriteLine($"{message} (y/n)");

            var input = Console.ReadLine()?.ToLower() ?? Empty;

            return input == "y";
        }

        public static void WriteToMultipleLines(string input)
        {
            const int maxLength = 100;

            if (input.Length <= maxLength)
            {
                Console.WriteLine(input);
                return;
            }

            var words = input.Split(' ');
            var currentLength = 0;
            var tmpOut = Empty;

            foreach (var word in words)
            {
                if (currentLength + word.Length > maxLength)
                {
                    Console.WriteLine(tmpOut);
                    currentLength = 0;
                    tmpOut = Empty;
                }

                tmpOut += currentLength > 0 ? $" {word}" : word;
                currentLength += word.Length;
            }

            if (currentLength > 0)
                Console.WriteLine(tmpOut);
        }

        public static void SetFullscreen() => ShowWindow(GetConsoleWindow(), 3);


        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}
