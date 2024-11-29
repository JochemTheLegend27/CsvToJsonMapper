using CsvToJsonWithMapping.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public static class JsonGenerator
{

    /// <summary>
    /// Generates JSON objects based on the provided mapping.
    /// </summary>
    /// <param name="mapping">The mapping configuration.</param>
    /// <param name="relations">The list of relations.</param>
    /// <param name="csvData">The CSV data.</param>
    /// <param name="joinedData">The joined data.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The generated JSON objects.</returns>
    public static List<Dictionary<string, object>> GenerateJsonFromMappings(Mapping mapping, List<Relation> relations, Dictionary<string, List<Dictionary<string, string>>> csvData, Dictionary<string, List<Dictionary<string, object>>> joinedData, ILogger logger)
    {
        var resultJson = new List<Dictionary<string, object>>();

        logger.LogInformation("Start generating JSON objects based on the mapping.");

        var count = 0;
        var jsonObject = new Dictionary<string, object>();
        foreach (var fieldMapping in mapping.Fields)
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

        foreach (var nestedField in mapping.NestedFields)
        {

            logger.LogInformation($"Processing type {nestedField.JSONNestedType} fields for '{nestedField.JSONNestedFieldName}'.");

            if (Enum.Parse<NestedType>(nestedField.JSONNestedType) == NestedType.Object)
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
                        logger.LogWarning($"CSV file or CSV field is missing from the mapping for '{field.JSONField}'.\nSo empty string added.");
                        jsonObject[field.JSONField] = string.Empty;
                    }
                    else
                    {

                        if (!csvData.ContainsKey(field.CSVFile))
                        {
                            throw new Exception($"CSV file '{field.CSVFile}' is missing from the supplied data.");
                        }

                        var currentCsvData = csvData[field.CSVFile];


                        // normal fields are fields that are the top layer so they go based on the order of the csv data (count)
                        jsonObjectNested[field.JSONField] = currentCsvData[count][field.CSVField];
                    }

                });

                if (nestedField.NestedFields.Count > 0)
                {
                    // Nested fields in nested fields is not supported 
                    // in current templates this is not necessary.... but maybe the company wants it?
                    logger.LogWarning("Nested fields in nested fields is not supported\nAdding empty arrays");
                    foreach (var nested in nestedField.NestedFields)
                    {
                        jsonObjectNested[nested.JSONNestedFieldName] = new List<Dictionary<string, object>>();
                    }
                }

                jsonObject[nestedField.JSONNestedFieldName] = jsonObjectNested;

            }
            else if (Enum.Parse<NestedType>(nestedField.JSONNestedType) == NestedType.Array)
            {
                // pkToJsonMapping is a Dictionary where:
                // - The key of the first Dictionary is a Dictionary<string, string>,
                //   where the key represents a filename of the PK (Primary Key)
                //   and the value contains the value of that PK.
                // - The value of the first Dictionary is another Dictionary<string, object>,
                //   containing the corresponding created JSON object.
                var pkToJsonMapping = new Dictionary<Dictionary<string, string>, Dictionary<string, object>>();

                List<FieldMapping> jsonFieldsWithoutCsv = new List<FieldMapping>();


                nestedField.Fields.ForEach(field =>
                {

                    if (field.JSONField == null)
                    {
                        logger.LogError($"JSON-veld ontbreekt in de mapping voor '{field.CSVField}'.");
                        throw new Exception($"JSON-veld ontbreekt in de mapping voor '{field.CSVField}'.");
                    }

                    if (field.CSVFile == null || field.CSVField == null)
                    {
                        logger.LogWarning($"CSV-bestand of CSV-veld ontbreekt in de mapping voor '{field.JSONField}'.");
                        jsonFieldsWithoutCsv.Add(field);
                    }
                    else
                    {

                        if (!csvData.ContainsKey(field.CSVFile))
                        {
                            throw new Exception($"CSV file '{field.CSVFile}' not found.");
                        }

                        logger.LogInformation($"Processing field '{field.CSVField}' in file '{field.CSVFile}'.");

                        var fileFkRelations = relations.FindAll(x => x.ForeignKey.CSVFileName == field.CSVFile).ToList();
                        var filePkRelations = relations.FindAll(x => x.PrimaryKey.CSVFileName == field.CSVFile).ToList();

                        if (fileFkRelations.Any())
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
                        else if (filePkRelations.Any())
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
                        else
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
                    }

                });

                if (nestedField.NestedFields.Count > 0)
                {
                    // Nested fields in nested fields is not supported 
                    // in current templates this is not necessary.... but maybe the company wants it?
                    logger.LogWarning("Nested fields in nested fields is not supported\nAdding empty arrays");
                    foreach (var nested in nestedField.NestedFields)
                    {
                        foreach (var map in pkToJsonMapping)
                        {
                            map.Value.Add(nested.JSONNestedFieldName, new List<Dictionary<string, object>>());
                        }
                    }
                }


                // Add the fields that do not have a CSV file but are defined in the mapping
                foreach (var fieldWithoutCsv in jsonFieldsWithoutCsv)
                {
                    foreach (var map in pkToJsonMapping)
                    {
                        map.Value.Add(fieldWithoutCsv.JSONField, string.Empty);
                    }
                }

                // Add the generated JSON objects to the main object
                jsonObject[nestedField.JSONNestedFieldName] = pkToJsonMapping.Values.ToList();
            }
            else
            {
                throw new Exception($"Unknown type {nestedField.JSONNestedType} for {nestedField.JSONNestedFieldName}");
            }
        }

        resultJson.Add(jsonObject);

        logger.LogInformation("JSON objects generated: " + JsonSerializer.Serialize(resultJson, new JsonSerializerOptions { WriteIndented = true }));
        return resultJson;
    }
}

