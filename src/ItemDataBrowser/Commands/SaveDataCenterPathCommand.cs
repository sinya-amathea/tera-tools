using ItemDataBrowser.Commands.Base;
using ItemDataBrowser.Objects;
using static ItemDataBrowser.Core.Extensions.ConsoleExtensions;

namespace ItemDataBrowser.Commands
{
    internal class SaveDataCenterPathCommand : BaseCommand
    {
        public override string Name => "sdc";

        public override string Example => "sdc";
        
        public override string Description => "Saves a new datacenter path to the settings file and reloads the files.";
        
        public override bool DisplayInHelp => true;
        
        public override void Run(CommandInfo info)
        {
            if (!info.TryGetParameter(0, out string path))
                return;

            if (!Directory.Exists(path))
            {
                Console.WriteLine("[Error] Invalid datacenter path");
                return;
            }

            Program.Provider.Options.DataCenter = path;
            Program.Provider.Save();

            if (AskYesNo("Reload from new path?"))
                Program.Data.Reload();
        }
    }
}
