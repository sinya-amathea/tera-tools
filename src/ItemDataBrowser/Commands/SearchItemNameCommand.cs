using ItemDataBrowser.Objects;
using Sinya.Tera.Shared.Schema;
using System.Text;
using static System.String;
using static ItemDataBrowser.Core.Extensions.ConsoleExtensions;
using ItemDataBrowser.Commands.Base;

namespace ItemDataBrowser.Commands
{
    internal class SearchItemNameCommand : BaseCommand
    {
        public override string Name { get; }
        
        public override string Example { get; }
        
        public override string Description { get; }
        
        public override bool DisplayInHelp { get; }
        
        public override void Run(CommandInfo info)
        {
            if (!info.TryGetParameter(0, out string value))
                return;

            var includeTooltip = false;

            if (info.HasParameter(1) && info.TryGetParameter(1, out string tmp))
                includeTooltip = tmp.ToLower() == "true";

            List<StringSheetItem> strings;

            if (includeTooltip)
            {
                // no word splitting here, filter engine cant handle prioritized filtering
                // example '(Name?=word1 & Name?=word2) | (ToolTip?=word1 & ToolTip?=word2)'
                if (value.StartsWith("\"") && value.EndsWith("\""))
                    value = value.Replace("\"", "");

                strings = Program.Data.ItemStrings.Cache
                    .Where(x => x.Name.Contains(value, StringComparison.InvariantCultureIgnoreCase) ||
                                (!IsNullOrWhiteSpace(x.ToolTip) && x.ToolTip.Contains(value, StringComparison.InvariantCultureIgnoreCase)))
                    .ToList();
            }
            else
            {
                if (value.StartsWith("\"") && value.EndsWith("\""))
                {
                    strings = Program.Data.ItemStrings.Cache
                        .Where(x => x.Name.Contains(value.Replace("\"", ""), StringComparison.InvariantCultureIgnoreCase))
                        .ToList();
                }
                else
                {
                    var filterString = Empty;
                    var words = value.Split(' ');

                    for (var i = 0; i < words.Length; i++)
                    {
                        var word = words[i];

                        filterString += $"Name?={word}";

                        if (i < words.Length - 1)
                            filterString += " & ";
                    }

                    var filter = Program.Filters.Build<StringSheetItem>(filterString);

                    if (filter == null)
                    {
                        Console.WriteLine($"[Error] Could not build filter.");
                        return;
                    }

                    strings = Program.Data.ItemStrings.Cache
                        .Where(filter.Compile())
                        .ToList();
                }
            }

            if (!strings.Any())
            {
                Console.WriteLine("[Search] No results found");
                return;
            }

            foreach (var itemString in strings)
            {
                Console.WriteLine($"[{itemString.Id}] {itemString.Name}");

                if (!IsNullOrWhiteSpace(itemString.ToolTip) && includeTooltip)
                    WriteToMultipleLines(itemString.ToolTip);

                Console.WriteLine();
            }

            if (AskYesNo("Create and save as filter?"))
            {
                var filter = new StringBuilder();

                foreach (var itemString in strings)
                    filter.Append($"Id=={itemString.Id} | ");

                var filterString = filter.ToString(0, filter.Length - 3);

                if (!Program.Filters.Validate<ItemData>(filterString))
                {
                    Console.WriteLine($"[Error] Invalid filter.");
                    return;
                }

                Console.WriteLine($"[Filter]: {filter}");
                var name = value.Replace(" ", "-");
                Program.Provider.Options.Filters.Add(new NamedFilter
                {
                    Name = name,
                    Value = filterString
                });
                Program.Provider.Save();
                Console.WriteLine($"[Filter] Saved as '{name}'");
            }
        }
    }
}
