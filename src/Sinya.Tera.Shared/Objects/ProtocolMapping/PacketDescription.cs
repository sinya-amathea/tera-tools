using System.Text;
using static System.String;

namespace Sinya.Tera.Shared.Objects.ProtocolMapping
{
    public class PacketDescription
    {
        public int OpCode { get; set; }

        public string Name { get; set; }

        public List<int> Versions { get; set; } = new();

        public Dictionary<int, string> DefinitionDetails { get; set; } = new();
    }
}
