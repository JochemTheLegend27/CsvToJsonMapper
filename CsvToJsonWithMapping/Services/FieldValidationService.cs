using CsvToJsonWithMapping.Models;

namespace CsvToJsonWithMapping.Services
{
    public class FieldValidationService
    {

        public object? ProcessFieldValidation(string? value, FieldMapping field)
        {

            value = string.IsNullOrWhiteSpace(value) || value.Trim().ToLowerInvariant() == "null" ? null : value;

            if (field.ConversionRules != null && field.ConversionRules.TryGetValue(value ?? string.Empty, out var convertedValue))
            {
                value = convertedValue?.ToString();
            }

            object? result = value ?? field.Validations.DefaultValue;

            ValidateRequiredField(result, field);

            if (result == null) return null;

            return field.Validations.Type.ToLowerInvariant() switch
            {
                "string" => ValidateString(result, field),
                "int" => ValidateNumeric<int>(result, field, int.TryParse),
                "long" => ValidateNumeric<long>(result, field, long.TryParse),
                "double" => ValidateNumeric<double>(result, field, double.TryParse),
                "bool" => ValidateBoolean(result, field),
                _ => ValidateString(result, field),
            };
        }

        private void ValidateRequiredField(object? result, FieldMapping field)
        {
            if (field.Validations.Required && result == null)
            {
                HandleValidationError($"The CSV field '{field.CSVField}' in the file '{field.CSVFile}' is missing a value. " +
                    $"This may lead to incorrect or unexpected values in the target field '{field.JSONField}'.",
                    field.Validations.ValidationsNeedToPass,
                    $"{field.CSVFile} - {field.CSVField}: Missing Required Field");
            }
        }

        private string ValidateString(object result, FieldMapping field)
        {
            var stringValue = result.ToString()!;
            ValidateMinMax(stringValue.Length, field, field.Validations.Min, field.Validations.Max);
            return stringValue;
        }

        private T ValidateNumeric<T>(object result, FieldMapping field, TryParseHandler<T> tryParse)
        {
            if (!tryParse(result.ToString(), out var numericValue))
            {
                HandleValidationError($"The CSV field '{field.CSVField}' in the file '{field.CSVFile}' is expecting a value of type {typeof(T).Name}, but it received '{result}'. " +
                    $"This could lead to unexpected behavior or incorrect values in the target field '{field.JSONField}'.",
                    field.Validations.ValidationsNeedToPass,
                    $"{field.CSVFile} - {field.CSVField}: {typeof(T).Name} Type Validation");
            }

            ValidateMinMax(Convert.ToDouble(numericValue), field, field.Validations.Min, field.Validations.Max);
            return numericValue!;
        }

        private bool ValidateBoolean(object result, FieldMapping field)
        {
            if (!bool.TryParse(result.ToString(), out var boolValue))
            {
                HandleValidationError($"The JSON field '{field.JSONField}' is expecting a boolean value, but the received value was '{result}'. " +
                    $"This may result in unexpected or incorrect behavior in the target field.",
                    field.Validations.ValidationsNeedToPass,
                    $"{field.CSVFile} - {field.CSVField}: Boolean Type Validation");
            }

            return boolValue;
        }

        private void ValidateMinMax<T>(T value, FieldMapping field, T? min, T? max) where T : struct, IComparable<T>
        {
            if (min.HasValue && value.CompareTo(min.Value) < 0)
            {
                HandleValidationError($"The JSON field '{field.JSONField}' has a minimum value of {min}, but the provided value '{value}' is smaller than the required minimum. " +
                     $"This could cause issues with data processing or unexpected results in the target field.",
                     field.Validations.ValidationsNeedToPass,
                     $"{field.CSVFile} - {field.CSVField}: Min Range Validation");
            }

            if (max.HasValue && value.CompareTo(max.Value) > 0)
            {
                HandleValidationError(
                    $"The JSON field '{field.JSONField}' has a maximum value of {max}, but the provided value '{value}' exceeds the maximum allowed. " +
                    $"This could lead to incorrect or unintended results in the target field.",
                    field.Validations.ValidationsNeedToPass,
                    $"{field.CSVFile} - {field.CSVField}: Max Range Validation");
            }
        }

        private void HandleValidationError(string message, bool validationsNeedToPass, string validationType)
        {
            if (validationsNeedToPass)
            {
                LogPublisher.PublishLogMessage($"Error: {validationType}", message);
            }
            else
            {
               LogPublisher.PublishLogMessage($"Warning: {validationType}", message);
            }
        }

        private delegate bool TryParseHandler<T>(string? input, out T result);
    }
}
