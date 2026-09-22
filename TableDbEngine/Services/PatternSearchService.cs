using System.Text.RegularExpressions;
using TableDbEngine.Models;

namespace TableDbEngine.Services
{
    public class PatternMatcher
    {
        public static string WildcardToRegex(string pattern)
        {
            string escaped = Regex.Escape(pattern);
            return "^" + escaped.Replace("\\*", ".*").Replace("\\?", ".") + "$";
        }

        public bool Matches(string value, string pattern)
        {
            if (value == null || pattern == null) return false;
            string regexStr = WildcardToRegex(pattern);
            return Regex.IsMatch(value, regexStr, RegexOptions.IgnoreCase);
        }
    }

    public class PatternSearchService
    {
        private readonly PatternMatcher _matcher = new();

        public List<Row> Search(Table table, string columnName, string pattern)
        {
            var colIndex = table.Columns.FindIndex(c => c.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase));
            if (colIndex == -1)
                throw new ArgumentException($"Колонку '{columnName}' не знайдено в таблиці.");

            var results = new List<Row>();
            foreach (var row in table.Rows)
            {
                if (colIndex < row.Cells.Count)
                {
                    string cellValue = row.Cells[colIndex].RawValue;
                    if (_matcher.Matches(cellValue, pattern))
                    {
                        results.Add(row);
                    }
                }
            }
            return results;
        }
    }
}
