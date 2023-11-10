using System.Text.RegularExpressions;
using ItemDataBrowser.Core;
using static System.String;

namespace ItemDataBrowser.Objects;

public class CommandInfo
{
    public string Name { get; set; }

    public List<string> Parameters { get; set; } = new();

    public bool TryGetParameter(int index, out string parameter)
    {
        if (index < 0 || index >= Parameters.Count)
        {
            Console.WriteLine($"[Error] Invalid parameter at index {index}");
            parameter = Empty;
            return false;
        }

        parameter = Parameters[index];

        if (IsNullOrWhiteSpace(parameter))
        {
            Console.WriteLine($"[Error] Invalid parameter value at index {index}");
            return false;
        }
        return true;
    }

    public bool HasParameter(int index) => index > 0 && index < Parameters.Count && !IsNullOrWhiteSpace(Parameters[index]);

    public ColumnSet GetColumnSet(int startIndex)
    {

        var columnSetMatch = new Regex(@"^(?:\$([\w\d]+)\$)$", RegexOptions.Compiled);

        if (startIndex < 0 || startIndex >= Parameters.Count)
        {
            Console.WriteLine($"[Warn] Invalid column definition, using default...");
            return Defaults.ColumnSet;
        }

        var list = Parameters.Skip(startIndex).ToList();

        if (list.Count == 1)
        {
            var match = columnSetMatch.Match(list[0]);

            if (match.Success)
            {
                var set = Program.Provider.Options.ColumnSets.FirstOrDefault(s => s.Name == match.Groups[1].Value);

                if (set != null)
                    return set;
            }
        }

        return new ColumnSet
        {
            List = list,
            Name = "_runtime_"
        };
    }
}