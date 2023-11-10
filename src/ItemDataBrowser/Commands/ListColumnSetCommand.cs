using ItemDataBrowser.Commands.Base;
using ItemDataBrowser.Objects;

namespace ItemDataBrowser.Commands
{
    internal class ListColumnSetCommand : BaseCommand
    {
        public override string Name => "lc";

        public override string Example => "lc";
        
        public override string Description => "List all saved column sets.";

        public override bool DisplayInHelp => true;

        public override void Run(CommandInfo info)
        {
            if (!Program.Provider.Options.ColumnSets.Any())
            {
                Console.WriteLine($"[Help] No column sets found");
                return;
            }

            foreach (var column in Program.Provider.Options.ColumnSets)
                Console.WriteLine($"[Help] {column.Name} => {column.List.Aggregate((current, next) => $"{current}, {next}")}");
        }
    }
}
