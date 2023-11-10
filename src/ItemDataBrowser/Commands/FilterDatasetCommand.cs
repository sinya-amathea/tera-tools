using ItemDataBrowser.Commands.Base;
using ItemDataBrowser.Objects;
using Sinya.Tera.Shared.Schema;
using static System.String;
using static ItemDataBrowser.Core.Extensions.ConsoleExtensions;

namespace ItemDataBrowser.Commands
{
    internal class FilterDatasetCommand : BaseCommand
    {
        public override string Name => "fd";

        public override string Example => "fd(name[,Id==123456 & Class==archer])";

        public override string Description => "Create a filtered dataset.\r\nUse the name of xml attribute and its value to filter combine filters with '&' and '|'\r\nUse only the name parameter to load a saved filter.";

        public override bool DisplayInHelp => true;

        public override void Run(CommandInfo info)
        {
            if (!info.TryGetParameter(0, out string name))
                return;

            var existing = Program.Data.Sets.FirstOrDefault(d => d.Name == name);

            if (existing != null)
            {
                if (!AskYesNo($"[Dataset] {name} already exists. Overwrite?"))
                    return;

                existing.Items.Clear();
                existing.Filter = Empty;
            }

            string filterString;
            var isLoadedFilter = false;

            if (info.Parameters.Count > 1)
            {
                if (!info.TryGetParameter(1, out filterString))
                    return;
            }
            else
            {
                var savedFilter = Program.Provider.Options.Filters.FirstOrDefault(f => f.Name == name);

                if (savedFilter == null)
                {
                    Console.WriteLine($"[Dataset] No such filter found {name}");
                    return;
                }

                filterString = savedFilter.Value;
                isLoadedFilter = true;
            }

            var filterExpression = Program.Filters.Build<ItemData>(filterString);

            if (filterExpression == null)
            {
                Console.WriteLine($"[Filter] Invalid filter");
                return;
            }

            var result = Program.Data.ItemData.Cache.Where(filterExpression.Compile()).ToList();

            Console.WriteLine(result.Any() ? $"[Filter] Dataset contains {result.Count} items." : "[Filter] Dataset contains no items.");

            if (existing != null)
            {
                existing.Items = result;
                existing.Filter = filterString;
                return;
            }

            if (result.Any())
            {
                Program.Data.Sets.Add(new Dataset
                {
                    Name = name,
                    Filter = filterString,
                    Items = result
                });



                if (!isLoadedFilter && AskYesNo("Save filter?"))
                    Program.Command.Execute(new CommandInfo
                    {
                        Name = "sf",
                        Parameters = new List<string>
                        {
                            name,
                            filterString
                        }
                    });

            }
        }
    }
}
