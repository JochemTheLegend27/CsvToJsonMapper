using CsvToJsonWithMapping.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvToJsonWithMapping.Services
{
    public static class FieldValidator
    {

        public static object? ProcessFieldValidation(string? value, FieldMapping field)
        {
            // Normalize input value
            value = string.IsNullOrWhiteSpace(value) ? null : value;

            //TODO: Conversion based on the given conversions in the mapping (field)



            // Apply default value if necessary
            object? result = value ?? field.Validations.DefaultValue;

            // Ensure required fields are not null
            if (field.Validations.Required && result == null)
            {
                throw new Exception($"Field '{field.JSONField}' is required but '{field.CSVField}' was null.");
            }

            // If result is null, skip type-specific validation
            if (result == null) return null;

            // Validate and convert based on field type
            return field.Validations.Type.ToLowerInvariant() switch
            {
                "string" => ValidateString(result, field),
                "int" => ValidateNumeric<int>(result, field, int.TryParse),
                "double" => ValidateNumeric<double>(result, field, double.TryParse),
                "bool" => ValidateBoolean(result, field),
                _ => throw new Exception($"Field type '{field.Validations.Type}' is not supported.")
            };
        }

        private static string ValidateString(object result, FieldMapping field)
        {
            var stringValue = result.ToString()!;
            int length = stringValue.Length;

            if (field.Validations.Min.HasValue && length < field.Validations.Min)
                throw new Exception($"Field '{field.JSONField}' is too short (minimum: {field.Validations.Min}).");

            if (field.Validations.Max.HasValue && length > field.Validations.Max)
                throw new Exception($"Field '{field.JSONField}' is too long (maximum: {field.Validations.Max}).");

            return stringValue;
        }

        private static T ValidateNumeric<T>(object result, FieldMapping field, TryParseHandler<T> tryParse)
        {
            if (!tryParse(result.ToString(), out var numericValue))
                throw new Exception($"Field '{field.JSONField}' expects a {typeof(T).Name} but got '{result}'.");

            var min = field.Validations.Min;
            var max = field.Validations.Max;

            if (min.HasValue && Convert.ToDouble(numericValue) < min.Value)
                throw new Exception($"Field '{field.JSONField}' is too small (minimum: {min}).");

            if (max.HasValue && Convert.ToDouble(numericValue) > max.Value)
                throw new Exception($"Field '{field.JSONField}' is too large (maximum: {max}).");

            return numericValue!;
        }

        private static bool ValidateBoolean(object result, FieldMapping field)
        {
            if (!bool.TryParse(result.ToString(), out var boolValue))
                throw new Exception($"Field '{field.JSONField}' expects a boolean but got '{result}'.");

            return boolValue;
        }

        private delegate bool TryParseHandler<T>(string? input, out T result);
    }
}
