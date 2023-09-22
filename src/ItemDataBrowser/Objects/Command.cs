namespace ItemDataBrowser.Objects;

public class Command
{
    public string Name { get; set; }

    public string Example { get; set; }

    public string Description { get; set; }

    public Action<CommandInfo> Action { get; set; }

    public bool DisplayInHelp { get; set; }
}