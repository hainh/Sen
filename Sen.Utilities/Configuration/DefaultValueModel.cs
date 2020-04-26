using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Sen.Utilities.Configuration
{
    public class DefaultValueModel<T> where T : DefaultValueModel<T>
    {
        public DefaultValueModel()
        {
            ApplyDefaultValues();
        }

        private void ApplyDefaultValues()
        {
            var type = GetType();
            var props = type.GetProperties();
            var defaultValueAttributeType = typeof(DefaultValueAttribute);
            foreach (var prop in props)
            {
                if (!prop.CanWrite)
                {
                    continue;
                }
                if (Attribute.GetCustomAttribute(prop, defaultValueAttributeType) is DefaultValueAttribute defaultValue)
                {
                    prop.SetValue(this, defaultValue.Value, null);
                }
                else if (prop.PropertyType.IsGenericType && prop.PropertyType.Name == typeof(Dictionary<,>).Name)
                {
                    prop.SetValue(this, Activator.CreateInstance(prop.PropertyType), null);
                }
            }
        }

        protected static ILogger logger = InternalLogger.GetLogger<T>();
    }
}
