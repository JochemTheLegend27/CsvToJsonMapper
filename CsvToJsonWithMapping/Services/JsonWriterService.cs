using System.Text.Json;

namespace CsvToJsonWithMapping.Services
{
    public class JsonWriterService
    {

        public void WriteJsonToFile(string filePath, List<Dictionary<string, object>> jsonData)
        {
            var jsonOutput = JsonSerializer.Serialize(jsonData, new JsonSerializerOptions { WriteIndented = true });

            try
            {
                File.WriteAllText(filePath, jsonOutput);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error writing JSON output to file '{filePath}': {ex.Message}");
            }
        }
    }
}
