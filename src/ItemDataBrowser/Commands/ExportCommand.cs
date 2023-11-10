using ItemDataBrowser.Commands.Base;
using ItemDataBrowser.Objects;
using Newtonsoft.Json;
using System.Reflection;
using System.Text;
using static System.String;

namespace ItemDataBrowser.Commands
{
    internal class ExportCommand : BaseCommand
    {
        private const string ExportFolder = "Exports";

        private readonly Dictionary<string, Func<Dataset, ColumnSet, string>> _formatDataAs = new();
        private readonly Dictionary<string, Action<string, string, string>> _exportToTarget = new();

        public ExportCommand()
        {
            _formatDataAs.Add("colList", FormatAsColList);
            _formatDataAs.Add("csv", FormatAsCsv);
            _formatDataAs.Add("json", FormatAsJson);
            _exportToTarget.Add("console", ExportToConsole);
            _exportToTarget.Add("file", ExportToFile);
        }

        public override string Name => "ex";

        public override string Example => "ex(name,format,target[,columns,...|$columnSet$])";

        public override string Description => "Export data in various formats.\r\nFormat: colList, csv, json\r\nTarget: file, console";
        
        public override bool DisplayInHelp => true;
        
        public override void Run(CommandInfo info)
        {
            if (!info.TryGetParameter(0, out string name))
                return;

            if (!info.TryGetParameter(1, out string format))
                return;

            if (!info.TryGetParameter(2, out string target))
                return;

            var columns = info.GetColumnSet(3);

            if (!_formatDataAs.ContainsKey(format))
            {
                Console.WriteLine($"[Export] No export format for {format} defined.");
                return;
            }

            if (!_exportToTarget.ContainsKey(target))
            {
                Console.WriteLine($"[Export] No export target for {target} defined.");
                return;
            }

            var dataset = Program.Data.Find(name);

            if (dataset == null)
            {
                Console.WriteLine("[Export] No data or dataset found.");
                return;
            }

            var formatted = _formatDataAs[format].Invoke(dataset, columns);

            _exportToTarget[target].Invoke(formatted, name, format);
        }

        private string FormatAsColList(Dataset data, ColumnSet columns)
        {
            var column = columns.List[0];
            var property = Program.Data.ItemData.PropertyCache.Single(p => p.Property.Name.Equals(column, StringComparison.InvariantCultureIgnoreCase)).Property;

            return data.Items
                .Select(i => property.GetValue(i)?.ToString() ?? Empty)
                .Aggregate((current, next) => $"{current},{next}");
        }

        private string FormatAsCsv(Dataset data, ColumnSet columns)
        {
            var csvBuilder = new StringBuilder();

            csvBuilder.AppendLine(columns.List.Aggregate((current, next) => $"{current},{next}"));

            var properties = new List<PropertyInfo>();

            foreach (var column in columns.List)
                properties.Add(Program.Data.ItemData.PropertyCache.Single(p => p.Property.Name.Equals(column, StringComparison.InvariantCultureIgnoreCase)).Property);

            foreach (var item in data.Items)
            {
                var row = properties
                    .Select(p => p.GetValue(item)?.ToString() ?? Empty)
                    .Aggregate((current, next) => $"{current},{next}");

                csvBuilder.AppendLine(row);
            }

            return csvBuilder.ToString();
        }

        private string FormatAsJson(Dataset data, ColumnSet columns)
        {
            var properties = new List<PropertyInfo>();

            foreach (var column in columns.List)
                properties.Add(Program.Data.ItemData.PropertyCache.Single(p => p.Property.Name.Equals(column, StringComparison.InvariantCultureIgnoreCase)).Property);

            var exportList = new List<Dictionary<string, string>>();

            foreach (var item in data.Items)
            {
                var selected = new Dictionary<string, string>();

                foreach (var property in properties)
                {
                    var value = property.GetValue(item)?.ToString() ?? Empty;

                    selected.Add(property.Name, value);
                }

                exportList.Add(selected);
            }

            return JsonConvert.SerializeObject(exportList, Formatting.Indented);
        }

        private void ExportToConsole(string data, string _, string __) => Console.WriteLine(data);

        private void ExportToFile(string data, string name, string fileType)
        {
            string extensions;

            switch (fileType)
            {
                case "csv":
                    extensions = ".csv";
                    break;
                case "json":
                    extensions = ".json";
                    break;
                default:
                    extensions = ".txt";
                    break;
            }

            var fileName = $"{name}_{DateTime.Now:yyyMMdd_HHmmss}{extensions}";
            var path = Path.Combine(Environment.CurrentDirectory, ExportFolder);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            var fullPath = Path.Combine(path, fileName);

            using (var file = new FileStream(fullPath, FileMode.Create))
            using (var writer = new StreamWriter(file))
                writer.Write(data);

            Console.WriteLine($"[Export] Saved to '{fullPath}'");
        }
    }
}
