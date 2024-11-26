using CsvHelper;
using CsvToJsonWithMapping.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;

/* TODO:
* Optimize the code less repetition
* Optimize the code more readable
* Optimize the code more maintainable (separate concerns (usage of different files/classes))
* 
* TESTs needed
* 
* 
* Rules implementation:
* - once a rule is broken, log the error and continue with the next field then dont create a output but log the error
* - under each mapping field there is a CSVField and CSVFile and there will be a Rule field
*      - rules can be:
*          - the field is required (cannot be empty or null)
*          - the field type (string, int, double, etc.) (it will try to parse to correct type, if not possible than error)
*          - the field length (min and max)
*          - Regex pattern (for example email, phone number, etc.)
*          - Converter (for example if the field is "yes" than convert to true, if "no" than convert to false, or when field value = 0 then it shpould become "pig")
*          - dependant on other field (for example if field A is not empty then field B should be empty)
*          
*          
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
        }
        catch (Exception ex)
        {
            throw new Exception($"An error occurred during processing: {ex.Message}");
        }
    }

    private static void CheckFileExists(string filePath, string fileType)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"{fileType} not found at path: {filePath}");
        }
    }

    private static void CheckDirectoryExists(string directoryPath, string directoryType)
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"{directoryType} not found: {directoryPath}");
        }
    }
}
