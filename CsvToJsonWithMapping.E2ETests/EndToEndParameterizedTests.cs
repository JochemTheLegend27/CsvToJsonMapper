using CsvToJsonWithMapping.E2ETests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CsvToJsonWithMapping.E2ETests
{
    public class EndToEndParameterizedTests
    {
        [Theory]
        [MemberData(nameof(GetTestScenarios))]
        public void TestEndToEndFlow(string relationsPath, string mappingPath, string csvDataPath, string expectedOutputPath)
        {
            var mockLogger = new Mock<ILogger>();

            // Load JSON dynamically
            var relations = TestScenarioHelper.LoadRelations(relationsPath);
            var mapping = TestScenarioHelper.LoadMapping(mappingPath);
            var csvData = TestScenarioHelper.LoadCsvData(csvDataPath);
            var expectedOutput = TestScenarioHelper.LoadExpectedOutput(expectedOutputPath);

            // Act
            var joinedData = CsvDataJoiner.JoinCsvDataBasedOnRelations(relations, csvData, mockLogger.Object);
            var finalResult = JsonGenerator.GenerateJsonFromMappings(mapping, relations, csvData, joinedData, mockLogger.Object);

            // Assert
            Assert.NotNull(finalResult);
            Assert.NotEmpty(finalResult);

            // Compare the actual result with expected
            Assert.True(JsonEquals(finalResult, expectedOutput), $"Actual output did not match the expected output. \nActual: {JsonSerializer.Serialize(finalResult)} \nExpected: {JsonSerializer.Serialize(expectedOutput)}");
        }

        public static IEnumerable<object[]> GetTestScenarios()
        {
            return TestScenarioHelper.GetAllTestScenarios();
        }

        private bool JsonEquals(object actual, object expected)
        {
            var actualJson = JsonSerializer.Serialize(actual, new JsonSerializerOptions { WriteIndented = true });
            var expectedJson = JsonSerializer.Serialize(expected, new JsonSerializerOptions { WriteIndented = true });

            return actualJson == expectedJson;
        }
    }


}
