using CsvToJsonWithMapping.Models;
using System.Text.Json;

namespace CsvToJsonWithMapping.Tests.EntireFlowTest
{
    public static class TestHelper
    {
        public static string GetRelationsJson(string jsonString)
        {
            return jsonString;
        }

        public static string GetMappingJson(string jsonString)
        {
            return jsonString; 
        }

        public static Dictionary<string, List<Dictionary<string, string>>> MockCsvData(
            Dictionary<string, List<Dictionary<string, string>>> data)
        {
            return data;
        }

        public static List<Relation> DeserializeRelations(string jsonString)
        {
            return JsonSerializer.Deserialize<List<Relation>>(jsonString) ?? new List<Relation>();
        }

        public static Mapping DeserializeMappings(string jsonString)
        {
            return JsonSerializer.Deserialize<Mapping>(jsonString) ?? new Mapping();
        }
    }
}
