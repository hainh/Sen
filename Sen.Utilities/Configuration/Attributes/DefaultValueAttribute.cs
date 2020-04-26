using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Sen.Utilities.Configuration
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class DefaultValueAttribute : Attribute
    {
        public DefaultValueAttribute(object value)
        {
            Value = value;
        }

        public DefaultValueAttribute(Type type)
        {
            if (type.IsArray)
            {
                Value = Activator.CreateInstance(type, new object[] { 0 });
            }
            else
            {
                Value = Activator.CreateInstance(type);
            }
        }

        public object Value { get; private set; }
    }
}
