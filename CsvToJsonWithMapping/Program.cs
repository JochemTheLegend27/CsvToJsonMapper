using CsvHelper;
using CsvToJsonWithMapping.Models;
using CsvToJsonWithMapping.Services;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;

/* TODO: 
* Rules implementation:
* - under each mapping field there is a CSVField and CSVFile and there will be a Rule field
*      - rules can be:
*          - Regex pattern (for example email, phone number, etc.)
*          - dependant on other field (for example if field A is not empty then field B should be empty)         
*/

class Program
{
    static void Main(string[] args)
    {
        // Set up logging
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });
        var logger = loggerFactory.CreateLogger<Program>();


        // Get the base directory of the current application
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

        // Define the file paths
        var csvFilesDirectory = Path.Combine(baseDirectory, @"..\..\..\CsvFiles");
        var mappingsFilePath = Path.Combine(baseDirectory, @"..\..\..\CsvToJsonMappings\mapping.json");
        var relationsFilePath = Path.Combine(baseDirectory, @"..\..\..\CsvToJsonMappings\relations.json");
        var outputJsonPath = Path.Combine(baseDirectory, @"..\..\..\finalOutput.json");

        CheckFileExists(mappingsFilePath, "Mapping file");
        CheckFileExists(relationsFilePath, "Relations file");
        CheckDirectoryExists(csvFilesDirectory, "CSV file directory");

        logger.LogInformation("Begin met het verwerken van bestanden...");

        try
        {
            var relationsJson = File.ReadAllText(relationsFilePath);
            var relations = JsonSerializer.Deserialize<List<Relation>>(relationsJson);

            logger.LogInformation($"Relations: {JsonSerializer.Serialize(relations, new JsonSerializerOptions { WriteIndented = true })}");

            var duplicateRelations = relations
                .GroupBy(x => new { x.ForeignKey.CSVFileName, x.ForeignKey.CSVField })
                .Where(g => g.Count() > 1)
                .Select(y => y.Key)
                .ToList();

            if (duplicateRelations.Any())
            {
                throw new Exception($"Duplicate relations found in relations file.\n{JsonSerializer.Serialize(duplicateRelations, new JsonSerializerOptions { WriteIndented = true })}");
            }

            var mappingsJson = File.ReadAllText(mappingsFilePath);
            var mapping = JsonSerializer.Deserialize<Mapping>(mappingsJson);

            if (mapping == null)
            {
                throw new Exception("Could not deserialize mapping file.");
            }

            logger.LogInformation($"Mapping: {JsonSerializer.Serialize(mapping, new JsonSerializerOptions { WriteIndented = true })}");

            var csvFilePaths = Directory.GetFiles(csvFilesDirectory, "*.csv");
            if (csvFilePaths.Length == 0)
            {
                throw new FileNotFoundException($"No CSV files found in directory: {csvFilesDirectory}");
            }

            var csvData = CsvFileReader.ReadCsvFiles(csvFilePaths, logger);
            logger.LogInformation($"CSV-data: {JsonSerializer.Serialize(csvData, new JsonSerializerOptions { WriteIndented = true })}");

            var joinedData = CsvDataJoiner.JoinCsvDataBasedOnRelations(relations, csvData, logger);

            var finalResult = JsonGenerator.GenerateJsonFromMappings(mapping, relations, csvData, joinedData, logger);

            JsonWriter.WriteJsonToFile(outputJsonPath, finalResult, logger);

            var validationLogs = FieldValidator.GetLog();
            foreach (var logEntry in validationLogs)
            {
                Console.WriteLine($"{logEntry.Key}:");
                foreach (var message in logEntry.Value)
                {
                    Console.WriteLine($" - {message}");
                }
            }

            var mappingLogs = FieldValidator.GetLog();
            foreach (var logEntry in mappingLogs)
            {
                Console.WriteLine($"{logEntry.Key}:");
                foreach (var message in logEntry.Value)
                {
                    Console.WriteLine($" - {message}");
                }
            }

        }
        catch (Exception ex)
        {
            throw new Exception($"An error occurred during processing: {ex.Message}");
        }
    }

    internal static void CheckFileExists(string filePath, string fileType)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"{fileType} not found at path: {filePath}");
        }
    }

    internal static void CheckDirectoryExists(string directoryPath, string directoryType)
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"{directoryType} not found: {directoryPath}");
        }
    }
}
