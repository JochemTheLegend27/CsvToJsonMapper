using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Moq;

namespace CsvToJsonWithMapping.Tests
{
    public class CsvFileTests
    {
        [Fact]
        public void CheckFileExists_ShouldThrowException_WhenFileDoesNotExist()
        {
            // Arrange
            var nonExistentFilePath = "nonexistent.json";

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => Program.CheckFileExists(nonExistentFilePath, "Test File"));
        }

        [Fact]
        public void ReadCsvFiles_ShouldReturnCorrectData_FromTestDataFolder()
        {
            // Arrange
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var csvFilePath = Path.Combine(baseDirectory, "TestData", "sample.csv");

            var mockLogger = Mock.Of<ILogger>();

            // Act
            var result = CsvFileReader.ReadCsvFiles(new[] { csvFilePath }, mockLogger);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ContainsKey("sample.csv"));
            Assert.Equal(2, result["sample.csv"].Count);
            Assert.Equal("Value1", result["sample.csv"][0]["Header1"]);
        }


    }
}