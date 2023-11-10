using ItemDataBrowser.Objects;
using Newtonsoft.Json;
using static System.String;

namespace ItemDataBrowser.Core
{
    internal class SettingsProvider
    {
        private const string SettingsFile = "settings.json";

        public Options Options { get; set; }

        public void Load()
        {
            var filePath = Path.Combine(Environment.CurrentDirectory, SettingsFile);

            if (!File.Exists(filePath))
            {
                var path = GetDatacenterPath();

                Options = new Options
                {
                    DataCenter = path,
                    AutoFullScreen = true
                };
                Save();
                return;
            }

            using (var file = new FileStream(Path.Combine(Environment.CurrentDirectory, SettingsFile), FileMode.Open))
            using (var reader = new StreamReader(file))
            {
                var content = reader.ReadToEnd();
                Options = JsonConvert.DeserializeObject<Options>(content)!;
            }

            if (!Directory.Exists(Options.DataCenter))
            {
                Options.DataCenter = GetDatacenterPath();
                Save();
            }

            Console.WriteLine("[Settings] Loading done");
            Console.WriteLine($"[Settings] Datacenter: {Options.DataCenter}");
        }

        public void Save()
        {
            try
            {
                var path = Path.Combine(Environment.CurrentDirectory, SettingsFile);

                using (var file = new FileStream(path, FileMode.Create))
                using (var writer = new StreamWriter(file))
                {
                    var content = JsonConvert.SerializeObject(Options, Formatting.Indented);

                    writer.Write(content);
                }

                Console.WriteLine($"[Info] Settings saved");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] {ex.Message}");
            }
        }

        private string GetDatacenterPath()
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
    }
}