using CsvToJsonWithMapping.Models;

namespace CsvToJsonWithMapping.Services
{
    public static class FieldValidationService
    {
        private static readonly Dictionary<string, List<string>> _log = new();

        public static object? ProcessFieldValidation(string? value, FieldMapping field)
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
                "double" => ValidateNumeric<double>(result, field, double.TryParse),
                "bool" => ValidateBoolean(result, field),
                _ => throw new Exception($"Field type '{field.Validations.Type}' is not supported.")
            };
        }

        private static void ValidateRequiredField(object? result, FieldMapping field)
        {
            if (field.Validations.Required && result == null)
            {
                HandleValidationError($"Field '{field.JSONField}' is required but '{field.CSVField}' was null.", field.Validations.ValidationsNeedToPass);
            }
        }

        private static string ValidateString(object result, FieldMapping field)
        {
            var stringValue = result.ToString()!;
            ValidateMinMax(stringValue.Length, field, field.Validations.Min, field.Validations.Max);
            return stringValue;
        }

        private static T ValidateNumeric<T>(object result, FieldMapping field, TryParseHandler<T> tryParse)
        {
            if (!tryParse(result.ToString(), out var numericValue))
            {
                HandleValidationError($"Field '{field.JSONField}' expects a {typeof(T).Name} but got '{result}'.", field.Validations.ValidationsNeedToPass);
            }

            ValidateMinMax(Convert.ToDouble(numericValue), field, field.Validations.Min, field.Validations.Max);
            return numericValue!;
        }

        private static bool ValidateBoolean(object result, FieldMapping field)
        {
            if (!bool.TryParse(result.ToString(), out var boolValue))
            {
                HandleValidationError($"Field '{field.JSONField}' expects a boolean but got '{result}'.", field.Validations.ValidationsNeedToPass);
            }

            return boolValue;
        }

        private static void ValidateMinMax<T>(T value, FieldMapping field, T? min, T? max) where T : struct, IComparable<T>
        {
            if (min.HasValue && value.CompareTo(min.Value) < 0)
            {
                HandleValidationError($"Field '{field.JSONField}' is too small (minimum: {min}).", field.Validations.ValidationsNeedToPass);
            }

            if (max.HasValue && value.CompareTo(max.Value) > 0)
            {
                HandleValidationError($"Field '{field.JSONField}' is too large (maximum: {max}).", field.Validations.ValidationsNeedToPass);
            }
        }

        private static void HandleValidationError(string message, bool validationsNeedToPass)
        {
            if (validationsNeedToPass)
            {
                LoggingService.LogError("Validation",message);
            }
            else
            {
                LoggingService.LogWarning("Validation", message);
            }
        }

        private delegate bool TryParseHandler<T>(string? input, out T result);
    }
}
