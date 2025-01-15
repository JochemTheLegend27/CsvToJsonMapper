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
            string headerLine = reader.ReadLine() ?? "";

            if (string.IsNullOrEmpty(headerLine))
            {
                LogPublisher.PublishLogMessage("Warning - CSVData", $"The file {filePath} is empty.");
            }

            char separator = DetermineSeparator(headerLine);

            var csvConfig = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = separator.ToString()
            };

            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            reader.DiscardBufferedData();

            using var csv = new CsvReader(reader, csvConfig);

            foreach (var record in csv.GetRecords<dynamic>())
            {
                yield return ((IDictionary<string, object>)record)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString());
            }
        }

        private char DetermineSeparator(string headerLine)
        {
            var possibleSeparators = new[] { ',', ';', '\t', '|', ':', '~', '^', '-'};

            var separatorCounts = possibleSeparators
                .ToDictionary(separator => separator, separator => headerLine.Count(c => c == separator));

            var mostUsedSeparator = separatorCounts.OrderByDescending(kvp => kvp.Value).First();

            if (mostUsedSeparator.Value > 0)
            {
                return mostUsedSeparator.Key;
            }

            var fallbackSeparator = headerLine
                .Where(c => !char.IsLetterOrDigit(c))
                .GroupBy(c => c)
                .OrderByDescending(group => group.Count())
                .FirstOrDefault()?.Key;

            if (fallbackSeparator.HasValue)
            {
                LogPublisher.PublishLogMessage("Warning - CSVData", $"Using fallback separator: {fallbackSeparator}");
                return fallbackSeparator.Value;
            }

            LogPublisher.PublishLogMessage("Warning - CSVData", $"No valid separator found. Defaulting to comma (,).");
            return ',';
        }

    }
}
