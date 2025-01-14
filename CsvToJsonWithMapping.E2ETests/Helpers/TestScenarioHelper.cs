using CsvToJsonWithMapping.Models;
using System.Text.Json;

namespace CsvToJsonWithMapping.AlgorithmTests.Helpers
{
    public static class TestScenarioHelper
    {
        public static List<Relation> LoadRelations(string path)
        {
            return JsonSerializer.Deserialize<List<Relation>>(File.ReadAllText(path)) ?? new List<Relation>();
        }

        public static Mapping LoadMapping(string path)
        {
            return JsonSerializer.Deserialize<Mapping>(File.ReadAllText(path)) ?? new Mapping();
        }

        public static Dictionary<string, IEnumerable<IDictionary<string, string?>>> LoadCsvData(string path)
        {
            return JsonSerializer.Deserialize<Dictionary<string, IEnumerable<IDictionary<string, string?>>>>(File.ReadAllText(path))
                   ?? new Dictionary<string, IEnumerable<IDictionary<string, string?>>>();
        }

        public static object LoadExpectedOutput(string path)
        {
            return JsonSerializer.Deserialize<object>(File.ReadAllText(path));
        }

        public static IEnumerable<object[]> GetAllTestScenarios()
        {
            var scenarioRoot = "TestScenarios/";
            var directories = Directory.GetDirectories(scenarioRoot);

            foreach (var dir in directories)
            {
                var relationsPath = Path.Combine(dir, "relations.json");
                var mappingPath = Path.Combine(dir, "mapping.json");
                var csvDataPath = Path.Combine(dir, "csv_data.json");
                var expectedOutputPath = Path.Combine(dir, "expected_output.json");

                if (File.Exists(relationsPath) &&
                    File.Exists(mappingPath) &&
                    File.Exists(csvDataPath) &&
                    File.Exists(expectedOutputPath))
                {
                    yield return new object[]
                    {
                    relationsPath,
                    mappingPath,
                    csvDataPath,
                    expectedOutputPath
                    };
                }
            }
        }
    }
}
