using CsvHelper;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace CsvToJsonWithMapping.Services
{
    public class CsvFileReaderService
    {
        /// <summary>
        /// Streams CSV files and returns their data incrementally.
        /// </summary>
        /// <param name="csvFilePaths">Paths of the CSV files to read.</param>
        /// <returns>An enumerable for each file's data, allowing for streaming.</returns>
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
