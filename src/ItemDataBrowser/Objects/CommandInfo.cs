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
}