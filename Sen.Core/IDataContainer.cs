using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Core
{
    public enum ServiceType : byte
    {
        EventData     = 0,
        OperationData = 1,
        PingData      = 2,
        EncryptData   = 3,
        ConfigData    = 4,
    }

    public enum DataType : byte
    {
        BOOL,
        BYTE,
        INT16,
        INT32,
        INT64,
        UINT16,
        UINT32,
        UINT64,
        FLOAT,
        DOUBLE,
        STRING,
        DICTIONARY,

        ARRAY = ARRAY_BOOL,
        ARRAY_BOOL = 16,
        ARRAY_BYTE,
        ARRAY_INT16,
        ARRAY_INT32,
        ARRAY_INT64,
        ARRAY_UINT16,
        ARRAY_UINT32,
        ARRAY_UINT64,
        ARRAY_FLOAT,
        ARRAY_DOUBLE,
        ARRAY_STRING,
    }

    /// <summary>
    /// Class that holds data to transfer to clients
    /// </summary>
    public interface IDataContainer
    {
        /// <summary>
        /// Type code of this <see cref="IDataContainer"/>, type code will be embeded to serialized stream.
        /// </summary>
        /// <returns></returns>
        byte ServiceCode { get; }

        byte Code { get; }

        Dictionary<byte, object> Parameters { get; set; }
    }

    public abstract class AbstractDataContainer : IDataContainer
    {
        /// <summary>
        /// Code number
        /// </summary>
        public byte Code { get; set; }

        /// <summary>
        /// Gets or sets the parameters that will be sent to or recieve from the client.
        /// </summary>
        public Dictionary<byte, object> Parameters { get; set; }

        /// <summary>
        /// Gets or sets the paramter associated with the specified key.
        /// </summary>
        /// <param name="parameterKey">The key of the parameter to get or set.</param>
        /// <returns>
        /// The parameter associated with the specified key. 
        /// If the specified key is not found, a get operation throws a KeyNotFoundException, 
        /// and a set operation creates a new paramter with the specified key.
        /// </returns>
        /// <exception cref="T:System.NullReferenceException">
        /// The <see cref="P:Photon.SocketServer.EventData.Parameters" /> property has not been initialized.
        /// </exception>
        public object this[byte parameterKey]
        {
            get
            {
                return this.Parameters[parameterKey];
            }
            set
            {
                this.Parameters[parameterKey] = value;
            }
        }

        public object this[Enum parameterKey]
        {
            get
            {
                return this.Parameters[(byte)(object)parameterKey];
            }
            set
            {
                this.Parameters[(byte)(object)parameterKey] = value;
            }
        }

        public abstract byte ServiceCode { get; }

        public string ToStringFull()
        {
            return ToStringFull(ReadableDataConverter.Default);
        }

        public string ToStringFull(ReadableDataConverter readableCodeConverter)
        {
            var sb = new StringBuilder(500);

            var codeName = readableCodeConverter.FromCode(Code);

            sb.Append(GetNameOfCodeType()).Append(" = ").Append(codeName).Append('(').Append(Code).Append(')').Append("{ \n");
            ParamsToString(sb, Parameters, readableCodeConverter);
            if (Parameters.Count == 0)
            {
                sb.Remove(sb.Length - 1, 1);
            }
            sb.Append('}');

            return sb.ToString();
        }

        protected void ParamsToString(StringBuilder buffer, Dictionary<byte, object> data, ReadableDataConverter readableDataConverter)
        {
            foreach (var item in data)
            {
                var paramName = readableDataConverter.FromParamKey(item.Key);
                buffer.Append("  ").Append(paramName).Append('(').Append(item.Key).Append(") : ");
                if (item.Value is Array)
                {
                    var array = item.Value as Array;
                    var type = item.Value.GetType().GetElementType().Name;
                    buffer.Append(type.ToUpper()).Append('[');
                    foreach (var i in array)
                    {
                        buffer.Append(i == null ? "null" : i.ToString()).Append(", ");
                    }
                    if (array.Length > 0)
                    {
                        buffer.Remove(buffer.Length - 2, 2);
                    }
                    buffer.Append(']');
                }
                else
                {
                    buffer.Append(item.Value.GetType().Name.ToUpper()).Append(' ').Append(item.Value.ToString());
                }
                buffer.Append('\n');
            }
        }

        public override string ToString()
        {
            return ToStringFull();
        }

        protected abstract string GetNameOfCodeType();
    }

    public class ReadableDataConverter
    {
        public string FromCode(byte code)
        {
            return string.Empty;
        }

        public string FromParamKey(byte key)
        {
            return string.Empty;
        }

        public static readonly ReadableDataConverter Default = new ReadableDataConverter();
    }
}
