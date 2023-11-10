using ItemDataBrowser.Commands.Base;
using ItemDataBrowser.Objects;

namespace ItemDataBrowser.Commands
{
    internal class DisplayDataCenterPathCommand : BaseCommand
    {
        public override string Name => "ddc";

        public override string Example => "ddc";
        
        public override string Description => "Displays path to decrypted datacenter folder.";

        public override bool DisplayInHelp => true;

        public override void Run(CommandInfo _) => Console.WriteLine($"[Help] Datacenter path: '{Program.Provider.Options.DataCenter}'");
    }
}
