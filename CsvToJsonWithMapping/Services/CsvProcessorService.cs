using CsvToJsonWithMapping.Models;
using CsvToJsonWithMapping.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CsvToJsonWithMapping.Services
{
    public class CsvProcessorService
    {
        private readonly LoggingService _loggingService;
        private readonly CsvFileReaderService _csvFileReaderService;
        private readonly CsvDataJoinerService _csvDataJoinerService;
        private readonly JsonGeneratorService _jsonGeneratorService;
        private readonly JsonWriterService _jsonWriterService;

        public CsvProcessorService(LoggingService loggingService, CsvFileReaderService csvFileReaderService, CsvDataJoinerService csvDataJoinerService, JsonGeneratorService jsonGeneratorService, JsonWriterService jsonWriterService)
        {
            _loggingService = loggingService;
            _csvFileReaderService = csvFileReaderService;
            _csvDataJoinerService = csvDataJoinerService;
            _jsonGeneratorService = jsonGeneratorService;
            _jsonWriterService = jsonWriterService;
        }

        public Task<string> ProcessCsvFilesAsync(string csvFilesDirectory, string mappingsFilePath, string relationsFilePath, string outputJsonPath)
        {
            _loggingService.ClearLogs();
            CheckFileExists(mappingsFilePath, "Mapping file");
            CheckFileExists(relationsFilePath, "Relations file");
            CheckDirectoryExists(csvFilesDirectory, "CSV file directory");

            try
            {
                var relationsJson = File.ReadAllText(relationsFilePath);
                var relations = JsonSerializer.Deserialize<List<Relation>>(relationsJson);

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

                var requiredCsvFiles = GetUniqueCsvFiles(mapping, relations);

                var csvFilePaths = Directory.GetFiles(csvFilesDirectory, "*.csv");
               
                foreach (var csvFile in requiredCsvFiles)
                {
                    if (!csvFilePaths.Any(x => x.Contains(csvFile)))
                    {
                        throw new FileNotFoundException($"Required CSV file not found: {csvFile}");
                    }
                }

                var csvData = _csvFileReaderService.StreamCsvFiles(csvFilePaths);
                
                var joinedData = _csvDataJoinerService.JoinCsvDataBasedOnRelations(relations, csvData);

                var finalResult = _jsonGeneratorService.GenerateJsonFromMappings(mapping, relations, csvData, joinedData);

                _jsonWriterService.WriteJsonToFile(outputJsonPath, finalResult);

                return Task.FromResult(JsonSerializer.Serialize(finalResult, new JsonSerializerOptions { WriteIndented = true }));

            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred during processing: {ex.Message}");
            }
        }

        internal HashSet<string> GetUniqueCsvFiles(Mapping mapping, List<Relation> relations)
        {
            var csvFiles = new HashSet<string>();

            foreach (var field in mapping.Fields)
            {
                if (!string.IsNullOrEmpty(field.CSVFile) && !csvFiles.Contains(field.CSVFile))
                {
                    csvFiles.Add(field.CSVFile);
                }
            }

            foreach (var nestedField in mapping.NestedFields)
            {
                foreach (var field in nestedField.Fields)
                {
                    if (!string.IsNullOrEmpty(field.CSVFile) && !csvFiles.Contains(field.CSVFile))
                    {
                        csvFiles.Add(field.CSVFile);
                    }
                }

                foreach (var nested in nestedField.NestedFields)
                {
                    var nestedCsvFiles = GetUniqueCsvFilesForNestedMapping(nested);
                    foreach (var nestedCsvFile in nestedCsvFiles)
                    {
                        if (!string.IsNullOrEmpty(nestedCsvFile) && !csvFiles.Contains(nestedCsvFile))
                        {
                            csvFiles.Add(nestedCsvFile);
                        }
                    }
                }
            }


            foreach (var relation in relations)
            {
                if (!string.IsNullOrEmpty(relation.ForeignKey.CSVFileName) && !csvFiles.Contains(relation.ForeignKey.CSVFileName))
                {
                    csvFiles.Add(relation.ForeignKey.CSVFileName);
                }

                if (!string.IsNullOrEmpty(relation.PrimaryKey.CSVFileName) && !csvFiles.Contains(relation.PrimaryKey.CSVFileName))
                {
                    csvFiles.Add(relation.PrimaryKey.CSVFileName);
                }
            }

            return csvFiles;
        }


        private HashSet<string> GetUniqueCsvFilesForNestedMapping(NestedMapping nestedMapping)
        {
            var csvFiles = new HashSet<string>();

            foreach (var field in nestedMapping.Fields)
            {
                if (!string.IsNullOrEmpty(field.CSVFile))
                {
                    csvFiles.Add(field.CSVFile);
                }
            }

            foreach (var nested in nestedMapping.NestedFields)
            {
                var nestedCsvFiles = GetUniqueCsvFilesForNestedMapping(nested);
                foreach (var nestedCsvFile in nestedCsvFiles)
                {
                    csvFiles.Add(nestedCsvFile);
                }
            }

            return csvFiles;
        }


        internal void CheckFileExists(string filePath, string fileType)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"{fileType} not found at path: {filePath}");
            }
        }

        internal void CheckDirectoryExists(string directoryPath, string directoryType)
        {
            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"{directoryType} not found: {directoryPath}");
            }
        }
    }
}
