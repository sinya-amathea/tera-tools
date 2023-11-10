using ItemDataBrowser.Commands.Base;
using ItemDataBrowser.Objects;
using static System.String;
using static ItemDataBrowser.Core.Extensions.ConsoleExtensions;

namespace ItemDataBrowser.Commands
{
    internal class SaveColumnSetCommand : BaseCommand
    {
        public override string Name => "sc";

        public override string Example => "sc(name,columns,...)";

        public override string Description => "Saves a list of validated columns to the settings file.";

        public override bool DisplayInHelp => true;

        public override void Run(CommandInfo info)
        {
            var name = info.Parameters[0];

            if (IsNullOrWhiteSpace(name))
            {
                Console.WriteLine("[Error] Invalid name");
                return;
            }

            var set = Program.Provider.Options.ColumnSets.FirstOrDefault(c => c.Name == name);

            if (set != null && !AskYesNo("[Save] A column set with the same name already exists, overwrite?"))
                return;

            var columnNames = info.Parameters.Skip(1).ToList();

            if (set == null)
            {
                set = new ColumnSet
                {
                    Name = name
                };
                Program.Provider.Options.ColumnSets.Add(set);
            }
            else
            {
                set.List.Clear();
            }

            foreach (var columnName in columnNames)
            {
                var mapping = Program.Data.ItemData.PropertyCache.FirstOrDefault(p =>
                    p.Property.Name.Equals(columnName, StringComparison.InvariantCultureIgnoreCase));

                if (mapping == null)
                {
                    Console.WriteLine($"[Error] Failed to save, invalid column name {columnName}");
                    return;
                }

                set.List.Add(mapping.Property.Name);
            }

            Program.Provider.Save();
        }
    }
}
