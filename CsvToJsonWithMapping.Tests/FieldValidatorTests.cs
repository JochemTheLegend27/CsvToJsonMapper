using CsvToJsonWithMapping.Models;
using CsvToJsonWithMapping.Services;
using Xunit;

namespace CsvToJsonWithMapping.Tests
{
    public class FieldValidatorTests
    {
        [Fact]
        public void ProcessFieldValidation_ShouldPassAndConvert_WhenValidTypeAndNoErrors()
        {
            // Arrange
            var fieldMapping = new FieldMapping
            {
                JSONField = "Field1",
                CSVField = "Column1",
                ConversionRules = new()
                {
                    { "valid", "convertedValue" }
                },
                Validations = new Validations
                {
                    Required = true,
                    Type = "string",
                    Min = null,
                    Max = null,
                    ValidationsNeedToPass = false
                }
            };

            // Act
            var result = FieldValidator.ProcessFieldValidation("valid", fieldMapping);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("convertedValue", result);
        }

        [Fact]
        public void ProcessFieldValidation_ShouldFail_IfRequiredFieldIsMissing()
        {
            // Arrange
            var fieldMapping = new FieldMapping
            {
                JSONField = "Field1",
                CSVField = "Column1",
                ConversionRules = new(),
                Validations = new Validations
                {
                    Required = true,
                    Type = "string",
                    Min = null,
                    Max = null,
                    ValidationsNeedToPass = true
                }
            };

            // Act & Assert
            var exception = Assert.Throws<Exception>(() =>
                FieldValidator.ProcessFieldValidation(null, fieldMapping)
            );

            Assert.Contains("is required but", exception.Message);
        }

        [Fact]
        public void ProcessFieldValidation_ShouldHandleIntValidationCorrectly()
        {
            // Arrange
            var fieldMapping = new FieldMapping
            {
                JSONField = "Field1",
                CSVField = "Column1",
                ConversionRules = new(),
                Validations = new Validations
                {
                    Required = true,
                    Type = "Int",
                    Min = 0,
                    Max = 100,
                    ValidationsNeedToPass = true
                }
            };

            // Act
            var result = FieldValidator.ProcessFieldValidation("10", fieldMapping);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<int>(result);
            Assert.Equal(10, result);
        }

        [Fact]
        public void ProcessFieldValidation_ShouldFail_IfOutOfRange()
        {
            // Arrange
            var fieldMapping = new FieldMapping
            {
                JSONField = "Field1",
                CSVField = "Column1",
                ConversionRules = new(),
                Validations = new Validations
                {
                    Required = true,
                    Type = "Int",
                    Min = 0,
                    Max = 10,
                    ValidationsNeedToPass = true
                }
            };

            // Act & Assert
            var exception = Assert.Throws<Exception>(() =>
                FieldValidator.ProcessFieldValidation("20", fieldMapping)
            );

            Assert.Contains("too large", exception.Message);
        }

        [Fact]
        public void ProcessFieldValidation_ShouldHandleDoubleTypeValidation()
        {
            // Arrange
            var fieldMapping = new FieldMapping
            {
                JSONField = "Field2",
                CSVField = "Column2",
                ConversionRules = new(),
                Validations = new Validations
                {
                    Required = true,
                    Type = "Double",
                    Min = 0.0,
                    Max = 100.0,
                    ValidationsNeedToPass = true
                }
            };

            // Act
            var result = FieldValidator.ProcessFieldValidation("50.5", fieldMapping);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<double>(result);
            Assert.Equal(50.5, result);
        }

        [Fact]
        public void ProcessFieldValidation_ShouldHandleBooleanValidationCorrectly()
        {
            // Arrange
            var fieldMapping = new FieldMapping
            {
                JSONField = "Field3",
                CSVField = "Column3",
                ConversionRules = new(),
                Validations = new Validations
                {
                    Required = true,
                    Type = "Bool",
                    Min = null,
                    Max = null,
                    ValidationsNeedToPass = true
                }
            };

            // Act
            var result = FieldValidator.ProcessFieldValidation("true", fieldMapping);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<bool>(result);
            Assert.Equal(true, result);
        }

        [Fact]
        public void ProcessFieldValidation_ShouldHandleEmptyValuesAsNull()
        {
            // Arrange
            var fieldMapping = new FieldMapping
            {
                JSONField = "Field4",
                CSVField = "Column4",
                ConversionRules = new(),
                Validations = new Validations
                {
                    Required = false,
                    Type = "string",
                    Min = null,
                    Max = null,
                    ValidationsNeedToPass = false
                }
            };

            // Act
            var result = FieldValidator.ProcessFieldValidation("", fieldMapping);

            // Assert
            Assert.Null(result);
        }
    }
}
