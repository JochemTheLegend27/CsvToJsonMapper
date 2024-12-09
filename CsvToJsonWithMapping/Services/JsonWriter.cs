using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public static class JsonWriter
{
    public static void WriteJsonToFile(string filePath, List<Dictionary<string, object>> jsonData, ILogger logger)
    {
        var jsonOutput = JsonSerializer.Serialize(jsonData, new JsonSerializerOptions { WriteIndented = true });

        try
        {
            File.WriteAllText(filePath, jsonOutput);
            logger.LogInformation($"JSON output successfully written to file: {filePath}");
        }
        catch (Exception ex)
        {
            throw new Exception($"Error writing JSON output to file '{filePath}': {ex.Message}");
        }
    }
}
