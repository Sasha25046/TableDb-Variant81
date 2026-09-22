using System.IO;
using System.Text.Json;
using TableDbEngine.Models;

namespace TableDbEngine.Services
{
    public class JsonDbStorage
    {
        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        public void Save(Database db, string filePath)
        {
            var json = JsonSerializer.Serialize(db, Options);
            File.WriteAllText(filePath, json);
        }

        public Database Load(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Файл бази даних не знайдено.");

            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<Database>(json, Options) ?? new Database("NewDB");
        }
    }
}
