using System.Text.RegularExpressions;

namespace TableDbEngine.Models
{
    public class ComplexInteger
    {
        public int RealPart { get; }
        public int ImaginaryPart { get; }

        public ComplexInteger(int real, int imaginary)
        {
            RealPart = real;
            ImaginaryPart = imaginary;
        }

        public static ComplexInteger Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new FormatException("Порожній рядок не є комплексним числом.");

            var cleaned = value.Replace(" ", "");

            var fullRegex = new Regex(@"^([+-]?\d+)([+-])(\d*)i$");
            var matchFull = fullRegex.Match(cleaned);
            if (matchFull.Success)
            {
                int real = int.Parse(matchFull.Groups[1].Value);
                string sign = matchFull.Groups[2].Value;
                string coeff = matchFull.Groups[3].Value;

                int im = string.IsNullOrEmpty(coeff) ? 1 : int.Parse(coeff);
                if (sign == "-") im = -im;

                return new ComplexInteger(real, im);
            }

            var pureImRegex = new Regex(@"^([+-]?\d*)i$");
            var matchIm = pureImRegex.Match(cleaned);
            if (matchIm.Success)
            {
                string coeff = matchIm.Groups[1].Value;
                int im = 1;
                if (coeff == "-" || coeff == "-1") im = -1;
                else if (coeff == "+" || coeff == "+1" || string.IsNullOrEmpty(coeff)) im = 1;
                else im = int.Parse(coeff);

                return new ComplexInteger(0, im);
            }

            if (int.TryParse(cleaned, out int pureReal))
            {
                return new ComplexInteger(pureReal, 0);
            }

            throw new FormatException($"Некоректний формат complexInteger: '{value}'");
        }

        public override string ToString()
        {
            if (ImaginaryPart >= 0)
                return $"{RealPart}+{ImaginaryPart}i";
            return $"{RealPart}{ImaginaryPart}i";
        }
    }
}