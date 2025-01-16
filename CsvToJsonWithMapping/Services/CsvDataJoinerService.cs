using CsvToJsonWithMapping.Models;

namespace CsvToJsonWithMapping.Services
{
    public class CsvDataJoinerService
    {
        public Dictionary<string, IEnumerable<IDictionary<string, object?>>> JoinCsvDataBasedOnRelations(List<Relation> relations, Dictionary<string, IEnumerable<IDictionary<string, string?>>> csvData)
        {
            var result = new Dictionary<string, List<Dictionary<string, object?>>>();

            foreach (var relation in relations)
            {
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

                    if (!result.ContainsKey(primaryKeyFile))
                    {
                        result[primaryKeyFile] = new List<Dictionary<string, object?>>();
                    }

                    foreach (var pkRecord in primaryKeyData)
                    {
                        if (!pkRecord.ContainsKey(primaryKeyField))
                        {
                            LogPublisher.PublishLogMessage("Error: CSVData - Missing column", $"The column '{primaryKeyField}' is not found in '{primaryKeyFile}'");
                            break;
                        }
                        var pkValue = pkRecord[primaryKeyField];
                        var enrichedRecord = pkRecord.ToDictionary(
                            kvp => kvp.Key,
                            kvp => (object?)kvp.Value
                        );
                        result[primaryKeyFile].Add(enrichedRecord);

                    }

                }
                else if (primaryKeyField != null && foreignKeyField != null)
                {
                    if (!csvData.ContainsKey(primaryKeyFile) || !csvData.ContainsKey(foreignKeyFile))
                    {
                        throw new Exception($"Missing file(s): {primaryKeyFile} or {foreignKeyFile}");
                    }

                    var primaryKeyData = csvData[primaryKeyFile];
                    var foreignKeyData = csvData[foreignKeyFile];

                    LogPublisher.PublishLogMessage("Information", $"Processing {primaryKeyData.Count()} records from {primaryKeyFile}");

                    if (!result.ContainsKey(primaryKeyFile))
                    {
                        result[primaryKeyFile] = new List<Dictionary<string, object?>>();
                    }

                    var foreignKeyLookup = foreignKeyData
                    .Where(record => record.ContainsKey(foreignKeyField) && record[foreignKeyField] != null)
                    .GroupBy(record => record[foreignKeyField])
                    .ToDictionary(
                        group => group.Key,
                        group => group.ToList()
                    );

                    foreach (var pkRecord in primaryKeyData)
                    {
                        if (!pkRecord.ContainsKey(primaryKeyField))
                        {
                            LogPublisher.PublishLogMessage("Error: CSVData - Missing column", $"The column '{primaryKeyField}' is not found in '{primaryKeyFile}'");
                            break;
                        }

                        var pkValue = pkRecord[primaryKeyField];

                        var existingRecord = result[primaryKeyFile].FirstOrDefault(record => record[primaryKeyField].ToString() == pkValue);

                        if (existingRecord == null)
                        {
                            var enrichedRecord = pkRecord.ToDictionary(
                                kvp => kvp.Key,
                                kvp => (object?)kvp.Value
                            );

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
                            if (foreignKeyLookup.TryGetValue(pkValue, out var relatedFkRecords))
                            {
                                if (existingRecord.ContainsKey(foreignKeyFile))
                                {
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
