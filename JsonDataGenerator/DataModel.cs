using System;
using System.Collections.Generic;
using System.Text;

namespace JsonDataGenerator
{
    class Value
    {
        public string ValueName { get; set; }
        public int KeyCode { get; set; }
        public string Type { get; set; }
        public bool IsArray { get; set; }
    }

    class Class
    {
        public string ClassName { get; set; }
        public int UnionCode { get; set; }
        public Value[] Values { get; set; }
    }
}
