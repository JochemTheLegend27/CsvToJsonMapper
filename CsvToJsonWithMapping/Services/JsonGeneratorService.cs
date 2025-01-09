using CsvToJsonWithMapping.Models;
using CsvToJsonWithMapping.Enums;
using CsvToJsonWithMapping.Services;

namespace CsvToJsonWithMapping.Services
{
    public class JsonGeneratorService
    {
        private int _totalFields = 0;
        private int _processedFields = 0;

        private readonly FieldValidationService _fieldValidationService;

        public JsonGeneratorService(FieldValidationService fieldValidationService)
        {
            _fieldValidationService = fieldValidationService;
        }

        public List<Dictionary<string, object?>> GenerateJsonFromMappings(Mapping mapping, List<Relation> relations, Dictionary<string, IEnumerable<IDictionary<string, string?>>> csvData, Dictionary<string, IEnumerable<IDictionary<string, object>>> joinedData)
        {
            _processedFields = 0;
            _totalFields = mapping.Fields.Count();
            mapping.NestedFields.ForEach(x =>
            {
                _totalFields += x.Fields.Count();
                _totalFields += x.NestedFields.Count();
            });

            var resultJson = new List<Dictionary<string, object?>>();

            var count = 0;
            var jsonObject = new Dictionary<string, object?>();

            ProcessFieldMappings(mapping.Fields, csvData, count, jsonObject);
            ProcessNestedFieldMappings(mapping.NestedFields, relations, csvData, joinedData, count, jsonObject);

            resultJson.Add(jsonObject);

            return resultJson;
        }


        private void ProcessFieldMappings(
            List<FieldMapping> fields,
            Dictionary<string, IEnumerable<IDictionary<string, string?>>> csvData,
            int count,
            Dictionary<string, object?> jsonObject)
        {
            foreach (var field in fields)
            {
                ProgressField();

                if (field.JSONField == null)
                {
                    throw new Exception($"JSON field missing in mapping for '{field.CSVField}'.");
                }

                if (field.CSVFile == null || field.CSVField == null)
                {
                    LogPublisher.PublishLogMessage("Warning: CSVData - Missing Field",
                        $"Warning: CSV file or field missing in mapping for '{field.JSONField}'.");
                    jsonObject[field.JSONField] = field.Validations?.DefaultValue;
                    continue;
                }

                if (!csvData.ContainsKey(field.CSVFile))
                {
                    throw new Exception($"CSV file '{field.CSVFile}' is missing from the supplied data.");
                }

                // Find the record at the specified count
                var currentCsvData = csvData[field.CSVFile];
                var record = currentCsvData.Skip(count).FirstOrDefault();

                if (record == null)
                {
                    throw new Exception($"Record at index {count} not found in CSV file '{field.CSVFile}'.");
                }

                if (!record.TryGetValue(field.CSVField, out var value))
                {
                    throw new Exception($"Field '{field.CSVField}' not found in record at index {count} in file '{field.CSVFile}'.");
                }

                // Process the field validation
                var validatedValue = _fieldValidationService.ProcessFieldValidation(value, field);
                jsonObject[field.JSONField] = validatedValue;
            }
        }


        private void ProcessNestedFieldMappings(List<NestedMapping> nestedMappings, List<Relation> relations, Dictionary<string, IEnumerable<IDictionary<string, string?>>> csvData, Dictionary<string, IEnumerable<IDictionary<string, object>>> joinedData, int count, Dictionary<string, object?> jsonObject)
        {
            foreach (var nestedField in nestedMappings)
            {

                if (Enum.Parse<NestedType>(nestedField.JSONNestedType) == NestedType.Object)
                {
                    ProcessObjectTypeFields(nestedField, csvData, count, jsonObject);
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

        // Method to process Object type fields
        private void ProcessObjectTypeFields(
            NestedMapping nestedField,
            Dictionary<string, IEnumerable<IDictionary<string, string?>>> csvData,
            int count,
            Dictionary<string, object?> jsonObject)
        {
            var jsonObjectNested = new Dictionary<string, object?>();

            foreach (var field in nestedField.Fields)
            {
                ProgressField();

                if (field.JSONField == null)
                {
                    throw new Exception($"JSON field missing in mapping for '{field.CSVField}'.");
                }

                if (field.CSVFile == null || field.CSVField == null)
                {
                    jsonObjectNested[field.JSONField] = field.Validations?.DefaultValue;
                }
                else
                {
                    if (!csvData.ContainsKey(field.CSVFile))
                    {
                        throw new Exception($"CSV file '{field.CSVFile}' is missing from the supplied data.");
                    }

                    var currentCsvData = csvData[field.CSVFile];
                    var record = currentCsvData.Skip(count).FirstOrDefault();

                    if (record == null)
                    {
                        throw new Exception($"Record at index {count} not found in CSV file '{field.CSVFile}'.");
                    }

                    if (!record.TryGetValue(field.CSVField, out var value))
                    {
                        throw new Exception($"Field '{field.CSVField}' not found in record at index {count} in file '{field.CSVFile}'.");
                    }

                    // Validate and add the value
                    var validatedValue = _fieldValidationService.ProcessFieldValidation(value, field);
                    jsonObjectNested[field.JSONField] = validatedValue;
                }
            }

            AddEmptyNestedFields(nestedField, jsonObjectNested);

            jsonObject[nestedField.JSONNestedFieldName] = jsonObjectNested;
        }


        // Method to process Array type fields
        private void ProcessArrayTypeFields(NestedMapping nestedField, List<Relation> relations, Dictionary<string, IEnumerable<IDictionary<string, string?>>> csvData, Dictionary<string, IEnumerable<IDictionary<string, object>>> joinedData, Dictionary<string, object?> jsonObject)
        {
            // pkToJsonMapping is a Dictionary where:
            // - The key of the first Dictionary is a Dictionary<string, string>,
            //   where the key represents a filename of the PK (Primary Key)
            //   and the value contains the value of that PK.
            // - The value of the first Dictionary is another Dictionary<string, object>,
            //   containing the corresponding created JSON object.
            var pkToJsonMapping = new Dictionary<Dictionary<string, string>, Dictionary<string, object>>();
            var jsonFieldsWithoutCsv = new List<FieldMapping>();

            foreach (var field in nestedField.Fields)
            {
                ProgressField();

                if (field.JSONField == null)
                {
                    throw new Exception($"JSON field missing in mapping for '{field.CSVField}'.");
                }

                if (field.CSVFile == null || field.CSVField == null)
                {
                    jsonFieldsWithoutCsv.Add(field);
                }
                else
                {
                    ProcessFieldWithRelations(field, relations, csvData, joinedData, pkToJsonMapping);
                }
            }

            AddEmptyNestedFields(nestedField, pkToJsonMapping);
            AddFieldsWithoutCsv(jsonFieldsWithoutCsv, pkToJsonMapping);
            jsonObject[nestedField.JSONNestedFieldName] = pkToJsonMapping.Values.ToList();
        }

        // Method to process a field with relations
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

        // Method to process a field with foreign key relations
        private void ProcessFkRelations(FieldMapping field, List<Relation> fileFkRelations, Dictionary<string, IEnumerable<IDictionary<string, string?>>> csvData, Dictionary<string, IEnumerable<IDictionary<string, object>>> joinedData, Dictionary<Dictionary<string, string>, Dictionary<string, object?>> pkToJsonMapping)
        {
            Relation? correctRelation = null;
            IEnumerable<IDictionary<string, object>>? correctJoined = null;

            // Find the right relationship and joined data
            foreach (var fileRelation in fileFkRelations)
            {
                var joinesForThisFile = joinedData.Where(x => x.Key == fileRelation.PrimaryKey.CSVFileName).ToList();

                // Multiple joins: find the correct one based on the presence of the file in the join
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
                throw new Exception($"No joined data found for '{field.CSVFile}'.\nBut was expected");
            }

            // Process the joined data
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

                var fileData = (record[field.CSVFile] as IEnumerable<IDictionary<string, string>>)?.FirstOrDefault();

                if (fileData == null)
                {
                    LogPublisher.PublishLogMessage($"Warning: CSVData - {field.CSVFile} Missing File Data",
                         $"Warning: No data found for file '{field.CSVFile}' in the current record for primary key '{correctRelation.PrimaryKey.CSVField}' with value '{primaryKeyValue}'. Ensure the file contains valid data for the expected field '{field.CSVField}' in the record. This could indicate missing data in the CSV.");
                    continue;
                }

                if (!fileData.ContainsKey(field.CSVField))
                {
                    LogPublisher.PublishLogMessage("Warning: CSVData - Missing Field",
                        $"Warning: The field '{field.CSVField}' was not found in the data for CSV file '{field.CSVFile}'. Verify that the field exists and is correctly named.");
                    continue;
                }


                // Check if a JSON object already exists for this key
                var existingKey = pkToJsonMapping.Keys.FirstOrDefault(pkDict => pkDict.ContainsValue(primaryKeyValue) && pkDict.ContainsKey(correctRelation.PrimaryKey.CSVFileName));
                var value = _fieldValidationService.ProcessFieldValidation(fileData[field.CSVField], field);

                if (existingKey != null)
                {
                    var existingJson = pkToJsonMapping[existingKey];

                    if (existingJson.ContainsKey(field.JSONField))
                    {
                        existingJson[field.JSONField] = value;
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

                    pkToJsonMapping[new Dictionary<string, string> { { correctRelation.PrimaryKey.CSVFileName, primaryKeyValue } }] = jsonObjectNested;
                }
            }
        }

        // Method to process a field with primary key relations
        private void ProcessPkRelations(FieldMapping field, List<Relation> filePkRelations, Dictionary<string, IEnumerable<IDictionary<string, string?>>> csvData, Dictionary<string, IEnumerable<IDictionary<string, object>>> joinedData, Dictionary<Dictionary<string, string>, Dictionary<string, object?>> pkToJsonMapping)
        {
            Relation? correctRelation = null;
            IEnumerable<IDictionary<string, object>>? correctJoined = null;

            // Find the right relationship and joined data
            foreach (var fileRelation in filePkRelations)
            {
                // Find the correct joined data based on the Foreign Key
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
                throw new Exception($"No joined data found for '{field.CSVFile}' for the PK relationship.");
            }

            // Process the joined data
            foreach (var fileData in correctJoined)
            {
                // Find the value of the primary key in the joined data
                string? primaryKeyValue = null;
                if (fileData.TryGetValue(correctRelation.PrimaryKey.CSVField, out var pKvalue))
                {
                    primaryKeyValue = pKvalue.ToString();
                }

                if (primaryKeyValue == null)
                {
                    throw new Exception($"No primary key value found for '{correctRelation.PrimaryKey.CSVField}' in file '{correctRelation.PrimaryKey.CSVFileName}'.");
                }

                if (!fileData.ContainsKey(field.CSVField))
                {
                    throw new Exception($"No data found for ' {field.CSVField} ' in joined record.");
                }

                // Check if a JSON object already exists for this primary key
                var existingKey = pkToJsonMapping.Keys.FirstOrDefault(pkDict => pkDict.ContainsKey(correctRelation.PrimaryKey.CSVFileName) && pkDict.ContainsValue(primaryKeyValue));
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

                    pkToJsonMapping[new Dictionary<string, string> { { correctRelation.PrimaryKey.CSVFileName, primaryKeyValue } }] = jsonObjectNested;
                }
            }
        }

        // Method to process a field without relations
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
                    throw new Exception($"CSV field '{field.CSVField}' not found in {field.CSVFile}.");
                }
            }
        }

        // Method to add empty nested fields
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

        // Overload for pkToJsonMapping
        private void AddEmptyNestedFields(NestedMapping nestedField, Dictionary<Dictionary<string, string>, Dictionary<string, object>> pkToJsonMapping)
        {
            if (nestedField.NestedFields.Count > 0)
            {
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

        // Method to add fields without CSV
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