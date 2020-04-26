using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Sen.Utilities.Configuration
{
    public class LoadChangedEventArgs : EventArgs
    {
        public Dictionary<PropertyInfo, object> OldValuesChanged { get; set; }
    }

    public delegate void LoadedEventHandler(object sender, EventArgs args);

    public class JsonConfig<T> : DefaultValueModel<T> where T : JsonConfig<T>
    {
        public event LoadedEventHandler Loaded;

        public static event LoadedEventHandler OnAnyConfigLoaded;

        private static readonly char[] _sArraySeparator = new char[] { ',' };

        private static readonly string convertibleType = nameof(IConvertible);

        private static bool IsConvertable(Type type) => type.GetInterface(convertibleType) != null;

        private bool thisOnLoadedCalled;

        private object _parent;

        public object GetParentConfig() => _parent;

        protected void SetParent(object parent)
        {
            _parent = parent;
        }

        protected virtual void OnLoaded(EventArgs args)
        {
            thisOnLoadedCalled = true;
            if (_parent == null)
            {
                OnAnyConfigLoaded?.Invoke(this, args);
            }
            Loaded?.Invoke(this, args);
            logger.LogInformation(
                "Loaded {0}, {1}", 
                GetType().FullName, 
                JsonSerializer.Serialize(this, new JsonSerializerOptions() { WriteIndented = true }));
        }

        public virtual T Load(JsonElement jsonDocument)
        {
            System.Diagnostics.Contracts.Contract.Requires(jsonDocument.ValueKind == JsonValueKind.Object);
            lock (this)
            {
                List<Exception> exceptions = new List<Exception>();
                Dictionary<PropertyInfo, object> oldValues = new Dictionary<PropertyInfo, object>();
                Dictionary<PropertyInfo, object> changedValues = new Dictionary<PropertyInfo, object>();

                PropertyInfo prop = null;
                JsonElement content;
                Type type = GetType();

                var validateAttrib = typeof(ValidateAttribute);

                foreach (JsonProperty keyValuePair in jsonDocument.EnumerateObject())
                {
                    prop = type.GetProperty(keyValuePair.Name);
                    content = keyValuePair.Value;

                    if (prop != null && prop.CanWrite)
                    {
                        var oldValue = prop.GetValue(this);
                        oldValues.Add(prop, oldValue);
                        Type propType = prop.PropertyType;

                        try
                        {
                            object value = Unassigned.Yes;
                            if (propType.IsGenericType
                                && propType.Name == typeof(Dictionary<,>).Name
                                && content.ValueKind == JsonValueKind.Object)
                            {
                                Type[] pairTypes = propType.GenericTypeArguments;
                                if (IsConvertable(pairTypes[0])
                                    && (pairTypes[1].IsArray || IsConvertable(pairTypes[1]) || IsConfigType(pairTypes[1])))
                                {
                                    value = Activator.CreateInstance(propType);
                                    var addMethod = propType.GetMethod("Add", pairTypes);
                                    foreach (JsonProperty jsonProperty in content.EnumerateObject())
                                    {
                                        object key = ConvertValue(pairTypes[0], jsonProperty.Name);
                                        object val = pairTypes[1].IsArray
                                                ? ToArray(pairTypes[1].GetElementType(), jsonProperty.Value)
                                                : ConvertValue(pairTypes[1], jsonProperty.Value);
                                        addMethod.Invoke(value, new object[] { key, val });
                                    }
                                }
                            }
                            else
                            {
                                value = ConvertValue(propType, content);
                            }

                            if (value is Unassigned)
                            {
                                throw new InvalidValueException($"Type ${propType.ToString()} of property ${type.Name}.${prop.Name} is not supported");
                            }
                            prop.SetValue(this, value);
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add(new Exception("Set property " + prop.Name + " of " + type.Name + " has error", ex));
                        }
                        var newValue = prop.GetValue(this);
                        if (IsValueChanged(propType, oldValue, newValue))
                        {
                            changedValues.Add(prop, oldValue);
                        }
                        foreach (ValidateAttribute validateAttribObject in Attribute.GetCustomAttributes(prop, validateAttrib))
                        {
                            try
                            {
                                validateAttribObject.Validate(prop.GetValue(this), prop.Name);
                            }
                            catch (Exception ex)
                            {
                                prop.SetValue(this, oldValue);
                                exceptions.Add(ex);
                            }
                        }
                    }
                }

                try
                {
                    ManualValidate();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    // Reset to old values
                    foreach (var item in oldValues)
                    {
                        item.Key.SetValue(this, item.Value);
                    }
                }

                thisOnLoadedCalled = false;
                OnLoaded(new LoadChangedEventArgs() { OldValuesChanged = changedValues });
                if (!thisOnLoadedCalled)
                {
                    throw new Exception(string.Format("Method {0} of subclass {1} not calls overridden one of base class {2}", nameof(OnLoaded), this.GetType().FullName, this.GetType().BaseType.FullName));
                }

                if (exceptions.Count > 0)
                {
                    throw new AggregateException("Validate on load " + GetType().Name + " have error. All configs were reverted to old values.", exceptions);
                }

                return (T)this;
            }
        }

        /// <summary>
        /// Validate all fields manually
        /// </summary>
        protected virtual void ManualValidate() { }

        protected object ConvertValue(Type type, JsonElement value)
        {
            if (type.IsEnum)
            {
                return Enum.Parse(type, value.GetString());
            }
            if (type.IsArray)
            {
                return ToArray(type.GetElementType(), value);
            }
            if (IsConfigType(type))
            {
                object instance = Activator.CreateInstance(type);
                var loadMethod = type.GetMethod(nameof(Load), new Type[] { typeof(JsonElement) });
                loadMethod.Invoke(instance, new object[] { value });
                var setParentMethod = type.GetMethod(nameof(SetParent), BindingFlags.NonPublic | BindingFlags.Instance);
                setParentMethod.Invoke(instance, new object[] { this });
                return instance;
            }
            if (IsConvertable(type))
            {
                return Convert.ChangeType(value.ToConvertable(), type);
            }
            return Unassigned.Yes;
        }

        protected static object ConvertValue(Type type, string value)
        {
            return type.IsEnum ? Enum.Parse(type, value) : Convert.ChangeType(value, type);
        }

        protected object ToArray(Type elementType, JsonElement arrayValue)
        {
            object[] result =
                arrayValue.ValueKind == JsonValueKind.Array
                ? arrayValue.EnumerateArray().Select(s => ConvertValue(elementType, s)).ToArray()
                : arrayValue.GetString().Split(_sArraySeparator).Select(s => ConvertValue(elementType, s)).ToArray();
            Array array = Array.CreateInstance(elementType, result.Length);
            Array.Copy(result, array, result.Length);
            return array;
        }

        protected bool IsValueChanged(Type elementType, object oldValue, object newValue)
        {
            if (oldValue == null)
            {
                return true;
            }
            if (elementType.IsArray)
            {
                Array o = (Array)oldValue;
                Array n = (Array)newValue;
                if (o.Length != n.Length)
                {
                    return true;
                }
                for (int i = 0; i < o.Length; i++)
                {
                    object v1 = o.GetValue(i), v2 = n.GetValue(i);
                    if ((v1 == null && v2 != null) || (v1 != null && v2 == null) || !v1.Equals(v2))
                    {
                        return true;
                    }
                }
                return false;
            }
            else
            {
                return !oldValue.Equals(newValue);
            }
        }

        internal static bool IsConfigType(Type type)
        {
            var configType = typeof(JsonConfig<>);
            while (true)
            {
                type = type.BaseType;
                if (type == null)
                {
                    return false;
                }
                if (type.IsGenericType && type.GetGenericTypeDefinition() == configType)
                {
                    return true;
                }
            }
        }

        public override bool Equals(object obj)
        {
            Type thisType = this.GetType();
            Type otherType = obj.GetType();
            if (thisType != otherType)
            {
                return false;
            }
            PropertyInfo[] properties = thisType.GetProperties();
            foreach (PropertyInfo prop in properties)
            {
                var thisValue = prop.GetValue(this);
                var otherValue = prop.GetValue(obj);
                if (prop.PropertyType.IsArray)
                {
                    Array thisArr = (Array)thisValue;
                    Array otherArr = (Array)otherValue;
                    if (thisArr.Length != otherArr.Length)
                    {
                        return false;
                    }
                    for (int i = 0; i < thisArr.Length; i++)
                    {
                        object thisElem = thisArr.GetValue(i);
                        object otherElem = otherArr.GetValue(i);
                        if ((thisElem == null ^ otherElem == null) || (thisElem != null && !thisElem.Equals(otherElem)))
                        {
                            return false;
                        }
                    }
                }
                else if ((thisValue == null ^ otherValue == null) || (thisValue != null && !thisValue.Equals(otherValue)))
                {
                    return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    enum Unassigned { Yes }
}
