using CsvToJsonWithMapping.Services;
using System.Text;
using System.Text.Json;

class Program
{
    static async Task Main(string[] args)
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

        var csvFilesDirectory = Path.Combine(baseDirectory, @"..\..\..\CsvFiles");
        var mappingsFilePath = Path.Combine(baseDirectory, @"..\..\..\CsvToJsonMappings\mapping.json");
        var relationsFilePath = Path.Combine(baseDirectory, @"..\..\..\CsvToJsonMappings\relations.json");
        var outputJsonPath = Path.Combine(baseDirectory, @"..\..\..\finalOutput.json");

        var logsOutputPath = Path.Combine(baseDirectory, @"..\..\..\logs.csv");

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
            var logs = logService.GetLogs();

            var groupedLogs = GroupLogs(logs);

            WriteLogsToFile(logsOutputPath, groupedLogs);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Initialization failed: {ex.Message}");
        }
    }

    private static void HandleProgressUpdated(double progress, LoggingService logService)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {Math.Round(progress)}% completed");
    }

    private static Dictionary<string, Dictionary<string, int>> GroupLogs(Dictionary<string, List<string>> logs)
    {
        var groupedLogs = new Dictionary<string, Dictionary<string, int>>();

        foreach (var logCategory in new[] { "Warning", "Error", "Information" })
        {
            groupedLogs[logCategory] = logs
                .Where(log => log.Key.Contains(logCategory))
                .SelectMany(log => log.Value)
                .GroupBy(message => message)
                .ToDictionary(
                    group => group.Key,
                    group => group.Count()
                );
        }

        return groupedLogs;
    }

    private static void WriteLogsToFile(string filePath, Dictionary<string, Dictionary<string, int>> logs)
    {
        try
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Writing logs to file");

            var sb = new StringBuilder();
            sb.AppendLine("LogType,LogMessage,OccurrenceCount");

            foreach (var logType in logs)
            {
                foreach (var logEntry in logType.Value)
                {
                    sb.AppendLine($"\"{logType.Key}\",\"{logEntry.Key}\",{logEntry.Value}");
                }
            }

            File.WriteAllText(filePath, sb.ToString());
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Logs written successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Failed to write logs: {ex.Message}");
        }
    }
}
