using ItemDataBrowser.Commands.Base;
using ItemDataBrowser.Objects;
using static System.String;

namespace ItemDataBrowser.Commands
{
    internal class PrintHelpCommand : BaseCommand
    {
        public override string Name => "help";

        public override string Example => Empty;

        public override string Description => Empty;

        public override bool DisplayInHelp => false;

        public override void Run(CommandInfo _)
        {
            foreach (var command in CommandEngine.Commands.Where(c => c.DisplayInHelp).OrderBy(c => c.Name))
            {
                Console.WriteLine($"[{command.Name}]\t\t{command.Example}");

                if (!IsNullOrWhiteSpace(command.Description))
                    Console.WriteLine(command.Description);

                Console.WriteLine();
            }

            Console.WriteLine("[exit]\r\nClose this application.");
        }
    }
}
