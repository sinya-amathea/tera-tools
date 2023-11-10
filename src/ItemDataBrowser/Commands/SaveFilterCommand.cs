using ItemDataBrowser.Commands.Base;
using ItemDataBrowser.Objects;
using Sinya.Tera.Shared.Schema;
using static ItemDataBrowser.Core.Extensions.ConsoleExtensions;

namespace ItemDataBrowser.Commands
{
    internal class SaveFilterCommand : BaseCommand
    {
        public override string Name => "sf";
        
        public override string Example => "sf(name,filter)";
        
        public override string Description => "Saves a validated filter definition to the settings file.";
        
        public override bool DisplayInHelp => true;
        
        public override void Run(CommandInfo info)
        {
            if (!info.TryGetParameter(0, out string name))
                return;

            if (!info.TryGetParameter(1, out string filterString))
                return;

            if (!Program.Filters.Validate<ItemData>(filterString))
            {
                Console.WriteLine($"[Error] Invalid filter");

                var errors = Program.Filters.GetErrors();

                if (errors.Any())
                    foreach (var error in errors)
                        Console.WriteLine($"[Error] {error.Name} => {error.Error}");

                return;
            }

            var existing = Program.Provider.Options.Filters.FirstOrDefault(f => f.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

            if (existing != null)
            {
                if (!AskYesNo($"[Save] {name} already exists, overwrite?"))
                    return;

                existing.Value = filterString;
            }
            else
            {
                Program.Provider.Options.Filters.Add(new NamedFilter
                {
                    Name = name,
                    Value = filterString
                });
            }

            Program.Provider.Save();
        }
    }
}
