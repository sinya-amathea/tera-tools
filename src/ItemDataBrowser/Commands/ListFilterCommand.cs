using ItemDataBrowser.Commands.Base;
using ItemDataBrowser.Objects;

namespace ItemDataBrowser.Commands
{
    internal class ListFilterCommand : BaseCommand
    {
        public override string Name => "lf";

        public override string Example => "lf";

        public override string Description => "Lists all saved filters.";

        public override bool DisplayInHelp => true;
        
        public override void Run(CommandInfo _)
        {
            if (!Program.Provider.Options.Filters.Any())
            {
                Console.WriteLine("[Help] No filters found");
                return;
            }

            foreach (var filter in Program.Provider.Options.Filters)
                Console.WriteLine($"[Help] {filter.Name} => {filter.Value}");
        }
    }
}
