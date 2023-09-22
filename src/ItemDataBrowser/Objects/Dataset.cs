using Sinya.Tera.Shared.Schema;

namespace ItemDataBrowser.Objects;

public class Dataset
{
    public string Name { get; set; }

    public string Filter { get; set; }

    public List<ItemData> Items { get; set; } = new();
}