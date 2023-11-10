using System.Reflection;
using ItemDataBrowser.Objects;
using System.Text.RegularExpressions;

namespace ItemDataBrowser.Commands.Base
{
    internal class CommandEngine
    {
        private static readonly Regex BasicCommand = new(@"^([a-z]+)(?:\((.+)\))?$", RegexOptions.Compiled);
        internal static readonly List<BaseCommand> Commands = new();

        public bool Initialize()
        {
            try
            {
                var assembly = Assembly.GetAssembly(GetType());

                if (assembly == null)
                {
                    Console.WriteLine("[Error]: Failed to initialize commands.");
                    return false;
                }

                foreach (var commandType in assembly.GetTypes()
                             .Where(t => t.IsClass && t.BaseType == typeof(BaseCommand)))
                {
                    var commandInstance = Activator.CreateInstance(commandType);

                    if (!(commandInstance is BaseCommand command))
                    {
                        Console.WriteLine($"[Error] Invalid command definition ({commandType.Name})");
                        continue;
                    }

                    Commands.Add(command);
                }

                Console.WriteLine($"[Commands]: Loaded {Commands.Count} command(s).");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Error]: Failed loading commands!");

#if DEBUG
                Console.WriteLine(ex);
#endif

                return false;
            }
        }

        public void Execute(string command)
        {
            var commandInfo = Parse(command);

            if (commandInfo == null)
            {
                Console.WriteLine("Invalid command. Use 'help' to get a list of commands");
                return;
            }

            Execute(commandInfo);
        }

        public void Execute(CommandInfo info)
        {
            var cmd = Commands.FirstOrDefault(c => c.Name == info.Name);

            if (cmd == null)
            {
                Console.WriteLine("Invalid command. Use 'help' to get a list of commands");
                return;
            }

            cmd.Run(info);
        }

        private CommandInfo? Parse(string command)
        {
            var match = BasicCommand.Match(command);

            if (!match.Success)
                return null;

            var info = new CommandInfo
            {
                Name = match.Groups[1].Value
            };

            if (match.Groups[2].Success)
                info.Parameters = match.Groups[2].Value.Split(',').ToList();

            return info;
        }
    }
}
