using CsvToJsonWithMapping.Models;
using CsvToJsonWithMapping.Services;
using Microsoft.Extensions.Logging;
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
        // Get the base directory of the current application
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

        // Define the file paths
        var csvFilesDirectory = Path.Combine(baseDirectory, @"..\..\..\CsvFiles");
        var mappingsFilePath = Path.Combine(baseDirectory, @"..\..\..\CsvToJsonMappings\mapping.json");
        var relationsFilePath = Path.Combine(baseDirectory, @"..\..\..\CsvToJsonMappings\relations.json");
        var outputJsonPath = Path.Combine(baseDirectory, @"..\..\..\finalOutput.json");

        CsvProcessorService csvProcessorService = new CsvProcessorService(new LoggingService(), new CsvFileReaderService(), new CsvDataJoinerService(), new JsonGeneratorService(new FieldValidationService()), new JsonWriterService());
        csvProcessorService.ProcessCsvFilesAsync(csvFilesDirectory, mappingsFilePath, relationsFilePath, outputJsonPath).Wait();
    }
}
