namespace TableDbEngine.Models
{
    public class Cell
    {
        public string RawValue { get; set; } = string.Empty;
        public Cell() { }
        public Cell(string value) => RawValue = value;
    }

    public class Column
    {
        public string Name { get; set; } = string.Empty;
        public DataType Type { get; set; }
        public Column() { }
        public Column(string name, DataType type) { Name = name; Type = type; }
    }

    public class Row
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public List<Cell> Cells { get; set; } = new();
        public Row() { }
        public Row(IEnumerable<string> values)
        {
            Cells = values.Select(v => new Cell(v)).ToList();
        }
    }

    public class Table
    {
        public string Name { get; set; } = string.Empty;
        public List<Column> Columns { get; set; } = new();
        public List<Row> Rows { get; set; } = new();

        public Table() { }
        public Table(string name) => Name = name;

        public void AddRow(Row row) => Rows.Add(row);
        public void RemoveRowAt(int index) => Rows.RemoveAt(index);
    }

    public class Database
    {
        public string Name { get; set; } = string.Empty;
        public List<Table> Tables { get; set; } = new();

        public Database() { }
        public Database(string name) => Name = name;
    }
}
