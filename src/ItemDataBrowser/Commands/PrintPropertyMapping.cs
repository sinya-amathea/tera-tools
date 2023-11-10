using ItemDataBrowser.Commands.Base;
using ItemDataBrowser.Objects;
using static ItemDataBrowser.Core.Extensions.ConsoleExtensions;

namespace ItemDataBrowser.Commands
{
    internal class PrintPropertyMapping : BaseCommand
    {
        public override string Name => "map";

        public override string Example => "map[(name)]";

        public override string Description => "Get a list of filter property to xml attribute mapping (filter properties usually are just PascalCase'd attribute names.\r\n[Optional] specify a part of a filter name to filter the list.";

        public override bool DisplayInHelp => true;

        public override void Run(CommandInfo info)
        {
            if (info.Parameters.Any() && info.TryGetParameter(0, out string name))
            {
                var searchFor = name.ToLower();
                var filtered = Program.Data.ItemData.PropertyCache
                    .Where(c => c.Property.Name.ToLower().Contains(searchFor) ||
                                c.AttributeName.ToLower().Contains(searchFor))
                    .ToList();

                if (!filtered.Any())
                {
                    if (!AskYesNo("No such property found. Display the whole list?"))
                        return;
                }
                else
                {
                    foreach (var mapping in filtered)
                        Console.WriteLine($"[Help] {mapping.Property.Name} => {mapping.AttributeName}");

                    return;
                }
            }

            foreach (var mapping in Program.Data.ItemData.PropertyCache)
                Console.WriteLine($"[Help] {mapping.Property.Name} => {mapping.AttributeName}");
        }
    }
}
