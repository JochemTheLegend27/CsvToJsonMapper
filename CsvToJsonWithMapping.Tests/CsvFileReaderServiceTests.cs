using CsvToJsonWithMapping.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace CsvToJsonWithMapping.Tests
{
    public class CsvFileReaderServiceTests
    {
        [Fact]
        public void DetermineSeparator_ShouldReturnMostUsedSeparator()
        {
            // Arrange
            var csvFileReaderService = new CsvFileReaderService();
            string headerLine = "Name,Age,Location";

            // Act
            var result = InvokeDetermineSeparator(csvFileReaderService, headerLine);

            // Assert
            Assert.Equal(',', result);
        }

        [Fact]
        public void DetermineSeparator_ShouldReturnMostFrequentSeparator_WhenMultipleSeparatorsArePresent()
        {
            // Arrange
            var csvFileReaderService = new CsvFileReaderService();
            string headerLine = "Name Age;Location-Name;City Age";

            // Act
            var result = InvokeDetermineSeparator(csvFileReaderService, headerLine);

            // Assert
            Assert.Equal(';', result);
        }

        [Fact]
        public void DetermineSeparator_ShouldUseMostFrequentNonAlphanumeric_WhenNoStandardSeparatorExists()
        {
            // Arrange
            var csvFileReaderService = new CsvFileReaderService();
            string headerLine = "Name Age Location";

            // Act
            var result = InvokeDetermineSeparator(csvFileReaderService, headerLine);

            // Assert
            Assert.Equal(' ', result);
        }

        [Fact]
        public void DetermineSeparator_ShouldHandleMultipleSeparatorTypes()
        {
            // Arrange
            var csvFileReaderService = new CsvFileReaderService();
            string headerLine = "Name|Age|Location|City|Country";

            // Act
            var result = InvokeDetermineSeparator(csvFileReaderService, headerLine);

            // Assert
            Assert.Equal('|', result);
        }

        [Fact]
        public void StreamCsv_ShouldCorrectlyParseFileWithCustomSeparator()
        {
            // Arrange
            var csvFileReaderService = new CsvFileReaderService();
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var testFilePath = Path.Combine(baseDirectory, "TestData", "sample_pipe_separated.csv");

            File.WriteAllText(testFilePath, "Name|Age|Location\nJohn|30|USA\nJane|25|UK");

            // Act
            var result = csvFileReaderService.StreamCsvFiles(new[] { testFilePath });

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ContainsKey("sample_pipe_separated.csv"));
            var data = result["sample_pipe_separated.csv"].ToList();

            Assert.Equal(2, data.Count);
            Assert.Equal("John", data[0]["Name"]);
            Assert.Equal("30", data[0]["Age"]);
            Assert.Equal("USA", data[0]["Location"]);
        }


        private char InvokeDetermineSeparator(CsvFileReaderService service, string headerLine)
        {
            // Use reflection to access the private method
            var method = typeof(CsvFileReaderService).GetMethod("DetermineSeparator",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (char)method.Invoke(service, new object[] { headerLine });
        }
    }
}
