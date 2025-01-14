using CsvHelper;
using System.Globalization;

namespace CsvToJsonWithMapping.Services
{
    public class CsvFileReaderService
    {
        public Dictionary<string, IEnumerable<IDictionary<string, string?>>> StreamCsvFiles(IEnumerable<string> csvFilePaths)
        {
            var csvData = new Dictionary<string, IEnumerable<IDictionary<string, string?>>>();

            foreach (var filePath in csvFilePaths)
            {
                string fileNameWithExtension = Path.GetFileName(filePath);

                csvData[fileNameWithExtension] = StreamCsv(filePath);
            }

            return csvData;
        }

        private IEnumerable<IDictionary<string, string?>> StreamCsv(string filePath)
        {
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            foreach (var record in csv.GetRecords<dynamic>())
            {
                yield return ((IDictionary<string, object>)record)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString());
            }
        }
    }
}
