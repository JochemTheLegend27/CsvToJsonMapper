using CsvToJsonWithMapping.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public static class JsonGenerator
{

    public static List<Dictionary<string, object>> GenerateJsonFromMappings(Mapping mapping, List<Relation> relations, Dictionary<string, List<Dictionary<string, string>>> csvData, Dictionary<string, List<Dictionary<string, object>>> joinedData, ILogger logger)
    {
        var resultJson = new List<Dictionary<string, object>>();

        logger.LogInformation("Start generating JSON objects based on the mapping.");

        var count = 0;
        var jsonObject = new Dictionary<string, object>();

        ProcessFieldMappings(mapping.Fields, csvData, count, jsonObject, logger);
        ProcessNestedFieldMappings(mapping.NestedFields, relations, csvData, joinedData, count, jsonObject, logger);

        resultJson.Add(jsonObject);

        logger.LogInformation("JSON objects generated: " + JsonSerializer.Serialize(resultJson, new JsonSerializerOptions { WriteIndented = true }));
        return resultJson;
    }


    private static void ProcessFieldMappings(List<FieldMapping> fields, Dictionary<string, List<Dictionary<string, string>>> csvData, int count, Dictionary<string, object> jsonObject, ILogger logger)
    {
        foreach (var fieldMapping in fields)
        {
            if (fieldMapping.JSONField == null)
            {
                throw new Exception($"JSON field missing in mapping for '{fieldMapping.CSVField}'.");
            }

            if (fieldMapping.CSVFile == null || fieldMapping.CSVField == null)
            {
                logger.LogWarning($"CSV file or CSV field is missing from the mapping for '{fieldMapping.JSONField}'.\nSo empty string added.");
                jsonObject[fieldMapping.JSONField] = string.Empty;
                continue;
            }

            if (!csvData.ContainsKey(fieldMapping.CSVFile))
            {
                throw new Exception($"CSV file '{fieldMapping.CSVFile}' is missing from the supplied data.");
            }

            var currentCsvData = csvData[fieldMapping.CSVFile];

            // normal fields are fields that are the top layer so they go based on the order of the csv data (count)
            jsonObject[fieldMapping.JSONField] = currentCsvData[count][fieldMapping.CSVField];
        }
    }

    private static void ProcessNestedFieldMappings(List<NestedMapping> nestedMappings, List<Relation> relations, Dictionary<string, List<Dictionary<string, string>>> csvData, Dictionary<string, List<Dictionary<string, object>>> joinedData, int count, Dictionary<string, object> jsonObject, ILogger logger)
    {
        foreach (var nestedField in nestedMappings)
        {
            logger.LogInformation($"Processing type {nestedField.JSONNestedType} fields for '{nestedField.JSONNestedFieldName}'.");

            if (Enum.Parse<NestedType>(nestedField.JSONNestedType) == NestedType.Object)
            {
                ProcessObjectTypeFields(nestedField, csvData, count, jsonObject, logger);
            }
            else if (Enum.Parse<NestedType>(nestedField.JSONNestedType) == NestedType.Array)
            {
                ProcessArrayTypeFields(nestedField, relations, csvData, joinedData, jsonObject, logger);
            }
            else
            {
                throw new Exception($"Unknown type {nestedField.JSONNestedType} for {nestedField.JSONNestedFieldName}");
            }
        }
    }

    // Method to process Object type fields
    private static void ProcessObjectTypeFields(NestedMapping nestedField, Dictionary<string, List<Dictionary<string, string>>> csvData, int count, Dictionary<string, object> jsonObject, ILogger logger)
    {
        var jsonObjectNested = new Dictionary<string, object>();

        nestedField.Fields.ForEach(field =>
        {
            if (field.JSONField == null)
            {
                throw new Exception($"JSON field missing in mapping for '{field.CSVField}'.");
            }

            if (field.CSVFile == null || field.CSVField == null)
            {
                logger.LogWarning($"CSV file or CSV field is missing from the mapping for '{field.JSONField}'. So empty string added.");
                jsonObjectNested[field.JSONField] = string.Empty;
            }
            else
            {
                if (!csvData.ContainsKey(field.CSVFile))
                {
                    throw new Exception($"CSV file '{field.CSVFile}' is missing from the supplied data.");
                }

                var currentCsvData = csvData[field.CSVFile];
                jsonObjectNested[field.JSONField] = currentCsvData[count][field.CSVField];
            }
        });

        AddEmptyNestedFields(nestedField, jsonObjectNested, logger);

        jsonObject[nestedField.JSONNestedFieldName] = jsonObjectNested;
    }

    // Method to process Array type fields
    private static void ProcessArrayTypeFields(NestedMapping nestedField, List<Relation> relations, Dictionary<string, List<Dictionary<string, string>>> csvData, Dictionary<string, List<Dictionary<string, object>>> joinedData, Dictionary<string, object> jsonObject, ILogger logger)
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
            ValidateField(field, logger);

            if (field.CSVFile == null || field.CSVField == null)
            {
                logger.LogWarning($"CSV file or CSV field is missing from the mapping for '{field.JSONField}'.");
                jsonFieldsWithoutCsv.Add(field);
            }
            else
            {
                ProcessFieldWithRelations(field, relations, csvData, joinedData, pkToJsonMapping, logger);
            }
        }

        AddEmptyNestedFields(nestedField, pkToJsonMapping, logger);
        AddFieldsWithoutCsv(jsonFieldsWithoutCsv, pkToJsonMapping);
        jsonObject[nestedField.JSONNestedFieldName] = pkToJsonMapping.Values.ToList();
    }

    // Method to validate a field
    private static void ValidateField(FieldMapping field, ILogger logger)
    {
        if (field.JSONField == null)
        {
            logger.LogError($"JSON field missing in mapping for '{field.CSVField}'.");
            throw new Exception($"JSON field missing in mapping for '{field.CSVField}'.");
        }
    }

    // Method to process a field with relations
    private static void ProcessFieldWithRelations(FieldMapping field, List<Relation> relations, Dictionary<string, List<Dictionary<string, string>>> csvData, Dictionary<string, List<Dictionary<string, object>>> joinedData, Dictionary<Dictionary<string, string>, Dictionary<string, object>> pkToJsonMapping, ILogger logger)
    {
        var fileFkRelations = relations.FindAll(x => x.ForeignKey.CSVFileName == field.CSVFile).ToList();
        var filePkRelations = relations.FindAll(x => x.PrimaryKey.CSVFileName == field.CSVFile).ToList();

        if (fileFkRelations.Any())
        {
            ProcessFkRelations(field, fileFkRelations, csvData, joinedData, pkToJsonMapping, logger);
        }
        else if (filePkRelations.Any())
        {
            ProcessPkRelations(field, filePkRelations, csvData, joinedData, pkToJsonMapping, logger);
        }
        else
        {
            ProcessFieldWithoutRelations(field, csvData, pkToJsonMapping, logger);
        }
    }

    // Method to process a field with foreign key relations
    private static void ProcessFkRelations(FieldMapping field, List<Relation> fileFkRelations, Dictionary<string, List<Dictionary<string, string>>> csvData, Dictionary<string, List<Dictionary<string, object>>> joinedData, Dictionary<Dictionary<string, string>, Dictionary<string, object>> pkToJsonMapping, ILogger logger)
    {
        logger.LogInformation($"FK Relations found for '{field.CSVField}' in file '{field.CSVFile}': {JsonSerializer.Serialize(fileFkRelations, new JsonSerializerOptions { WriteIndented = true })}");
        Relation? correctRelation = null;
        List<Dictionary<string, object>>? correctJoined = null;

        // Find the right relationship and joined data
        foreach (var fileRelation in fileFkRelations)
        {
            var joinesForThisFile = joinedData.Where(x => x.Key == fileRelation.PrimaryKey.CSVFileName).ToList();
            logger.LogInformation($"Joined data found for '{field.CSVFile}': {JsonSerializer.Serialize(joinesForThisFile, new JsonSerializerOptions { WriteIndented = true })}");

            // Multiple joins: find the correct one based on the presence of the file in the join
            foreach (var joinForThisFile in joinesForThisFile)
            {
                var isCorrectJoin = joinForThisFile.Value.Any(x => x.ContainsKey(field.CSVFile));
                if (isCorrectJoin)
                {

                    correctJoined = joinForThisFile.Value;
                    correctRelation = fileRelation;
                    logger.LogInformation($"Correct joined data found for '{field.CSVFile}'.\n{JsonSerializer.Serialize(correctJoined, new JsonSerializerOptions { WriteIndented = true })}");
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
            var primaryKeyValue = record[correctRelation.PrimaryKey.CSVField].ToString();
            if (primaryKeyValue == null)
            {
                throw new Exception($"No primary key value found for '{correctRelation.PrimaryKey.CSVField}' in file '{correctRelation.PrimaryKey.CSVFileName}'.");
            }

            var fileData = ((List<Dictionary<string, string>>)record[field.CSVFile]).FirstOrDefault();
            if (fileData == null)
            {
                throw new Exception($"No data found for '{field.CSVFile}' in joined record.");
            }
            if (!fileData.ContainsKey(field.CSVField))
            {
                throw new Exception($"No data found for '{field.CSVField}' in joined record.");
            }

            // Check if a JSON object already exists for this key
            var existingKey = pkToJsonMapping.Keys.FirstOrDefault(pkDict => pkDict.ContainsValue(primaryKeyValue) && pkDict.ContainsKey(correctRelation.PrimaryKey.CSVFileName));
            if (existingKey != null)
            {
                var existingJson = pkToJsonMapping[existingKey];

                if (existingJson.ContainsKey(field.JSONField))
                {
                    existingJson[field.JSONField] = fileData[field.CSVField];
                }
                else
                {
                    existingJson.Add(field.JSONField, fileData[field.CSVField]);
                }
            }
            else
            {
                var jsonObjectNested = new Dictionary<string, object>();
                jsonObjectNested[field.JSONField] = fileData[field.CSVField];

                pkToJsonMapping[new Dictionary<string, string> { { correctRelation.PrimaryKey.CSVFileName, primaryKeyValue } }] = jsonObjectNested;
            }
        }
    }

    // Method to process a field with primary key relations
    private static void ProcessPkRelations(FieldMapping field, List<Relation> filePkRelations, Dictionary<string, List<Dictionary<string, string>>> csvData, Dictionary<string, List<Dictionary<string, object>>> joinedData, Dictionary<Dictionary<string, string>, Dictionary<string, object>> pkToJsonMapping, ILogger logger)
    {
        logger.LogInformation($"PK Relations found for '{field.CSVField}' in file '{field.CSVFile}': {JsonSerializer.Serialize(filePkRelations, new JsonSerializerOptions { WriteIndented = true })}");
        Relation? correctRelation = null;
        List<Dictionary<string, object>>? correctJoined = null;

        // Find the right relationship and joined data
        foreach (var fileRelation in filePkRelations)
        {
            // Find the correct joined data based on the Foreign Key
            var joinesForThisFile = joinedData.Where(x => x.Key == fileRelation.PrimaryKey.CSVFileName).ToList();
            logger.LogInformation($"Joined data found for '{field.CSVFile}': {JsonSerializer.Serialize(joinesForThisFile, new JsonSerializerOptions { WriteIndented = true })}");

            foreach (var joinForThisFile in joinesForThisFile)
            {
                var isCorrectJoin = joinForThisFile.Key == field.CSVFile;
                if (isCorrectJoin)
                {
                    correctJoined = joinForThisFile.Value;
                    correctRelation = fileRelation;
                    logger.LogInformation($"Correct joined data found for '{field.CSVFile}'.\n{JsonSerializer.Serialize(correctJoined, new JsonSerializerOptions { WriteIndented = true })}");
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
            logger.LogError($"Geen gejoined data gevonden voor '{field.CSVFile}' voor de PK-relatie.");
            throw new Exception($"No joined data found for '{field.CSVFile}' for the PK relationship.");
        }

        // Process the joined data
        foreach (var fileData in correctJoined)
        {
            // Find the value of the primary key in the joined data
            var primaryKeyValue = fileData.GetValueOrDefault(correctRelation.PrimaryKey.CSVField).ToString();
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
            if (existingKey != null)
            {
                var existingJson = pkToJsonMapping[existingKey];

                existingJson.Add(field.JSONField, fileData[field.CSVField]);
            }
            else
            {
                var jsonObjectNested = new Dictionary<string, object>();
                jsonObjectNested[field.JSONField] = fileData[field.CSVField];

                pkToJsonMapping[new Dictionary<string, string> { { correctRelation.PrimaryKey.CSVFileName, primaryKeyValue } }] = jsonObjectNested;
            }
        }
    }

    // Method to process a field without relations
    private static void ProcessFieldWithoutRelations(FieldMapping field, Dictionary<string, List<Dictionary<string, string>>> csvData, Dictionary<Dictionary<string, string>, Dictionary<string, object>> pkToJsonMapping, ILogger logger)
    {
        logger.LogWarning($"No relation found for '{field.CSVField}' in file '{field.CSVFile}'.");

        var currentCsvData = csvData[field.CSVFile];

        var rowNumber = 0;
        foreach (var record in currentCsvData)
        {
            if (record.ContainsKey(field.CSVField))
            {
                var value = record[field.CSVField];

                var primaryKeyValue = rowNumber.ToString();

                var existingKey = pkToJsonMapping.Keys.FirstOrDefault(pkDict => pkDict.ContainsKey(field.CSVFile) && pkDict.ContainsValue(primaryKeyValue));
                if (existingKey != null)
                {
                    var existingJson = pkToJsonMapping[existingKey];

                    existingJson.Add(field.JSONField, value);

                }
                else
                {
                    var jsonObjectNested = new Dictionary<string, object>();
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
    private static void AddEmptyNestedFields(NestedMapping nestedField, Dictionary<string, object> jsonObjectNested, ILogger logger)
    {
        if (nestedField.NestedFields.Count > 0)
        {
            logger.LogWarning("Nested fields in nested fields are not supported. Adding empty arrays.");
            foreach (var nested in nestedField.NestedFields)
            {
                jsonObjectNested[nested.JSONNestedFieldName] = new List<Dictionary<string, object>>();
            }
        }
    }

    // Overload for pkToJsonMapping
    private static void AddEmptyNestedFields(NestedMapping nestedField, Dictionary<Dictionary<string, string>, Dictionary<string, object>> pkToJsonMapping, ILogger logger)
    {
        if (nestedField.NestedFields.Count > 0)
        {
            logger.LogWarning("Nested fields in nested fields are not supported. Adding empty arrays.");
            foreach (var nested in nestedField.NestedFields)
            {
                foreach (var map in pkToJsonMapping)
                {
                    map.Value.Add(nested.JSONNestedFieldName, new List<Dictionary<string, object>>());
                }
            }
        }
    }

    // Method to add fields without CSV
    private static void AddFieldsWithoutCsv(List<FieldMapping> jsonFieldsWithoutCsv, Dictionary<Dictionary<string, string>, Dictionary<string, object>> pkToJsonMapping)
    {
        foreach (var fieldWithoutCsv in jsonFieldsWithoutCsv)
        {
            foreach (var map in pkToJsonMapping)
            {
                map.Value.Add(fieldWithoutCsv.JSONField, string.Empty);
            }
        }
    }

}

