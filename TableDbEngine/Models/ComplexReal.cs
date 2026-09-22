using System.Globalization;
using System.Text.RegularExpressions;

namespace TableDbEngine.Models
{
    public class ComplexReal
    {
        public double RealPart { get; }
        public double ImaginaryPart { get; }

        public ComplexReal(double real, double imaginary)
        {
            RealPart = real;
            ImaginaryPart = imaginary;
        }

        public static ComplexReal Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new FormatException("Порожній рядок не є комплексним числом.");

            var cleaned = value.Replace(" ", "").Replace(',', '.');

            // 1. Повна форма: a+bi або a-bi
            var fullRegex = new Regex(@"^([+-]?\d+(\.\d+)?)([+-])(\d*(\.\d+)?)i$");
            var matchFull = fullRegex.Match(cleaned);
            if (matchFull.Success)
            {
                double real = double.Parse(matchFull.Groups[1].Value, CultureInfo.InvariantCulture);
                string sign = matchFull.Groups[3].Value;
                string coeff = matchFull.Groups[4].Value;

                double im = string.IsNullOrEmpty(coeff) ? 1.0 : double.Parse(coeff, CultureInfo.InvariantCulture);
                if (sign == "-") im = -im;

                return new ComplexReal(real, im);
            }

            // 2. Чисто уявна форма: bi
            var pureImRegex = new Regex(@"^([+-]?\d*(\.\d+)?)i$");
            var matchIm = pureImRegex.Match(cleaned);
            if (matchIm.Success)
            {
                string coeff = matchIm.Groups[1].Value;
                double im = 1.0;
                if (coeff == "-") im = -1.0;
                else if (coeff == "+" || string.IsNullOrEmpty(coeff)) im = 1.0;
                else im = double.Parse(coeff, CultureInfo.InvariantCulture);

                return new ComplexReal(0.0, im);
            }

            // 3. Чисто дійсна форма: a
            if (double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out double pureReal))
            {
                return new ComplexReal(pureReal, 0.0);
            }

            throw new FormatException($"Некоректний формат complexReal: '{value}'");
        }

        public override string ToString()
        {
            string r = RealPart.ToString(CultureInfo.InvariantCulture);
            string i = ImaginaryPart.ToString(CultureInfo.InvariantCulture);
            if (ImaginaryPart >= 0)
                return $"{r}+{i}i";
            return $"{r}{i}i";
        }
    }
}