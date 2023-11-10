using ItemDataBrowser.Core.Cache;
using ItemDataBrowser.Objects;
using static ItemDataBrowser.Core.Extensions.ConsoleExtensions;

namespace ItemDataBrowser.Core
{
    internal class DataProvider
    {
        public List<Dataset> Sets = new();

        public ItemDataCache ItemData { get; } = new();

        public ItemStringSheetCache ItemStrings { get; } = new();

        public void Load()
        {
            Console.WriteLine("----------------------------------------------------------------");

            var loadingTasks = new List<Task>
            {
                new Task(() => ItemData.Load()),
                new Task(() => ItemStrings.Load())
            };

            loadingTasks.ForEach(t => t.Start());
            Task.WaitAll(loadingTasks.ToArray());

            Console.WriteLine("----------------------------------------------------------------");
        }

        public void Reload()
        {
            ItemData.Clear();
            ItemStrings.Clear();

            Load();
        }

        public Dataset? Find(string name)
        {
            var dataset = Sets.FirstOrDefault(d => d.Name == name);

            if (dataset == null)
            {
                var savedFilter = Program.Provider.Options.Filters.FirstOrDefault(f => f.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

                if (savedFilter == null)
                {
                    Console.WriteLine($"[Out] No such dataset found ({name})");
                    return null;
                }

                if (!AskYesNo("[Out] The data set is not loaded but a saved filter exists, load?"))
                    return null;

                Program.Command.Execute(new CommandInfo
                {
                    Name = "fd",
                    Parameters = new List<string> { name }
                });

                dataset = Sets.Single(d => d.Name == name);
            }

            return dataset;
        }
    }
}
