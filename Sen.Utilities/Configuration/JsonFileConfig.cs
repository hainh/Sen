using System;
using System.IO;
using System.Text.Json;

namespace Sen.Utilities.Configuration
{
    public class JsonFileConfig<T> : JsonConfig<T> where T : JsonFileConfig<T>
    {
        public static string ConfigDirectory
        {
            get
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                UriBuilder uri = new UriBuilder(baseDir);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetFullPath(path + "\\Config\\");
            }
        }

        public string FullPath { get; protected set; }

        private readonly FileSystemWatcher fileSystemWatcher;

        private DateTime LastLoadTime = DateTime.MinValue;

        public JsonFileConfig(string fileName)
        {
            FullPath = ConfigDirectory + fileName;
            fileSystemWatcher = new FileSystemWatcher(Path.GetDirectoryName(FullPath), Path.GetFileName(FullPath));
            fileSystemWatcher.Changed += FileSystemWatcher_Changed;
        }

        public T Load()
        {
            byte[] data = File.ReadAllBytes(FullPath);
            // discard BOM
            int discard = data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF ? 3 : 0;
            var option = new JsonDocumentOptions() 
            { 
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            };
            JsonDocument jsonDoc = JsonDocument.Parse(new ReadOnlyMemory<byte>(data, discard, data.Length - discard), option);
            this.Load(jsonDoc.RootElement);
            return (T)this;
        }

        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if ((DateTime.UtcNow - LastLoadTime).TotalSeconds < 1)
            {
                return;
            }
            LastLoadTime = DateTime.UtcNow;
            this.Load();
        }
    }
}
