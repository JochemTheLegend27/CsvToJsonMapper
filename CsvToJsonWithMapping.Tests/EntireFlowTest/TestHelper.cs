using CsvToJsonWithMapping.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CsvToJsonWithMapping.Tests.EntireFlowTest
{
    public static class TestHelper
    {
        public static string GetRelationsJson(string jsonString)
        {
            return jsonString; // Accepts test-defined JSON strings.
        }

        public static string GetMappingJson(string jsonString)
        {
            return jsonString; // Accepts test-defined JSON strings.
        }

        public static Dictionary<string, List<Dictionary<string, string>>> MockCsvData(
            Dictionary<string, List<Dictionary<string, string>>> data)
        {
            return data; // Return mocked data directly for tests.
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
