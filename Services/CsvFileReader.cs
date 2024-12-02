using CsvHelper;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

public static class CsvFileReader
{
    /// <summary>
    /// Reads multiple CSV files and returns the data as a dictionary.
    /// </summary>
    /// <param name="csvFilePaths">The paths of the CSV files to read.</param>
    /// <param name="logger">The logger to log information and errors.</param>
    /// <returns>A dictionary containing the CSV data, where the key is the file name with extension and the value is a list of dictionaries representing the records.</returns>
    public static Dictionary<string, List<Dictionary<string, string>>> ReadCsvFiles(IEnumerable<string> csvFilePaths, ILogger logger)
    {
        var csvData = new Dictionary<string, List<Dictionary<string, string>>>();

        foreach (var filePath in csvFilePaths)
        {
            try
            {
                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                string fileNameWithExtension = Path.GetFileName(filePath);
                var records = csv.GetRecords<dynamic>().ToList();
                csvData[fileNameWithExtension] = records.Select(record => (IDictionary<string, object>)record)
                                            .Select(dict => dict.ToDictionary(k => k.Key, k => k.Value?.ToString() ?? "null"))
                                            .ToList();

                logger.LogInformation($"Successfully read CSV file: {fileNameWithExtension}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error reading CSV file '{filePath}': {ex.Message}");
            }
        }

        return csvData;
    }
}
