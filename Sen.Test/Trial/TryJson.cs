using Sen.Utilities.Configuration;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Trial
{
    class TryJson
    {
        public static void Run()
        {
            var n = new NewClas() { A = 1, B = DayOfWeek.Monday, C  = new NestedData() { H = "1", I = "2", K = "3" } };
            var s = JsonSerializer.Serialize(n);
            var json = JsonDocument.Parse("sdf");
            Console.WriteLine(json.RootElement.GetString());
            object a = Convert.ChangeType(json.RootElement.GetProperty("A").ToConvertable(), typeof(float));
            Console.WriteLine(a);
        }
    }

    class NewClas
    {
        public NewClas() { }

        public NewClas (int a, DayOfWeek b)
        {
            A = a;
            B = b;
        }

        public int A { get; set; }

        public DayOfWeek B { get; set; }

        public NestedData C { get; set; }
    }

    class NestedData
    {
        public string H { get; set; }
        public string I { get; set; }
        public string K { get; set; }
    }

    class AlwaysJsonExtension
    {
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }
}
