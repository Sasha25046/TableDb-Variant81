using System.IO;
using Xunit;
using TableDbEngine.Models;
using TableDbEngine.Services;

namespace TableDbEngine.Tests
{
    public class DbEngineTests
    {
        [Fact]
        public void Test1_TypeValidation_ValidatesCorrectly()
        {
            Assert.True(TypeValidator.Validate("105", DataType.Integer));
            Assert.False(TypeValidator.Validate("105.4", DataType.Integer));
            Assert.True(TypeValidator.Validate("-14.8", DataType.Real));
            Assert.True(TypeValidator.Validate("a", DataType.Char));
            Assert.False(TypeValidator.Validate("abc", DataType.Char));
        }

        [Theory]
        [InlineData("3+4i", 3, 4)]
        [InlineData("-2-i", -2, -1)]
        [InlineData("i", 0, 1)]
        [InlineData("5i", 0, 5)]
        [InlineData("-7", -7, 0)]
        public void Test2_ComplexIntegerParsing_HandlesCanonicalAndEdgeCases(string input, int expReal, int expIm)
        {
            var complex = ComplexInteger.Parse(input);
            Assert.Equal(expReal, complex.RealPart);
            Assert.Equal(expIm, complex.ImaginaryPart);
        }

        [Fact]
        public void Test3_PatternSearch_MatchesWildcardsOnComplexValues()
        {
            var table = new Table("Sensors");
            table.Columns.Add(new Column("Impedance", DataType.ComplexInteger));
            table.Columns.Add(new Column("Tag", DataType.String));

            table.AddRow(new Row(new[] { "3+4i", "Alpha" }));
            table.AddRow(new Row(new[] { "-3+4i", "Beta" }));
            table.AddRow(new Row(new[] { "3-2i", "Gamma" }));
            table.AddRow(new Row(new[] { "15+4i", "Delta" }));

            var searchService = new PatternSearchService();

            // Пошук за wildcard зірочкою: закінчується на +4i
            var found = searchService.Search(table, "Impedance", "*+4i");

            Assert.Equal(3, found.Count);
            Assert.Contains(found, r => r.Cells[1].RawValue == "Alpha");
            Assert.Contains(found, r => r.Cells[1].RawValue == "Beta");
            Assert.Contains(found, r => r.Cells[1].RawValue == "Delta");

            // Пошук з ? (рівно 1 символ)
            var singleCharMatch = searchService.Search(table, "Impedance", "?+4i");
            Assert.Single(singleCharMatch);
            Assert.Equal("Alpha", singleCharMatch[0].Cells[1].RawValue);
        }

        [Fact]
        public void Test4_JsonStorage_SaveAndLoadPreservesData()
        {
            var db = new Database("TestDb");
            var table = new Table("Users");
            table.Columns.Add(new Column("Age", DataType.Integer));
            table.AddRow(new Row(new[] { "20" }));
            db.Tables.Add(table);

            var storage = new JsonDbStorage();
            string tempFile = Path.GetTempFileName();

            try
            {
                storage.Save(db, tempFile);
                var loadedDb = storage.Load(tempFile);

                Assert.Equal("TestDb", loadedDb.Name);
                Assert.Single(loadedDb.Tables);
                Assert.Equal("20", loadedDb.Tables[0].Rows[0].Cells[0].RawValue);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }
    }
}
