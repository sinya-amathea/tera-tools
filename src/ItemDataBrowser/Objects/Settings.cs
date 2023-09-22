namespace ItemDataBrowser.Objects
{
    public class Settings
    {
        public string DataCenter { get; set; }

        public List<NamedFilter> Filters { get; set; } = new();

        public List<ColumnSet> ColumnSets { get; set; } = new();
    }

    public class NamedFilter
    {
        public string Name { get; set; }

        public string Value { get; set; }
    }

    public class ColumnSet
    {
        public string Name { get; set; }

        public List<string> List { get; set; } = new();
    }
}
