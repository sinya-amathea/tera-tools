using System.Collections.Concurrent;
using System.Xml;

namespace ItemDataBrowser.Core.Xml
{
    internal abstract class BaseXmlListProvider<T>
        where T : class
    {
        private readonly string _xPath;
        private readonly string _folder;
        private readonly string _fileFilter;

        public ConcurrentBag<T> Cache { get; }

        protected BaseXmlListProvider(
            string xPath,
            string folder,
            string filter = "*.*")
        {
            _xPath = xPath;
            _fileFilter = filter;
            _folder = folder;
            Cache = new ConcurrentBag<T>();
        }

        public void Load()
        {
            Initialize();

            var path = Path.Combine(Program.Provider.Options.DataCenter, _folder);

            if (!Directory.Exists(path))
            {
                Console.WriteLine($"[Error] Folder '{path}' not found.");
                return;
            }

            var taskList = new List<Task>();

            foreach (var file in Directory.EnumerateFiles(path, _fileFilter, SearchOption.TopDirectoryOnly))
            {
                taskList.Add(new Task(() =>
                {
                    Console.WriteLine($"[{GetFileName(file)}] Loading...");
                    ParseFile(file);
                }));
            }

            taskList.ForEach(t => t.Start());
            Task.WaitAll(taskList.ToArray());
            Console.WriteLine($"[{_folder}] Loaded {Cache.Count} entries");
        }

        public void Clear() => Cache.Clear();

        private void ParseFile(string path)
        {
            var document = new XmlDocument();

            document.Load(path);
            var root = document.DocumentElement;

            if (root == null)
            {
                Console.WriteLine($"[{GetFileName(path)}] Invalid document.");
                return;
            }

            var namespaces = new XmlNamespaceManager(document.NameTable);

            SetNamespaces(namespaces);

            var nodes = root.SelectNodes(_xPath, namespaces);

            if (nodes == null || nodes.Count == 0)
            {
                Console.WriteLine($"[{GetFileName(path)}] No data found.");
                return;
            }

            var loadedItems = 0;

            foreach (XmlNode node in nodes)
            {
                if (node != null && TryParse(node, out T? data) && data != null)
                {
                    Cache.Add(data);
                    loadedItems++;
                }
            }

            Console.WriteLine($"[{GetFileName(path)}] Loaded {loadedItems}/{nodes.Count} entries");
        }

        protected abstract void SetNamespaces(XmlNamespaceManager namespaces);

        protected abstract bool TryParse(XmlNode node, out T? data);

        protected abstract void Initialize();

        protected static string GetFileName(string path) => Path.GetFileNameWithoutExtension(path);
    }
}
