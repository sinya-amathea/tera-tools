using ItemDataBrowser.Objects;

namespace ItemDataBrowser.Commands.Base
{
    internal abstract class BaseCommand
    {
        public abstract string Name { get; }

        public abstract string Example { get; }

        public abstract string Description { get; }

        public abstract bool DisplayInHelp { get; }

        public abstract void Run(CommandInfo info);
    }
}
