using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace JsonDataGenerator
{
    public class DataCodeGenerator
    {
        const string MessagePackObjectAttribute = "MessagePackObjectAttribute";
        const string IUnionData = "IUnionData";
        const string UnionAttribute = "UnionAttribute";
        const string KeyAttribute = "KeyAttribute";
        static readonly string[] ExcludeAssemblyNames = new[]
        {
            "DotNetty.",
            "MessagePack",
            "Microsoft.",
            "Newtonsoft.Json",
            "NLog",
            "Orleans.",
            "Sen.Interfaces",
            "Sen.Utilities",
            "Sen.Grains",
            "Sen.Proxy",
            "Sen.Server",
            "System."
        };

        /// <summary>
        /// Generate json description of data type
        /// </summary>
        /// <param name="directoryPath">Root directory contains assemblies to find data types</param>
        /// <returns></returns>
        public byte[] GenerateDataJson(string directoryPath)
        {
            string[] fileNames = Directory.GetFiles(directoryPath, "*.dll");
            var assemblies = fileNames
                .Where(fileName => !ExcludeAssemblyNames.Any(excludeFile => Path.GetFileNameWithoutExtension(fileName).Contains(excludeFile)))
                .Select(fileName => Assembly.LoadFrom(fileName)).ToArray();
            List<Class> Types = new List<Class>(1000);
            List<Type> allDataTypes = new List<Type>(1000);
            var rootUnionInterfaces = new List<Type>();

            foreach (Assembly assembly in assemblies)
            {
                Type[] assemblyTypes = assembly.GetTypes();
                rootUnionInterfaces.AddRange(
                    assemblyTypes.Where(type => type.IsInterface
                                        && type.GetInterface(IUnionData) != null
                                        && type.CustomAttributes.Any(attr => attr.AttributeType.Name == UnionAttribute)));
                IEnumerable<Type> dataTypes = assemblyTypes
                    .Where(type => type.CustomAttributes.Any(attr => attr.AttributeType.Name == MessagePackObjectAttribute));
                allDataTypes.AddRange(dataTypes);
            }

            if (rootUnionInterfaces.Count != 1)
            {
                ShowErrorMessageNotExactOneUnionInterface(rootUnionInterfaces.Count);
                return null;
            }
            Type root = rootUnionInterfaces[0];
            Dictionary<Type, int> typeAndCodeDict = GetDataTypeAndUnionCodeFromInterfaceOfUnions(root);

            foreach (Type dataType in allDataTypes)
            {
                int key = -1;
                if (dataType.GetInterface(IUnionData) != null)
                {
                    if (!typeAndCodeDict.TryGetValue(dataType, out key))
                    {
                        key = -1;
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine($"WARNING: Type {dataType.FullName} has not been declare as Union attrib in {root.FullName}");
                        Console.ResetColor();
                    }
                }
                Types.Add(new Class()
                {
                    ClassName = dataType.IsGenericType ? dataType.Name.Substring(0, dataType.Name.LastIndexOf('`')) : dataType.Name,
                    UnionCode = key,
                    Values = GetDataValues(dataType)
                });
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"");
            Console.ResetColor();

            return JsonSerializer.SerializeToUtf8Bytes(new { Types }, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
        }

        private Value[] GetDataValues(Type dataType)
        {
            IEnumerable<Value> props = dataType
                .GetProperties()
                .Where(prop => (prop.GetMethod?.IsPublic??false) && (prop.SetMethod?.IsPublic??false))
                .Select(prop => new Value() 
                { 
                    KeyCode = GetKey(prop),
                    Type = GetType(prop.PropertyType),
                    ValueName = prop.Name,
                    IsArray = prop.PropertyType.IsArray
                });
            IEnumerable<Value> fields = dataType
                .GetFields()
                .Where(field => field.IsPublic)
                .Select(field => new Value() 
                { 
                    KeyCode = GetKey(field),
                    Type = GetType(field.FieldType),
                    ValueName = field.Name,
                    IsArray = field.FieldType.IsArray
                });
            return props.Concat(fields).ToArray();
        }

        private string GetType(Type type)
        {
            return type.IsArray ? type.GetElementType().Name : type.IsEnum ? type.GetEnumUnderlyingType().Name : type.Name;
        }

        static int GetKey(MemberInfo memberInfo)
        {
            CustomAttributeData keyAttr = memberInfo
                .GetCustomAttributesData()
                .FirstOrDefault(attr => attr.AttributeType.Name == KeyAttribute);
            return (int)keyAttr.ConstructorArguments[0].Value;
        }

        Dictionary<Type, int> GetDataTypeAndUnionCodeFromInterfaceOfUnions(Type unionInterfaceType)
        {
            Dictionary<Type, int> result = new Dictionary<Type, int>();
            var unions = unionInterfaceType.CustomAttributes.Where(attr => attr.AttributeType.Name == UnionAttribute);
            foreach (var attr in unions)
            {
                CustomAttributeTypedArgument arg1 = attr.ConstructorArguments[0];
                CustomAttributeTypedArgument arg2 = attr.ConstructorArguments[1];
                result.Add((Type)arg2.Value, (int)arg1.Value);
            }
            return result;
        }

        void ShowErrorMessageNotExactOneUnionInterface(int count)
        {
            if (count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Application has 0 Union interface that extend IUnionData");
            }
            else if (count > 1)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Application has {0} Union interfaces that extend IUnionData. "
                    + "We only permit 1 Union interface", count);
            }
            Console.ResetColor();
        }
    }
}
