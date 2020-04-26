using System;
using System.Collections.Generic;
using System.Text;

namespace Sen.Utilities.Configuration
{
    public abstract class ValidateAttribute : Attribute
    {
        public abstract void Validate(object value, string name);
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class IntegerInRangeAttribute : ValidateAttribute
    {
        public IntegerInRangeAttribute()
        {
            Min = int.MinValue;
            Max = int.MaxValue;
        }

        public int Min { get; set; }

        public int Max { get; set; }

        public override void Validate(object value, string name)
        {
            var int_value = (int)value;
            if (int_value < Min || int_value > Max)
            {
                throw new InvalidValueException(string.Format("{0} must be in range of [{1}, {2}], current {3}", name, Min, Max, value));
            }
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class FloatInRangeAttribute : ValidateAttribute
    {
        public FloatInRangeAttribute()
        {
            Min = float.MinValue;
            Max = float.MaxValue;
        }

        public float Min { get; set; }

        public float Max { get; set; }

        public override void Validate(object value, string name)
        {
            var int_value = (float)value;
            if (int_value < Min || int_value > Max)
            {
                throw new InvalidValueException(string.Format("{0} must be in range of [{1}, {2}], current {3}", name, Min, Max, value));
            }
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class DoubleInRangeAttribute : ValidateAttribute
    {
        public DoubleInRangeAttribute()
        {
            Min = double.MinValue;
            Max = double.MaxValue;
        }

        public double Min { get; set; }

        public double Max { get; set; }

        public override void Validate(object value, string name)
        {
            var int_value = (double)value;
            if (int_value < Min || int_value > Max)
            {
                throw new InvalidValueException(string.Format("{0} must be in range of [{1}, {2}], current {3}", name, Min, Max, value));
            }
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class ArrayLengthAttribute : ValidateAttribute
    {
        public ArrayLengthAttribute(int length)
        {
            _length = length;
        }

        private readonly int _length;

        public override void Validate(object value, string name)
        {
            var length = ((Array)value).Length;
            if (length != _length)
            {
                throw new InvalidValueException(string.Format("{0} config array length is not valid, required {1}, has {2}", name, _length, length));
            }
        }
    }
}
