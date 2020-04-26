using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Sen.Utilities.Configuration
{
    public struct ConvertableJsonElem : IConvertible
    {
        JsonElement _jsonElement;

        public ConvertableJsonElem(JsonElement jsonElement)
        {
            _jsonElement = jsonElement;
        }

        public TypeCode GetTypeCode()
        {
            return _jsonElement.ValueKind switch
            {
                JsonValueKind.Undefined => TypeCode.DBNull,
                JsonValueKind.Object => TypeCode.Object,
                JsonValueKind.Array => TypeCode.Object,
                JsonValueKind.String => TypeCode.String,
                JsonValueKind.Number => TypeCode.Double,
                JsonValueKind.True => TypeCode.Boolean,
                JsonValueKind.False => TypeCode.Boolean,
                JsonValueKind.Null => TypeCode.DBNull,
                _ => TypeCode.DBNull,
            };
        }

        public bool ToBoolean(IFormatProvider provider) => _jsonElement.GetBoolean();

        public byte ToByte(IFormatProvider provider) => _jsonElement.GetByte();

        public char ToChar(IFormatProvider provider) => (char)_jsonElement.GetUInt16();

        public DateTime ToDateTime(IFormatProvider provider) => _jsonElement.GetDateTime();

        public decimal ToDecimal(IFormatProvider provider) => _jsonElement.GetDecimal();

        public double ToDouble(IFormatProvider provider) => _jsonElement.GetDouble();

        public short ToInt16(IFormatProvider provider) => _jsonElement.GetInt16();

        public int ToInt32(IFormatProvider provider) => _jsonElement.GetInt32();

        public long ToInt64(IFormatProvider provider) => _jsonElement.GetInt64();

        public sbyte ToSByte(IFormatProvider provider) => _jsonElement.GetSByte();

        public float ToSingle(IFormatProvider provider) => _jsonElement.GetSingle();

        public string ToString(IFormatProvider provider) => _jsonElement.GetString();

        public object ToType(Type conversionType, IFormatProvider provider)
        {
            throw new NotSupportedException($"Convert to type {conversionType.FullName} is not supported.");
        }

        public ushort ToUInt16(IFormatProvider provider) => _jsonElement.GetUInt16();

        public uint ToUInt32(IFormatProvider provider) => _jsonElement.GetUInt32();

        public ulong ToUInt64(IFormatProvider provider) => _jsonElement.GetUInt64();
    }

    public static class JsonExtension
    {
        public static ConvertableJsonElem ToConvertable(this JsonElement jsonElement)
        {
            return new ConvertableJsonElem(jsonElement);
        }
    }
}
