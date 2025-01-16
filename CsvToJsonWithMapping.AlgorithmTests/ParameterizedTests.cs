using CsvToJsonWithMapping.AlgorithmTests.Helpers;
using CsvToJsonWithMapping.Services;
using Moq;
using System.Text.Json;

namespace CsvToJsonWithMapping.AlgorithmTests
{
    public class ParameterizedTests
    {
        [Theory]
        [MemberData(nameof(GetTestScenarios))]
        public void AlgorithmFlow(string relationsPath, string mappingPath, string csvDataPath, string expectedOutputPath)
        {
            var mockFieldValidationService = new Mock<FieldValidationService>();

            var csvDataJoinerService = new CsvDataJoinerService();
            var jsonGeneratorService = new JsonGeneratorService(mockFieldValidationService.Object);

            var relations = TestScenarioHelper.LoadRelations(relationsPath);
            var mapping = TestScenarioHelper.LoadMapping(mappingPath);
            var csvData = TestScenarioHelper.LoadCsvData(csvDataPath);
            var expectedOutput = TestScenarioHelper.LoadExpectedOutput(expectedOutputPath);

            // Act
            var joinedData = csvDataJoinerService.JoinCsvDataBasedOnRelations(relations, csvData);
            var finalResult = jsonGeneratorService.GenerateJsonFromMappings(mapping, relations, csvData, joinedData);

            // Assert
            Assert.NotNull(finalResult);
            Assert.NotEmpty(finalResult);

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
