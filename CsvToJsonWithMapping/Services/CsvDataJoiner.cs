using CsvToJsonWithMapping.Models;
using Microsoft.Extensions.Logging;

public static class CsvDataJoiner
{
    /// <summary>
    /// Joins CSV data based on relations.
    /// </summary>
    /// <param name="relations">The list of relations.</param>
    /// <param name="csvData">The CSV data.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>
    /// A dictionary where:
    /// - The key is the name of the primary CSV file (`primaryKeyFile`).
    /// - The value is a list of dictionaries, where each dictionary represents a record in the primary CSV file, enriched with:
    ///   - The original fields from the primary CSV file as key-value pairs.
    ///   - An additional key for each related foreign CSV file (`foreignKeyFile`), where the value is a list of dictionaries.
    ///     - Each dictionary in this list corresponds to a record from the foreign CSV file that is related to the primary record via the specified foreign key.
    /// </returns>

    public static Dictionary<string, List<Dictionary<string, object?>>> JoinCsvDataBasedOnRelations(List<Relation> relations, Dictionary<string, List<Dictionary<string, string?>>> csvData, ILogger logger)
    {
        var result = new Dictionary<string, List<Dictionary<string, object?>>>();

        foreach (var relation in relations)
        {
            // Extract relationship metadata
            var primaryKeyField = relation.PrimaryKey.CSVField;
            var foreignKeyField = relation.ForeignKey.CSVField;
            var primaryKeyFile = relation.PrimaryKey.CSVFileName;
            var foreignKeyFile = relation.ForeignKey.CSVFileName;

            logger.LogInformation($"Processing relation between {primaryKeyFile} and {foreignKeyFile} based on fields: {primaryKeyField} and {foreignKeyField}");

            if (primaryKeyField != null && foreignKeyField == null)
            {
                if (!csvData.ContainsKey(primaryKeyFile))
                {
                    throw new Exception($"Missing file: {primaryKeyFile}");
                }

                var primaryKeyData = csvData[primaryKeyFile];
                logger.LogInformation($"Loaded {primaryKeyData.Count} PK records");

                // Initialize the result for the PK file if not already present
                if (!result.ContainsKey(primaryKeyFile))
                {
                    result[primaryKeyFile] = new List<Dictionary<string, object?>>();
                }

                foreach (var pkRecord in primaryKeyData)
                {
                    var pkValue = pkRecord[primaryKeyField];
                    // Create a new record if not found
                    var enrichedRecord = pkRecord.ToDictionary(
                        kvp => kvp.Key,
                        kvp => (object?)kvp.Value
                    );
                    result[primaryKeyFile].Add(enrichedRecord);

                }

            }
            else if (primaryKeyField != null && foreignKeyField != null)
            {
                // Validate CSV data
                if (!csvData.ContainsKey(primaryKeyFile) || !csvData.ContainsKey(foreignKeyFile))
                {
                    throw new Exception($"Missing file(s): {primaryKeyFile} or {foreignKeyFile}");
                }

                var primaryKeyData = csvData[primaryKeyFile];
                var foreignKeyData = csvData[foreignKeyFile];

                logger.LogInformation($"Loaded {primaryKeyData.Count} PK records and {foreignKeyData.Count} FK records");

                // Initialize the result for the PK file if not already present
                if (!result.ContainsKey(primaryKeyFile))
                {
                    result[primaryKeyFile] = new List<Dictionary<string, object?>>();
                }

                // Create a lookup of FK records by foreign key value
                var foreignKeyLookup = foreignKeyData
                    .GroupBy(record => record[foreignKeyField])
                    .ToDictionary(
                        group => group.Key,
                        group => group.ToList()
                    );

                // Process each PK record
                foreach (var pkRecord in primaryKeyData)
                {
                    var pkValue = pkRecord[primaryKeyField];

                    // Check if a record with this PK already exists in the result
                    var existingRecord = result[primaryKeyFile].FirstOrDefault(record => record[primaryKeyField] == pkValue);

                    if (existingRecord == null)
                    {
                        // Create a new record if not found
                        var enrichedRecord = pkRecord.ToDictionary(
                            kvp => kvp.Key,
                            kvp => (object?)kvp.Value
                        );

                        // Add related FK records as a list (or empty if none found)
                        if (foreignKeyLookup.TryGetValue(pkValue, out var relatedFkRecords))
                        {
                            enrichedRecord[foreignKeyFile] = relatedFkRecords;
                        }
                        else
                        {
                            enrichedRecord[foreignKeyFile] = new List<Dictionary<string, object?>>();
                            logger.LogWarning($"No FK records found for PK value: {pkValue}");
                        }

                        result[primaryKeyFile].Add(enrichedRecord);
                    }
                    else
                    {
                        // Add FK records to the existing record (combine them)
                        if (foreignKeyLookup.TryGetValue(pkValue, out var relatedFkRecords))
                        {
                            // Merge the related records into the existing one
                            if (existingRecord.ContainsKey(foreignKeyFile))
                            {
                                // Ensure it's a list to append
                                var existingRelatedRecords = existingRecord[foreignKeyFile] as List<Dictionary<string, string?>>;
                                existingRelatedRecords.AddRange(relatedFkRecords);
                            }
                            else
                            {
                                existingRecord[foreignKeyFile] = relatedFkRecords;
                            }
                        }
                        else
                        {
                            logger.LogWarning($"No FK records found for PK value: {pkValue}");
                        }
                    }
                }
            }
        }

        return result;
    }
}
