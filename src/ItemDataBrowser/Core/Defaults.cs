using ItemDataBrowser.Objects;

namespace ItemDataBrowser.Core
{
    internal static class Defaults
    {
        public static readonly ColumnSet ColumnSet = new()
        {
            Name = "Default",
            List = new List<string> { "Id", "Category", "Name", "CombatItemType", "CombatItemSubType", "RequiredLevel", "RequiredGender", "RequiredClass", "RequiredRace" }
        };
    }
}
