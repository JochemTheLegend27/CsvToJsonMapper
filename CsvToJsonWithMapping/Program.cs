using CsvToJsonWithMapping.Services;

class Program
{
    static async Task Main(string[] args)
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

        var csvFilesDirectory = Path.Combine(baseDirectory, @"..\..\..\CsvFiles");
        var mappingsFilePath = Path.Combine(baseDirectory, @"..\..\..\CsvToJsonMappings\mapping.json");
        var relationsFilePath = Path.Combine(baseDirectory, @"..\..\..\CsvToJsonMappings\relations.json");
        var outputJsonPath = Path.Combine(baseDirectory, @"..\..\..\finalOutput.json");

        var logService = new LoggingService();
        var csvProcessorService = new CsvProcessorService(
            logService,
            new CsvFileReaderService(),
            new CsvDataJoinerService(),
            new JsonGeneratorService(new FieldValidationService()),
            new JsonWriterService()
        );

        logService.OnProgressUpdated += progress => HandleProgressUpdated(progress, logService);

        try
        {
            await csvProcessorService.ProcessCsvFilesAsync(csvFilesDirectory, mappingsFilePath, relationsFilePath, outputJsonPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Initialization failed: {ex.Message}");
        }
    }

    private static void HandleProgressUpdated(double progress, LoggingService logService)
    {
        Console.WriteLine($"{logService.GetProgress()}% completed");
    }
}
