using System;
using System.IO;
using System.Text;

namespace JsonDataGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            string folder = args.Length == 1 ? args[0] : ".";
            folder = Path.GetFullPath(folder);
            var jsonUtf8 = new DataCodeGenerator().GenerateDataJson(folder);
            if (jsonUtf8 != null)
            {
                string destinationFilePath = Path.Join(folder, "MessageTypes.js");
                using var fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write);
                using var binWriter = new BinaryWriter(fileStream);
                binWriter.Write(JsCodeHeader);
                binWriter.Write(jsonUtf8);
                binWriter.Write((byte)';');
            }
        }

        static readonly byte[] JsCodeHeader = Encoding.UTF8.GetBytes("this.MessageTypes = ");
    }
}
