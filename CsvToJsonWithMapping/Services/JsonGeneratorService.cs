using CsvToJsonWithMapping.Models;
using CsvToJsonWithMapping.Enums;
using System.Text.Json;
using System.Diagnostics;
using System.Text;
using System.Security.Cryptography;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.ComponentModel.Design;

namespace CsvToJsonWithMapping.Services
{
    public class JsonGeneratorService
    {
        private int _totalFields = 0;
        private int _processedFields = 0;

        private int _currentRecord = 0;
        private string _currentRecordValue = string.Empty;
        private string? _recordFieldName = null;
        private string? _recordFileName = null;

        private readonly FieldValidationService _fieldValidationService;

        public JsonGeneratorService(FieldValidationService fieldValidationService)
        {
            _fieldValidationService = fieldValidationService;
        }

        public List<Dictionary<string, object?>> GenerateJsonFromMappings(Mapping mapping, List<Relation> relations, Dictionary<string, IEnumerable<IDictionary<string, string?>>> csvData, Dictionary<string, IEnumerable<IDictionary<string, object>>> joinedData)
        {
            _processedFields = 0;

            var resultJson = new List<Dictionary<string, object?>>();

            var (count, usedCsvFile, usedCsvField) = GetRecordCount(mapping, csvData, relations);

            _totalFields = mapping.Fields.Count();
            mapping.NestedFields.ForEach(x =>
            {
                _totalFields += x.Fields.Count();
                _totalFields += x.NestedFields.Count();
            });
            _totalFields *= count;

            for (var i = 0; i < Math.Max(count, 1); i++)
            {
                _currentRecord = i;
                _recordFieldName = usedCsvField;
                _recordFileName = usedCsvFile;

                _currentRecordValue = !string.IsNullOrEmpty(_recordFileName) && !string.IsNullOrEmpty(_recordFieldName) && csvData.ContainsKey(_recordFileName)
                     ? csvData[_recordFileName]
                         .ElementAtOrDefault(_currentRecord)?[_recordFieldName] ?? string.Empty
                     : string.Empty;

                var jsonObject = new Dictionary<string, object?>();

                ProcessFieldMappings(mapping.Fields, csvData, jsonObject);
                ProcessNestedFieldMappings(mapping.NestedFields, relations, csvData, joinedData, jsonObject);

                resultJson.Add(jsonObject);
            }

            return resultJson;
        }

        internal (int Count, string? CsvFile, string? CsvField) GetRecordCount(Mapping mapping, Dictionary<string, IEnumerable<IDictionary<string, string?>>> csvData, List<Relation> relations)
        {
            var fieldsWithFiles = mapping.Fields.Select(field => new { field.CSVFile, field.CSVField }).ToList();

            if (!fieldsWithFiles.Any())
            {
                return (0, null, null);
            }

            foreach (var field in fieldsWithFiles)
            {
                var matchingRelation = relations.FirstOrDefault(rel =>
                    rel.PrimaryKey.CSVFileName == field.CSVFile && rel.PrimaryKey.CSVField == field.CSVField);

                if (matchingRelation != null)
                {
                    var primaryKeyField = matchingRelation.PrimaryKey.CSVField;
                    var csvFileName = matchingRelation.PrimaryKey.CSVFileName;

                    if (csvData.ContainsKey(csvFileName))
                    {
                        var count = csvData[csvFileName]
                            .Select(record => record.ContainsKey(primaryKeyField) ? record[primaryKeyField] : null)
                            .Distinct()
                            .Count();

                        return (count, csvFileName, primaryKeyField);
                    }
                }
            }

            var primaryCsvFile = fieldsWithFiles.FirstOrDefault()?.CSVFile;
            if (primaryCsvFile == null)
            {
                return (0, null, null);
            }
            var fallbackCount = csvData[primaryCsvFile].Count();
            return (fallbackCount, primaryCsvFile, null);
        }


        private void ProcessFieldMappings(List<FieldMapping> fields, Dictionary<string, IEnumerable<IDictionary<string, string?>>> csvData, Dictionary<string, object?> jsonObject)
        {
            foreach (var field in fields)
            {
                ProgressField();

                if (field.JSONField == null)
                {
                    throw new Exception($"JSON field missing in mapping for '{field.CSVField}'.");
                }

                if (string.IsNullOrEmpty(field.CSVFile) || string.IsNullOrEmpty(field.CSVField))
                {
                    LogPublisher.PublishLogMessage("Warning: CSVData - Missing Field",
                        $"Warning: CSV file or field missing in mapping for '{field.JSONField}'.");
                    jsonObject[field.JSONField] = _fieldValidationService.ProcessFieldValidation(null, field);
                    continue;
                }

                if (!csvData.ContainsKey(field.CSVFile))
                {
                    throw new Exception($"CSV file '{field.CSVFile}' is missing from the supplied data.");
                }

                var currentCsvData = csvData[field.CSVFile];
                var record = currentCsvData.Skip(_currentRecord).FirstOrDefault();

                if (record == null)
                {
                    throw new Exception($"Record at index {_currentRecord} not found in CSV file '{field.CSVFile}'.");
                }

                if (!record.TryGetValue(field.CSVField, out var value))
                {
                    throw new Exception($"Field '{field.CSVField}' not found in record at index {_currentRecord} in file '{field.CSVFile}'.");
                }

                var validatedValue = _fieldValidationService.ProcessFieldValidation(value, field);
                jsonObject[field.JSONField] = validatedValue;
            }
        }

        private void ProcessNestedFieldMappings(List<NestedMapping> nestedMappings, List<Relation> relations, Dictionary<string, IEnumerable<IDictionary<string, string?>>> csvData, Dictionary<string, IEnumerable<IDictionary<string, object>>> joinedData, Dictionary<string, object?> jsonObject)
        {
            foreach (var nestedField in nestedMappings)
            {

                if (Enum.Parse<NestedType>(nestedField.JSONNestedType) == NestedType.Object)
                {
                    ProcessObjectTypeFields(nestedField, csvData, jsonObject);
                }
                else if (Enum.Parse<NestedType>(nestedField.JSONNestedType) == NestedType.Array)
                {
                    ProcessArrayTypeFields(nestedField, relations, csvData, joinedData, jsonObject);
                }
                else
                {
                    throw new Exception($"Unknown type {nestedField.JSONNestedType} for {nestedField.JSONNestedFieldName}");
                }
            }
        }

        private void ProcessObjectTypeFields(NestedMapping nestedField, Dictionary<string, IEnumerable<IDictionary<string, string?>>> csvData, Dictionary<string, object?> jsonObject)
        {
            var jsonObjectNested = new Dictionary<string, object?>();

            foreach (var field in nestedField.Fields)
            {
                if (field.JSONField == null)
                {
                    throw new Exception($"JSON field missing in mapping for '{field.CSVField}'.");
                }

                if (string.IsNullOrEmpty(field.CSVFile) || string.IsNullOrEmpty(field.CSVField))
                {
                    jsonObjectNested[field.JSONField] = _fieldValidationService.ProcessFieldValidation(null, field);
                }
                else
                {
                    if (!csvData.ContainsKey(field.CSVFile))
                    {
                        throw new Exception($"CSV file '{field.CSVFile}' is missing from the supplied data.");
                    }

                    var currentCsvData = csvData[field.CSVFile];
                    var record = currentCsvData.Skip(_currentRecord).FirstOrDefault();

                    if (record == null)
                    {
                        throw new Exception($"Record at index {_currentRecord} not found in CSV file '{field.CSVFile}'.");
                    }

                    if (!record.TryGetValue(field.CSVField, out var value))
                    {
                        throw new Exception($"Field '{field.CSVField}' not found in record at index {_currentRecord} in file '{field.CSVFile}'.");
                    }

                    var validatedValue = _fieldValidationService.ProcessFieldValidation(value, field);
                    jsonObjectNested[field.JSONField] = validatedValue;
                }
                ProgressField();
            }

            AddEmptyNestedFields(nestedField, jsonObjectNested);

            jsonObject[nestedField.JSONNestedFieldName] = jsonObjectNested;
        }

        private void ProcessArrayTypeFields(NestedMapping nestedField, List<Relation> relations, Dictionary<string, IEnumerable<IDictionary<string, string?>>> csvData, Dictionary<string, IEnumerable<IDictionary<string, object>>> joinedData, Dictionary<string, object?> jsonObject)
        {
            /* pkToJsonMapping is a Dictionary where:
                - The key of the first Dictionary is a Dictionary<string, string>,
                where the key represents a filename of the PK (Primary Key)
                and the value contains the value of that PK.
                - The value of the first Dictionary is another Dictionary<string, object>,
                containing the corresponding created JSON object. */
            var pkToJsonMapping = new Dictionary<Dictionary<string, string>, Dictionary<string, object>>();
            var jsonFieldsWithoutCsv = new List<FieldMapping>();

            foreach (var field in nestedField.Fields)
            {
                if (field.JSONField == null)
                {
                    throw new Exception($"JSON field missing in mapping for '{field.CSVField}'.");
                }

                if (string.IsNullOrEmpty(field.CSVFile) || string.IsNullOrEmpty(field.CSVField))
                {
                    jsonFieldsWithoutCsv.Add(field);
                }
                else
                {
                    ProcessFieldWithRelations(field, relations, csvData, joinedData, pkToJsonMapping);
                }
                ProgressField();
            }

            AddEmptyNestedFields(nestedField, pkToJsonMapping);
            AddFieldsWithoutCsv(jsonFieldsWithoutCsv, pkToJsonMapping);
            jsonObject[nestedField.JSONNestedFieldName] = pkToJsonMapping.Values.ToList();
        }

        private void ProcessFieldWithRelations(FieldMapping field, List<Relation> relations, Dictionary<string, IEnumerable<IDictionary<string, string?>>> csvData, Dictionary<string, IEnumerable<IDictionary<string, object>>> joinedData, Dictionary<Dictionary<string, string>, Dictionary<string, object>> pkToJsonMapping)
        {
            var fileFkRelations = relations.FindAll(x => x.ForeignKey.CSVFileName == field.CSVFile).ToList();
            var filePkRelations = relations.FindAll(x => x.PrimaryKey.CSVFileName == field.CSVFile).ToList();

            if (fileFkRelations.Any())
            {
                ProcessFkRelations(field, fileFkRelations, csvData, joinedData, pkToJsonMapping);
            }
            else if (filePkRelations.Any())
            {
                ProcessPkRelations(field, filePkRelations, csvData, joinedData, pkToJsonMapping);
            }
            else
            {
                ProcessFieldWithoutRelations(field, csvData, pkToJsonMapping);
            }
        }

        private void ProcessFkRelations(FieldMapping field, List<Relation> fileFkRelations, Dictionary<string, IEnumerable<IDictionary<string, string?>>> csvData, Dictionary<string, IEnumerable<IDictionary<string, object>>> joinedData, Dictionary<Dictionary<string, string>, Dictionary<string, object?>> pkToJsonMapping)
        {
            Relation? correctRelation = null;
            IEnumerable<IDictionary<string, object>>? correctJoined = null;

            foreach (var fileRelation in fileFkRelations)
            {
                var joinesForThisFile = joinedData.Where(x => x.Key == fileRelation.PrimaryKey.CSVFileName).ToList();

                foreach (var joinForThisFile in joinesForThisFile)
                {
                    var isCorrectJoin = joinForThisFile.Value.Any(x => x.ContainsKey(field.CSVFile));
                    if (isCorrectJoin)
                    {
                        correctJoined = joinForThisFile.Value.Cast<IDictionary<string, object>>().ToList();
                        correctRelation = fileRelation;
                        break;
                    }
                }
                if (correctJoined != null)
                {
                    break;
                }
            }

            if (correctJoined == null)
            {
                LogPublisher.PublishLogMessage("Error: CSVData - Missing joined data", $"No joined data found for '{field.CSVFile}'. Ensure that the CSV file contains all the required columns for the join operation.");
            }
            else
            {
                foreach (var record in correctJoined)
                {
                    var primaryKeyValue = record[correctRelation.PrimaryKey.CSVField]?.ToString();
                    if (primaryKeyValue == null)
                    {
                        LogPublisher.PublishLogMessage("Warning: CSVData - Missing Primary Key",
                            $"Warning: The primary key value for '{correctRelation.PrimaryKey.CSVField}' is missing in the current record from the file '{field.CSVFile}'.");
                        continue; // Skip this record to avoid further processing issues
                    }

                    if (record.ContainsKey(field.CSVFile) == false)
                    {
                        LogPublisher.PublishLogMessage($"Warning: CSVData - {field.CSVFile} Missing File Data",
                            $"Warning: No data found for file '{field.CSVFile}' in the current record for primary key '{correctRelation.PrimaryKey.CSVField}' with value '{primaryKeyValue}'. Ensure the file contains valid data for the expected field '{field.CSVField}' in the record. This could indicate missing data in the CSV.");
                        continue;
                    }

                    var fileData = (record[field.CSVFile] as IEnumerable<IDictionary<string, string>>) ?? null;

                    if (fileData == null)
                    {
                        LogPublisher.PublishLogMessage($"Warning: CSVData - {field.CSVFile} Missing File Data",
                             $"Warning: No data found for file '{field.CSVFile}' in the current record for primary key '{correctRelation.PrimaryKey.CSVField}' with value '{primaryKeyValue}'. Ensure the file contains valid data for the expected field '{field.CSVField}' in the record. This could indicate missing data in the CSV.");
                        continue;
                    }

                    if (fileData != null && !fileData.Any(dict => dict.ContainsKey(field.CSVField)))
                    {
                        LogPublisher.PublishLogMessage("Warning: CSVData - Missing Field",
                            $"Warning: The field '{field.CSVField}' was not found in the data for CSV file '{field.CSVFile}'. Verify that the field exists and is correctly named.");
                        continue;
                    }

                    if (_recordFileName == correctRelation.PrimaryKey.CSVFileName && _currentRecordValue != primaryKeyValue)
                    {
                        continue;
                    }

                    var keyBase = $"{correctRelation.PrimaryKey.CSVFileName}";
                    var processedKeys = new HashSet<string>();

                    foreach (var data in fileData)
                    {
                        string key;
                        int index = 0;

                        // Generate a unique key by appending an index if necessary
                        do
                        {
                            key = index == 0 ? keyBase : $"{keyBase}_{index}";
                            index++;
                        } while (processedKeys.Contains(key));

                        processedKeys.Add(key);

                        var existingKey = pkToJsonMapping.Keys.FirstOrDefault(pkDict => pkDict.ContainsKey(key) && pkDict.ContainsValue(primaryKeyValue));

                        var value = _fieldValidationService.ProcessFieldValidation(data[field.CSVField], field);

                        if (existingKey != null)
                        {
                            var existingJson = pkToJsonMapping[existingKey];

                            if (existingJson.ContainsKey(field.JSONField))
                            {
                                existingJson[field.JSONField] = existingJson[field.JSONField] + " " + value;
                            }
                            else
                            {
                                existingJson.Add(field.JSONField, value);
                            }
                        }
                        else
                        {
                            var jsonObjectNested = new Dictionary<string, object?>();
                            jsonObjectNested[field.JSONField] = value;

                            pkToJsonMapping[new Dictionary<string, string> { { key, primaryKeyValue } }] = jsonObjectNested;
                        }
                    }
                }
            }
        }

        private void ProcessPkRelations(FieldMapping field, List<Relation> filePkRelations, Dictionary<string, IEnumerable<IDictionary<string, string?>>> csvData, Dictionary<string, IEnumerable<IDictionary<string, object>>> joinedData, Dictionary<Dictionary<string, string>, Dictionary<string, object?>> pkToJsonMapping)
        {
            Relation? correctRelation = null;
            IEnumerable<IDictionary<string, object>>? correctJoined = null;

            foreach (var fileRelation in filePkRelations)
            {
                var joinesForThisFile = joinedData.Where(x => x.Key == fileRelation.PrimaryKey.CSVFileName).ToList();

                foreach (var joinForThisFile in joinesForThisFile)
                {
                    var isCorrectJoin = joinForThisFile.Key == field.CSVFile;
                    if (isCorrectJoin)
                    {
                        correctJoined = joinForThisFile.Value.Cast<IDictionary<string, object>>().ToList();
                        correctRelation = fileRelation;
                        break;
                    }
                }
                if (correctJoined != null)
                {
                    break;
                }
            }

            if (correctJoined == null)
            {
                LogPublisher.PublishLogMessage("Error: CSVData - Missing joined data", $"No joined data found for '{field.CSVFile}'. Ensure that the CSV file contains all the required columns for the join operation.");
            }
            else
            {
                foreach (var fileData in correctJoined)
                {
                    string? primaryKeyValue = null;
                    if (fileData.TryGetValue(correctRelation.PrimaryKey.CSVField, out var pKvalue))
                    {
                        primaryKeyValue = pKvalue.ToString();
                    }

                    if (primaryKeyValue == null)
                    {
                        LogPublisher.PublishLogMessage("Warning: CSVData - Missing Primary Key",
                            $"Warning: The primary key value for '{correctRelation.PrimaryKey.CSVField}' is missing in the current record from the file '{field.CSVFile}'.");
                        continue;
                    }

                    if (!fileData.ContainsKey(field.CSVField))
                    {
                        LogPublisher.PublishLogMessage("Warning: CSVData - Missing Field",
                            $"Warning: The field '{field.CSVField}' was not found in the data for CSV file '{field.CSVFile}'. Verify that the field exists and is correctly named.");
                        continue;
                    }

                    if (_recordFileName == correctRelation.PrimaryKey.CSVFileName && _currentRecordValue != primaryKeyValue)
                    {
                        continue;
                    }

                    var key = $"{correctRelation.PrimaryKey.CSVFileName}";

                    var existingKey = pkToJsonMapping.Keys.FirstOrDefault(pkDict => pkDict.ContainsKey(key) && pkDict.ContainsValue(primaryKeyValue));
                    var value = _fieldValidationService.ProcessFieldValidation(fileData[field.CSVField].ToString(), field);

                    if (existingKey != null)
                    {
                        var existingJson = pkToJsonMapping[existingKey];

                        if (existingJson.ContainsKey(field.JSONField))
                        {
                            existingJson[field.JSONField] = existingJson[field.JSONField] + " " + value;
                        }
                        else
                        {
                            existingJson.Add(field.JSONField, value);
                        }
                    }
                    else
                    {
                        var jsonObjectNested = new Dictionary<string, object?>();
                        jsonObjectNested[field.JSONField] = value;

                        pkToJsonMapping[new Dictionary<string, string> { { key, primaryKeyValue } }] = jsonObjectNested;
                    }
                }
            }
        }

        private void ProcessFieldWithoutRelations(FieldMapping field, Dictionary<string, IEnumerable<IDictionary<string, string?>>> csvData, Dictionary<Dictionary<string, string>, Dictionary<string, object?>> pkToJsonMapping)
        {

            var currentCsvData = csvData[field.CSVFile];

            var rowNumber = 0;
            foreach (var record in currentCsvData)
            {
                if (record.ContainsKey(field.CSVField))
                {
                    var value = _fieldValidationService.ProcessFieldValidation(record[field.CSVField], field);

                    var primaryKeyValue = rowNumber.ToString();

                    var existingKey = pkToJsonMapping.Keys.FirstOrDefault(pkDict => pkDict.ContainsKey(field.CSVFile) && pkDict.ContainsValue(primaryKeyValue));
                    if (existingKey != null)
                    {
                        var existingJson = pkToJsonMapping[existingKey];

                        if (existingJson.ContainsKey(field.JSONField))
                        {
                            existingJson[field.JSONField] = existingJson[field.JSONField] + " " + value;
                        }
                        else
                        {
                            existingJson.Add(field.JSONField, value);
                        }
                    }
                    else
                    {
                        var jsonObjectNested = new Dictionary<string, object?>();
                        jsonObjectNested[field.JSONField] = value;

                        pkToJsonMapping[new Dictionary<string, string> { { field.CSVFile, primaryKeyValue } }] = jsonObjectNested;
                    }
                    rowNumber++;
                }
                else
                {
                    LogPublisher.PublishLogMessage("Error: CSVData - Missing column", $"CSV column '{field.CSVField}' not found in '{field.CSVFile}'.");
                }
            }
        }

        private void AddEmptyNestedFields(NestedMapping nestedField, Dictionary<string, object?> jsonObjectNested)
        {
            if (nestedField.NestedFields.Count > 0)
            {
                LogPublisher.PublishLogMessage("Warning: Nested Fields - Unsupported",
                       $"Warning: Nested fields in nested fields are not supported. Nested field '{nestedField.JSONNestedFieldName}' will be empty.");
                foreach (var nested in nestedField.NestedFields)
                {
                    ProgressField();
                    jsonObjectNested[nested.JSONNestedFieldName] = new List<Dictionary<string, object?>>();
                }
            }
        }

        private void AddEmptyNestedFields(NestedMapping nestedField, Dictionary<Dictionary<string, string>, Dictionary<string, object>> pkToJsonMapping)
        {
            if (nestedField.NestedFields.Count > 0)
            {
                LogPublisher.PublishLogMessage("Warning: Nested Fields - Unsupported",
                       $"Warning: Nested fields in nested fields are not supported. Nested field '{nestedField.JSONNestedFieldName}' will be empty.");
                foreach (var nested in nestedField.NestedFields)
                {
                    ProgressField();
                    foreach (var map in pkToJsonMapping)
                    {
                        map.Value.Add(nested.JSONNestedFieldName, new List<Dictionary<string, object>>());
                    }
                }
            }
        }

        private void AddFieldsWithoutCsv(List<FieldMapping> jsonFieldsWithoutCsv, Dictionary<Dictionary<string, string>, Dictionary<string, object>> pkToJsonMapping)
        {
            foreach (var fieldWithoutCsv in jsonFieldsWithoutCsv)
            {
                foreach (var map in pkToJsonMapping)
                {
                    map.Value.Add(fieldWithoutCsv.JSONField, string.Empty);
                }
            }
        }

        private void ProgressField()
        {
            _processedFields++;
            LogPublisher.PublishProgress(_processedFields, _totalFields);
        }
    }
}