using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace Senla.Gamer.Utilities
{
    public delegate void LoadedEventHandler(object sender, EventArgs args);

    public class ConfigLoader
    {
        private string fileName;

        private FileSystemWatcher _fileWatcher;

        private int count = 0;

        public event LoadedEventHandler Loaded;

        protected virtual void OnLoaded(EventArgs args)
        {
            if (Loaded != null)
            {
                Loaded(this, args);
            }
        }

        private static string _configDirectory = null;

        public static string ConfigDirectory
        {
            get
            {
                if (_configDirectory == null)
                {
                    string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                    UriBuilder uri = new UriBuilder(codeBase);
                    string path = Uri.UnescapeDataString(uri.Path);
                    _configDirectory = Path.GetFullPath(path + "\\Config\\");
                }

                return _configDirectory;
            }
        }


        public ConfigLoader(string fileName)
        {
            this.fileName = fileName;
            _fileWatcher = new FileSystemWatcher(ConfigDirectory, fileName);
            _fileWatcher.EnableRaisingEvents = true;
            _fileWatcher.NotifyFilter = NotifyFilters.LastWrite;

            _fileWatcher.Changed += Load;
            _fileWatcher.Created += Load;
            Load(null, null);
        }

        void Load(object sender, FileSystemEventArgs e)
        {
            lock (this)
            {
                count++;
                if (count % 2 == 0)
                {
                    return;
                }
                System.Threading.Thread.Sleep(100);
                using (var reader = XmlReader.Create(Path.GetFullPath(ConfigDirectory) + fileName))
                {
                    PropertyInfo prop = null;
                    string content = null;

                    reader.MoveToContent();
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            prop = this.GetType().GetProperty(reader.Name);
                            content = reader.ReadString();
                        }

                        if (prop != null && content != null)
                        {
                            if (prop.PropertyType.IsArray)
                            {
                                var t = prop.PropertyType.GetElementType();
                                prop.SetValue(this, ToArray(t, content));
                            }
                            else
                            {
                                prop.SetValue(this, Convert.ChangeType(content, prop.PropertyType), null);
                            }
                        }
                        prop = null;
                        content = null;
                    }
                }

                OnLoaded(EventArgs.Empty);
            }
        }

        public static object ToArray(Type elementType, string valueString)
        {
            var valuesArray = valueString.Split(',');
            var result = valuesArray.Select(s => Convert.ChangeType(s, elementType)).ToArray();
            var array = Array.CreateInstance(elementType, result.Length);
            Array.Copy(result, array, result.Length);
            return array;
        }
    }
}
