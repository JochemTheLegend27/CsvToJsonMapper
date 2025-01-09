using CsvToJsonWithMapping.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Moq;

namespace CsvToJsonWithMapping.Tests
{
    public class CsvFileServiceTests
    {
        [Fact]
        public void CheckFileExists_ShouldThrowException_WhenFileDoesNotExist()
        {
            // Arrange
            var mockLoggingService = new Mock<LoggingService>();
            var mockCsvFileReaderService = new Mock<CsvFileReaderService>();
            var mockCsvDataJoinerService = new Mock<CsvDataJoinerService>();
            var mockFieldValidationService = new Mock<FieldValidationService>();
            var mockJsonGeneratorService = new Mock<JsonGeneratorService>(mockFieldValidationService.Object);
            var mockJsonWriterService = new Mock<JsonWriterService>();

            var csvProcessorService = new CsvProcessorService(
                mockLoggingService.Object,
                mockCsvFileReaderService.Object,
                mockCsvDataJoinerService.Object,
                mockJsonGeneratorService.Object,
                mockJsonWriterService.Object
            );

            var nonExistentFilePath = "nonexistent.json";

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => csvProcessorService.CheckFileExists(nonExistentFilePath, "Test File"));
        }

        [Fact]
        public void ReadCsvFiles_ShouldReturnCorrectData_FromTestDataFolder()
        {
            // Arrange
            var csvFileReaderService = new CsvFileReaderService();
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var csvFilePath = Path.Combine(baseDirectory, "TestData", "sample.csv");

            var mockLogger = Mock.Of<ILogger>();

            // Act
            var result = csvFileReaderService.StreamCsvFiles(new[] { csvFilePath });

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ContainsKey("sample.csv"));
            var sampleData = result["sample.csv"].ToList(); // Convert to List to access by index

            Assert.Equal(2, sampleData.Count);
            Assert.Equal("Value1", sampleData[0]["Header1"]);
        }


    }
}