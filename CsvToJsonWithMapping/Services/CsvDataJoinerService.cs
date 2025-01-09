using CsvToJsonWithMapping.Models;
using CsvToJsonWithMapping.Services;
using Microsoft.Extensions.Logging;

namespace CsvToJsonWithMapping.Services
{
    public class CsvDataJoinerService
    {
        /// <summary>
        /// Joins CSV data based on relations.
        /// </summary>
        /// <param name="relations">The list of relations.</param>
        /// <param name="csvData">The CSV data.</param>
        /// <returns>
        /// A dictionary where:
        /// - The key is the name of the primary CSV file (`primaryKeyFile`).
        /// - The value is a list of dictionaries, where each dictionary represents a record in the primary CSV file, enriched with:
        ///   - The original fields from the primary CSV file as key-value pairs.
        ///   - An additional key for each related foreign CSV file (`foreignKeyFile`), where the value is a list of dictionaries.
        ///     - Each dictionary in this list corresponds to a record from the foreign CSV file that is related to the primary record via the specified foreign key.
        /// </returns>
        /// 

        public Dictionary<string, IEnumerable<IDictionary<string, object?>>> JoinCsvDataBasedOnRelations(List<Relation> relations, Dictionary<string, IEnumerable<IDictionary<string, string?>>> csvData)
        {
            var result = new Dictionary<string, List<Dictionary<string, object?>>>();

            foreach (var relation in relations)
            {
                // Extract relationship metadata
                var primaryKeyField = relation.PrimaryKey.CSVField;
                var foreignKeyField = relation.ForeignKey.CSVField;
                var primaryKeyFile = relation.PrimaryKey.CSVFileName;
                var foreignKeyFile = relation.ForeignKey.CSVFileName;

                LogPublisher.PublishLogMessage("Information", $"Processing relation: {primaryKeyFile} ({primaryKeyField}) -> {foreignKeyFile} ({foreignKeyField})");

                if (primaryKeyField != null && foreignKeyField == null)
                {
                    if (!csvData.ContainsKey(primaryKeyFile))
                    {
                        throw new Exception($"Missing file: {primaryKeyFile}");
                    }

                    var primaryKeyData = csvData[primaryKeyFile];
                    LogPublisher.PublishLogMessage("Information", $"Processing {primaryKeyData.Count()} records from {primaryKeyFile}");

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

                    LogPublisher.PublishLogMessage("Information", $"Processing {primaryKeyData.Count()} records from {primaryKeyFile}");

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
                                LogPublisher.PublishLogMessage($"Relation Warning {primaryKeyFile} <=> {foreignKeyFile} ", $"No related records found for {primaryKeyFile} ({primaryKeyField} = {pkValue}) in {foreignKeyFile}");
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
                                    var existingRelatedRecords = existingRecord[foreignKeyFile] as List<IDictionary<string, string?>>;
                                    existingRelatedRecords.AddRange(relatedFkRecords);

                                    existingRecord[foreignKeyFile] = existingRelatedRecords;
                                }
                                else
                                {
                                    existingRecord[foreignKeyFile] = relatedFkRecords;
                                }
                            }
                            else
                            {
                                LogPublisher.PublishLogMessage($"Relation Warning {primaryKeyFile} <=> {foreignKeyFile} ", $"No related records found for {primaryKeyFile} ({primaryKeyField} = {pkValue}) in {foreignKeyFile}");
                            }
                        }
                    }
                }
            }

            var finalResult = new Dictionary<string, IEnumerable<IDictionary<string, object?>>>();
            foreach (var kvp in result)
            {
                finalResult[kvp.Key] = kvp.Value;
            }

            return finalResult;
        }
    }
}
