using System.Globalization;
using TableDbEngine.Models;

namespace TableDbEngine.Services
{
    public static class TypeValidator
    {
        public static bool Validate(string value, DataType type)
        {
            if (value == null) return false;
            return type switch
            {
                DataType.Integer => int.TryParse(value, out _),
                DataType.Real => double.TryParse(value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out _),
                DataType.Char => value.Length == 1,
                DataType.String => true,
                DataType.ComplexInteger => TryParseComplexInt(value),
                DataType.ComplexReal => TryParseComplexReal(value),
                _ => false
            };
        }

        private static bool TryParseComplexInt(string value)
        {
            try { ComplexInteger.Parse(value); return true; }
            catch { return false; }
        }

        private static bool TryParseComplexReal(string value)
        {
            try { ComplexReal.Parse(value); return true; }
            catch { return false; }
        }
    }
}
