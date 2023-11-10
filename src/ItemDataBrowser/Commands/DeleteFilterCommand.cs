using ItemDataBrowser.Commands.Base;
using ItemDataBrowser.Objects;

namespace ItemDataBrowser.Commands
{
    internal class DeleteFilterCommand : BaseCommand
    {
        public override string Name => "df";
        
        public override string Example => "df(name)";

        public override string Description => "Delete a filter by name.";

        public override bool DisplayInHelp => true;

        public override void Run(CommandInfo info)
        {
            if (!info.TryGetParameter(0, out string name))
                return;

            if (Program.Provider.Options.Filters.All(f => f.Name != name))
            {
                Console.WriteLine($"[Filter] No such filter found.");
                return;
            }

            Program.Provider.Options.Filters.RemoveAll(f => f.Name == name);
            Program.Provider.Save();
            Console.WriteLine($"[Filter] Removed '{name}'.");
        }
    }
}
