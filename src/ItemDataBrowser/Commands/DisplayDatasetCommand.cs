using ItemDataBrowser.Commands.Base;
using ItemDataBrowser.Objects;
using System.Reflection;
using System.Text;
using static System.String;

namespace ItemDataBrowser.Commands
{
    internal class DisplayDatasetCommand : BaseCommand
    {
        public override string Name => "dd";

        public override string Example => "dd(name[,columns,...|$columnSet$])";

        public override string Description => "Print the dataset to the console.\r\n[Optional] specify a list of columns.";

        public override bool DisplayInHelp => true;

        public override void Run(CommandInfo info)
        {
            if (!info.Parameters.Any())
            {
                Console.WriteLine("[Out] Invalid parameters");
                return;
            }

            var name = info.Parameters[0];
            var dataset = Program.Data.Find(name);

            if (dataset == null)
            {
                Console.WriteLine("[Out] No data or dataset found.");
                return;
            }

            var columns = info.GetColumnSet(1).List;
            var headers = new List<TableHeader>();
            var properties = new List<PropertyInfo>();

            foreach (var column in columns)
            {
                var header = new TableHeader
                {
                    Name = column,
                    Content = $" {column} "
                };

                header.Length = header.Content.Length;
                headers.Add(header);
                properties.Add(Program.Data.ItemData.PropertyCache.Single(p => p.Property.Name.Equals(column, StringComparison.InvariantCultureIgnoreCase)).Property);
            }

            var rows = new List<TableRow>();

            foreach (var item in dataset.Items)
            {
                var row = new TableRow();

                foreach (var property in properties)
                {
                    var value = property.GetValue(item)?.ToString() ?? Empty;
                    var header = headers.Single(h =>
                        String.Equals(h.Name, property.Name, StringComparison.InvariantCultureIgnoreCase));

                    if (value.Length > header.Length)
                        header.Length = value.Length + 2;

                    row.Values.Add(value);
                }

                rows.Add(row);
            }

            var headerRow = $"| {headers.Select(h => h.Content.PadRight(h.Length, ' ')).Aggregate((current, next) => $"{current} | {next}")} |";
            var divider = $"{new String('-', headerRow.Length)}";

            Console.WriteLine($"[Dataset] {name} ({dataset.Filter}, {dataset.Items.Count})");
            Console.WriteLine(divider);
            Console.WriteLine(headerRow);
            Console.WriteLine(divider);

            foreach (var row in rows)
            {
                var rowBuilder = new StringBuilder();

                rowBuilder.Append("| ");

                for (var i = 0; i < row.Values.Count; i++)
                {
                    var value = row.Values[i];
                    var header = headers[i];

                    rowBuilder.Append(value.PadRight(header.Length, ' '));

                    if (i < row.Values.Count - 1)
                        rowBuilder.Append(" | ");
                }

                rowBuilder.Append(" |");
                Console.WriteLine(rowBuilder.ToString());
            }

            Console.WriteLine(divider);
        }
    }
}
