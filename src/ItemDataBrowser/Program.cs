using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using ItemDataBrowser.Objects;
using Newtonsoft.Json;
using Sinya.Tera.Shared.Schema;
using static System.String;
using Formatting = Newtonsoft.Json.Formatting;

namespace ItemDataBrowser
{
    internal class Program
    {
        private const string ExportFolder = "Exports";
        private const string ItemDataFolder = "ItemData";
        private const string StrSheetItemFolder = "StrSheet_Item";
        private const string SettingsFile = "settings.json";

        private static readonly Regex BasicCommand = new(@"^([a-z]+)(?:\((.+)\))?$", RegexOptions.Compiled);
        private static readonly List<ItemData> Items = new();
        private static readonly List<PropertyMapping> PropCache = new();
        private static readonly List<Command> Commands = new ()
        {
            new Command{Name = "ddc", Example = "ddc", Action = DisplayDatacenter, Description = "Displays path to decrypted datacenter folder.", DisplayInHelp = true},
            new Command{Name = "sdc", Example = "sdc(path)", Action = SaveDatacenterPath, Description = "Saves a new datacenter path to the settings file.", DisplayInHelp = true},
            new Command{Name = "fd", Example = "fd(name[,Id==123456 & Class==archer])", Action = Filter, Description = "Create a filtered dataset.\r\nUse the name of xml attribute and its value to filter combine filters with '&' and '|'\r\nUse only the name parameter to load a saved filter", DisplayInHelp = true},
            new Command{Name = "dd", Example = "dd(name[,columns,...|$columnSet$])", Action = DisplayDataSet, Description = "Print the dataset to the console.\r\n[Optional] specify a list of columns.", DisplayInHelp = true},
            new Command{Name = "sf", Example = "sf(name,filter)", Action = SaveFilter, Description = "Saves a validated filter definition to the settings file.", DisplayInHelp = true},
            new Command{Name = "lf", Example = "lf", Action = ListFilter, Description = "Lasts all saved filters.", DisplayInHelp = true},
            new Command{Name = "ex", Example = "ex(name,format,target,[,columns,...|$columnSet$])", Action = Export, Description = "Export data in various formats.\r\nFormat: colList, csv, json\r\nTarget: file, console", DisplayInHelp = true},
            new Command{Name = "sc", Example = "sc(name,columns,...)", Action = SaveColumnSet, Description = "Saves a list of validated columns to the settings file.",  DisplayInHelp = true},
            new Command{Name = "sl", Example = "sl", Action = ListColumnSet, Description = "List all saved column sets", DisplayInHelp = true},
            new Command{Name = "map",  Example ="map[(name)]",  Action = PrintPropertyMapping, Description = "Get a list of filter property to xml attribute mapping (filter properties usually are just PascalCase'd attribute names.\r\n[Optional] specify a part of a filter name to filter the list.", DisplayInHelp = true},
            new Command{Name = "help", Example = Empty, Action = PrintHelp, Description = Empty, DisplayInHelp = false}
        };
        private static readonly Dictionary<string, Func<Dataset, ColumnSet, string>> FormatDataAs = new()
        {
            { "colList", FormatAsColList },
            { "csv", FormatAsCsv },
            { "json", FormatAsJson }
        };
        private static readonly Dictionary<string, Action<string, string, string>> ExportToTarget = new()
        {
            {"console", ExportToConsole},
            {"file", ExportToFile}
        };
        private static readonly FilterBuilder FilterBuilder = new();
        private static readonly List<Dataset> DataSets = new();
        private static readonly ColumnSet DefaultColumnSet = new()
        {
            Name = "Default",
            List = new List<string> { "Id", "Category", "Name", "CombatItemType", "CombatItemSubType", "RequiredLevel", "RequiredGender", "RequiredClass", "RequiredRace" }
        };

        private static Settings Settings;

        static void Main(string[] args)
        {
            Console.Title = "TERA ItemData Browser (v 0.1)";
            ShowWindow(GetConsoleWindow(), 3);

            ReadSettings();
            SetupCache();
            Console.WriteLine("----------------------------------------------------------------");
            LoadItemData(Settings.DataCenter);
            MainLoop();
        }

        static void SetupCache()
        {
            Console.WriteLine("[Cache] Create property <=> attribute cache");

            var properties = typeof(ItemData).GetProperties();
            
            foreach (var propertyInfo in properties)
            {
                var attrName = char.ToLower(propertyInfo.Name[0]) + propertyInfo.Name.Substring(1);

                PropCache.Add(new PropertyMapping
                {
                    AttributeName = attrName,
                    Property = propertyInfo
                });
            }
        }

        static void ReadSettings()
        {
            var filePath = Path.Combine(Environment.CurrentDirectory, SettingsFile);

            if (!File.Exists(filePath))
            {
                var path = GetDatacenterPath();

                Settings = new Settings
                {
                    DataCenter = path
                };
                SaveSettings();
                return;
            }

            using (var file = new FileStream(Path.Combine(Environment.CurrentDirectory, SettingsFile), FileMode.Open))
            using (var reader = new StreamReader(file))
            {
                var content = reader.ReadToEnd();
                Settings = JsonConvert.DeserializeObject<Settings>(content)!;
            }

            if (!Directory.Exists(Settings.DataCenter))
            {
                Settings.DataCenter = GetDatacenterPath();
                SaveSettings();
            }

            Console.WriteLine("[Settings] Loading done");
            Console.WriteLine($"[Settings] Datacenter: {Settings.DataCenter}");
        }

        static string GetDatacenterPath()
        {
            do
            {
                Console.WriteLine("Enter path of the directory to a decrypted datacenter");
                var path = Console.ReadLine();

                // todo: validate if its a dc path
                if (IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                {
                    Console.WriteLine("Invalid path.");
                    Console.WriteLine();
                }
                else
                {
                    return path;
                }
            } while (true);
        }

        static void SaveSettings()
        {
            try
            {
                using (var file = new FileStream(Path.Combine(Environment.CurrentDirectory, SettingsFile),
                           FileMode.OpenOrCreate))
                using (var writer = new StreamWriter(file))
                {
                    var content = JsonConvert.SerializeObject(Settings, Formatting.Indented);

                    writer.Write(content);
                }

                Console.WriteLine($"[Info] Settings saved");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] {ex.Message}");
            }
        }

        static void LoadItemData(string path)
        {
            var itemDataPath = Path.Combine(path, ItemDataFolder);

            foreach (var file in Directory.EnumerateFiles(itemDataPath, "ItemData-?????.xml", SearchOption.TopDirectoryOnly))
            {
                Console.WriteLine($"[{GetFileName(file)}] Loading...");
                ParseXml(file);
            }

            Console.WriteLine($"[ItemData] Loaded {Items.Count} items");
        }

        static void MainLoop()
        {
            do
            {
                Console.WriteLine();
                Console.WriteLine("Enter command:");
                
                var command = Console.ReadLine() ?? Empty;

                if (command == "exit") 
                    return;

                var commandInfo = ParseCommand(command);

                if (commandInfo == null)
                {
                    Console.WriteLine("Invalid command. Use 'help' to get a list of commands");
                    continue;
                }

                var cmd = Commands.FirstOrDefault(c => c.Name == commandInfo.Name);

                if (cmd == null)
                {
                    Console.WriteLine("Invalid command. Use 'help' to get a list of commands");
                    continue;
                }

                cmd.Action.Invoke(commandInfo);
            } while (true);
        }

        static CommandInfo? ParseCommand(string command)
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

        static void Filter(CommandInfo info)
        {
            if (!info.TryGetParameter(0, out string name))
                return;

            var existing = DataSets.FirstOrDefault(d => d.Name == name);

            if (existing != null)
            {
                if (!AskYesNo($"[Dataset] {name} already exists. Overwrite?"))
                    return;
                    
                existing.Items.Clear();
                existing.Filter = Empty;
            }

            string filterString;
            var isLoadedFilter = false;

            if (info.Parameters.Count > 1)
            {
                if (!info.TryGetParameter(1, out filterString))
                    return;
            }
            else
            {
                var savedFilter = Settings.Filters.FirstOrDefault(f => f.Name == name);

                if (savedFilter == null)
                {
                    Console.WriteLine($"[Dataset] No such filter found {name}");
                    return;
                }

                filterString = savedFilter.Value;
                isLoadedFilter = true;
            }

            var filterExpression = FilterBuilder.Get<ItemData>(filterString);

            if (filterExpression == null)
            {
                Console.WriteLine($"[Filter] Invalid filter");
                return;
            }

            var result = Items.Where(filterExpression.Compile()).ToList();

            Console.WriteLine(result.Any() ? $"[Filter] Dataset contains {result.Count} items." : "[Filter] Dataset contains no items.");

            if (existing != null)
            {
                existing.Items = result;
                existing.Filter = filterString;
                return;
            }

            if (result.Any())
            {
                DataSets.Add(new Dataset
                {
                    Name = name,
                    Filter = filterString,
                    Items = result
                });

                if (!isLoadedFilter && AskYesNo("Save filter?"))
                    SaveFilter(new CommandInfo
                    {
                        Name = "sf",
                        Parameters = new List<string>
                        {
                            name,
                            filterString
                        }
                    });

            }
        }

        static void Export(CommandInfo info)
        {
            if (!info.TryGetParameter(0, out string name))
                return;

            if (!info.TryGetParameter(1, out string format))
                return;
            
            if (!info.TryGetParameter(2, out string target))
                return;

            var columns = GetColumnList(info, 3);

            if (!FormatDataAs.ContainsKey(format))
            {
                Console.WriteLine($"[Export] No export format for {format} defined.");
                return;
            }

            if (!ExportToTarget.ContainsKey(target))
            {
                Console.WriteLine($"[Export] No export target for {target} defined.");
                return;
            }

            var dataset = GetDataset(name);

            if (dataset == null)
            {
                Console.WriteLine("[Export] No data or dataset found.");
                return;
            }

            var formatted = FormatDataAs[format].Invoke(dataset, columns);

            ExportToTarget[target].Invoke(formatted, name, format);
        }

        static ColumnSet GetColumnList(CommandInfo info, int startIndex)
        {
            var columnSetMatch = new Regex(@"^(?:\$([\w\d]+)\$)$", RegexOptions.Compiled);

            if (startIndex < 0 || startIndex >= info.Parameters.Count)
            {
                Console.WriteLine($"[Warn] Invalid column definition, using default...");
                return DefaultColumnSet;
            }

            var list = info.Parameters.Skip(startIndex).ToList();

            if (list.Count == 1)
            {
                var match = columnSetMatch.Match(list[0]);

                if (match.Success)
                {
                    var set = Settings.ColumnSets.FirstOrDefault(s => s.Name == match.Groups[1].Value);

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

        static void SaveFilter(CommandInfo info)
        {
            if (!info.TryGetParameter(0, out string name))
                return;
            
            if (!info.TryGetParameter(1, out string filterString))
                return;

            if (!FilterBuilder.Validate<ItemData>(filterString))
            {
                Console.WriteLine($"[Error] Invalid filter");

                var errors = FilterBuilder.GetErrors();

                if (errors.Any())
                    foreach (var error in errors)
                        Console.WriteLine($"[Error] {error.Name} => {error.Error}");

                return;
            }

            var existing = Settings.Filters.FirstOrDefault(f => f.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

            if (existing != null)
            {
                if (!AskYesNo($"[Save] {name} already exists, overwrite?"))
                    return;

                existing.Value = filterString;
            }
            else
            {
                Settings.Filters.Add(new NamedFilter
                {
                    Name = name,
                    Value = filterString
                });
            }
            
            SaveSettings();
        }

        static void SaveColumnSet(CommandInfo info)
        {
            var name = info.Parameters[0];

            if (IsNullOrWhiteSpace(name))
            {
                Console.WriteLine("[Error] Invalid name");
                return;
            }

            var set = Settings.ColumnSets.FirstOrDefault(c => c.Name == name);

            if (set != null && !AskYesNo("[Save] A column set with the same name already exists, overwrite?"))
                return;

            var columnNames = info.Parameters.Skip(1).ToList();

            if (set == null)
            {
                set = new ColumnSet
                {
                    Name = name
                };
                Settings.ColumnSets.Add(set);
            }
            else
            {
                set.List.Clear();
            }

            foreach (var columnName in columnNames)
            {
                var mapping = PropCache.FirstOrDefault(p =>
                    p.Property.Name.Equals(columnName, StringComparison.InvariantCultureIgnoreCase));

                if (mapping == null)
                {
                    Console.WriteLine($"[Error] Failed to save, invalid column name {columnName}");
                    return;
                }

                set.List.Add(mapping.Property.Name);
            }

            SaveSettings();
        }

        static void SaveDatacenterPath(CommandInfo info)
        {
            if (!info.TryGetParameter(0, out string path))
                return;

            if (!Directory.Exists(path))
            {
                Console.WriteLine("[Error] Invalid datacenter path");
                return;
            }

            Settings.DataCenter = path;
            SaveSettings();
        }

        static void ListFilter(CommandInfo _)
        {
            if (!Settings.Filters.Any())
            {
                Console.WriteLine("[Help] No filters found");
                return;
            }

            foreach (var filter in Settings.Filters) 
                Console.WriteLine($"[Help] {filter.Name} => {filter.Value}");
        }

        static void ListColumnSet(CommandInfo _)
        {
            if (!Settings.ColumnSets.Any())
            {
                Console.WriteLine($"[Help] No column sets found");
                return;
            }

            foreach(var column in Settings.ColumnSets)
                Console.WriteLine($"[Help] {column.Name} => {column.List.Aggregate((current, next) => $"{current}, {next}")}");
        }

        static void DisplayDataSet(CommandInfo info)
        {
            if (!info.Parameters.Any())
            {
                Console.WriteLine("[Out] Invalid parameters");
                return;
            }

            var name = info.Parameters[0];
            var dataset = GetDataset(name);

            if (dataset == null)
            {
                Console.WriteLine("[Out] No data or dataset found.");
                return;
            }

            var columns = GetColumnList(info, 1).List;
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
                properties.Add(PropCache.Single(p => p.Property.Name.Equals(column, StringComparison.InvariantCultureIgnoreCase)).Property);
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

        static void DisplayDatacenter(CommandInfo _) => Console.WriteLine($"[Help] Datacenter path: '{Settings.DataCenter}'");

        static Dataset? GetDataset(string name)
        {
            var dataset = DataSets.FirstOrDefault(d => d.Name == name);

            if (dataset == null)
            {
                var savedFilter = Settings.Filters.FirstOrDefault(f => f.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

                if (savedFilter == null)
                {
                    Console.WriteLine($"[Out] No such dataset found ({name})");
                    return null;
                }

                if (!AskYesNo("[Out] The data set is not loaded but a saved filter exists, load?"))
                    return null;

                Filter(new CommandInfo
                {
                    Name = "fd",
                    Parameters = new List<string> { name }
                });

                dataset = DataSets.Single(d => d.Name == name);
            }

            return dataset;
        }

        static void PrintHelp(CommandInfo _)
        {
            foreach (var command in Commands.Where(c => c.DisplayInHelp))
            {
                Console.WriteLine($"[{command.Name}]\t\t{command.Example}");

                if (!IsNullOrWhiteSpace(command.Description))
                    Console.WriteLine(command.Description);

                Console.WriteLine();
            }

            Console.WriteLine("[exit]\r\nClose this application.");
        }

        static void PrintPropertyMapping(CommandInfo info)
        {
            if (info.Parameters.Any() && info.TryGetParameter(0, out string name))
            {
                var searchFor = name.ToLower();
                var filtered = PropCache
                    .Where(c => c.Property.Name.ToLower().Contains(searchFor) || 
                                c.AttributeName.ToLower().Contains(searchFor))
                    .ToList();

                if (!filtered.Any())
                {
                    if (!AskYesNo("No such property found. Display the whole list?"))
                        return;
                }
                else
                {
                    foreach (var mapping in filtered)
                        Console.WriteLine($"[Help] {mapping.Property.Name} => {mapping.AttributeName}");

                    return;
                }
            }

            foreach (var mapping in PropCache)
                Console.WriteLine($"[Help] {mapping.Property.Name} => {mapping.AttributeName}");
        }

        static void ParseXml(string path)
        {
            var document = new XmlDocument();

            document.Load(path);
            var root = document.DocumentElement;

            if (root == null)
            {
                Console.WriteLine($"[{GetFileName(path)}] Invalid document.");
                return;
            }

            var nsMan = new XmlNamespaceManager(document.NameTable);

            nsMan.AddNamespace("i", "https://vezel.dev/novadrop/dc/ItemData");

            var itemNodes = root.SelectNodes("/i:ItemData/i:Item", nsMan);

            if (itemNodes == null || itemNodes.Count == 0)
            {
                Console.WriteLine($"[{GetFileName(path)}] No item data found.");
                return;
            }

            var loadedItems = 0;

            foreach (XmlNode itemNode in itemNodes)
            {
                try
                {
                    var item = new ItemData();
                    
                    foreach (var mapping in PropCache)
                    {
                        var attr = itemNode?.Attributes?[mapping.AttributeName];

                        if (attr == null)
                            continue;

                        object? value = null;

                        if (mapping.Property.PropertyType == typeof(bool))
                        {
                            value = attr.Value.ToLower() == "true";
                        }
                        else if (mapping.Property.PropertyType == typeof(bool?) && !IsNullOrWhiteSpace(attr.Value))
                        {
                            value = (bool?) (attr.Value.ToLower() == "true");
                        }
                        else if (mapping.Property.PropertyType == typeof(int?) && !IsNullOrWhiteSpace(attr.Value))
                        {
                            if (Int32.TryParse(attr.Value, out int iValue))
                                value = (int?) iValue;
                        }
                        else if (mapping.Property.PropertyType == typeof(float?) && !IsNullOrWhiteSpace(attr.Value))
                        {
                            if (Single.TryParse(attr.Value, out float iValue))
                                value = (float?) iValue;
                        }
                        else
                        {
                            value = Convert.ChangeType(attr.Value, mapping.Property.PropertyType);
                        }

                        mapping.Property.SetValue(item, value);
                    }

                    Items.Add(item);
                    loadedItems++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            Console.WriteLine($"[{GetFileName(path)}] Loaded {loadedItems}/{itemNodes.Count} items");
        }

        private static string GetFileName(string path) => Path.GetFileNameWithoutExtension(path);

        public static bool AskYesNo(string? message = null)
        {
            message ??= "Proceed?";
            
            Console.WriteLine($"{message} (y/n)");

            var input = Console.ReadLine()?.ToLower() ?? Empty;

            return input == "y";
        }

        static string FormatAsColList(Dataset data, ColumnSet columns)
        {
            var column = columns.List[0];
            var property = PropCache.Single(p => p.Property.Name.Equals(column, StringComparison.InvariantCultureIgnoreCase)).Property;

            return data.Items
                .Select(i => property.GetValue(i)?.ToString() ?? Empty)
                .Aggregate((current, next) => $"{current},{next}");
        }

        static string FormatAsCsv(Dataset data, ColumnSet columns)
        {
            var csvBuilder = new StringBuilder();

            csvBuilder.AppendLine(columns.List.Aggregate((current, next) => $"{current},{next}"));

            var properties = new List<PropertyInfo>();

            foreach (var column in columns.List)
                properties.Add(PropCache.Single(p => p.Property.Name.Equals(column, StringComparison.InvariantCultureIgnoreCase)).Property);

            foreach (var item in data.Items)
            {
                var row = properties
                    .Select(p => p.GetValue(item)?.ToString() ?? Empty)
                    .Aggregate((current, next) => $"{current},{next}");

                csvBuilder.AppendLine(row);
            }

            return csvBuilder.ToString();
        }

        static string FormatAsJson(Dataset data, ColumnSet columns)
        {
            var properties = new List<PropertyInfo>();

            foreach (var column in columns.List)
                properties.Add(PropCache.Single(p => p.Property.Name.Equals(column, StringComparison.InvariantCultureIgnoreCase)).Property);

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

        static void ExportToConsole(string data, string _, string __) => Console.WriteLine(data);

        static void ExportToFile(string data, string name, string fileType)
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

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}