
using CsvHelper;
using CsvToJsonWithMapping.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;

/* TODO:
* Optimize the code less repetition
* Optimize the code more readable
* Optimize the code more maintainable (separate concerns (usage of different files/classes))
* 
* TESTs needed
* 
* 
* Rules implementation:
* - once a rule is broken, log the error and continue with the next field then dont create a output but log the error
* - under each mapping field there is a CSVField and CSVFile and there will be a Rule field
*      - rules can be:
*          - the field is required (cannot be empty or null)
*          - the field type (string, int, double, etc.) (it will try to parse to correct type, if not possible than error)
*          - the field length (min and max)
*          - Regex pattern (for example email, phone number, etc.)
*          - Converter (for example if the field is "yes" than convert to true, if "no" than convert to false, or when field value = 0 then it shpould become "pig")
*          
*          
*     Advanced rules (maybe if time):
*       - custom rule (for example if field A is not empty then field B should be empty) (hardest part: with current mapping proces this type of validation can only be done after all fields are processed)
 */

class Program
{
    static void Main(string[] args)
    {
        // Stel logging in
        // Set up logging
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });
        var logger = loggerFactory.CreateLogger<Program>();

        // Verkrijg de basisdirectory van de huidige applicatie
        // Get the base directory of the current application
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

        // Definieer de bestandspaden
        // Define the file paths
        var csvFilesDirectory = Path.Combine(baseDirectory, @"..\..\..\CsvFiles");
        var mappingsFilePath = Path.Combine(baseDirectory, @"..\..\..\CsvToJsonMappings\mapping.json");
        var relationsFilePath = Path.Combine(baseDirectory, @"..\..\..\CsvToJsonMappings\relations.json");
        var outputJsonPath = Path.Combine(baseDirectory, @"..\..\..\finalOutput.json");

        logger.LogInformation("Begin met het verwerken van bestanden...");
        // Starting to process files...

        try
        {
            // Laad het mapping-bestand
            // Load the mapping file
            if (!File.Exists(mappingsFilePath))
            {
                logger.LogError($"Mapping-bestand niet gevonden op pad: {mappingsFilePath}");
                // Mapping file not found at path
                return;
            }

            if (!File.Exists(relationsFilePath))
            {
                logger.LogError($"Relatiebestand niet gevonden op pad: {relationsFilePath}");
                // Relations file not found at path
                return;
            }

            // Lees het relatiebestand
            // Read the relations file
            var relationsJson = File.ReadAllText(relationsFilePath);
            var relations = JsonSerializer.Deserialize<List<Relation>>(relationsJson);

            logger.LogInformation($"Relatiegegevens: {JsonSerializer.Serialize(relations, new JsonSerializerOptions { WriteIndented = true })}");

            var duplicateRelations = relations.GroupBy(x => new { x.ForeignKey.CSVFileName, x.ForeignKey.CSVField })
                .Where(g => g.Count() > 1)
                .Select(y => y.Key)
                .ToList();

            if (duplicateRelations.Any())
            {
                logger.LogError($"Duplicate relations found: {JsonSerializer.Serialize(duplicateRelations, new JsonSerializerOptions { WriteIndented = true })}");
                throw new Exception("Duplicate relations found in relations file.");
            }

            // Relations data logged in readable format

            // Lees het mapping-bestand
            // Read the mapping file
            var mappingsJson = File.ReadAllText(mappingsFilePath);
            var mapping = JsonSerializer.Deserialize<Mapping>(mappingsJson);

            logger.LogInformation($"Mappinggegevens: {JsonSerializer.Serialize(mapping, new JsonSerializerOptions { WriteIndented = true })}");
            // Mapping data logged in readable format

            if (mapping == null)
            {
                logger.LogError("Kon het mapping-bestand niet deserialiseren.");
                throw new Exception("Mapping-bestand kon niet worden gedeserialiseerd.");
            }

            // Laad CSV-gegevens
            // Load CSV data
            if (!Directory.Exists(csvFilesDirectory))
            {
                logger.LogError($"CSV-bestandsdirectory niet gevonden: {csvFilesDirectory}");
                throw new Exception("CSV-bestandsdirectory niet gevonden.");
            }
            var csvFilePaths = Directory.GetFiles(csvFilesDirectory, "*.csv");
            if (csvFilePaths.Length == 0)
            {
                logger.LogError("Geen CSV-bestanden gevonden in de directory.");
                throw new Exception("Geen CSV-bestanden gevonden in de directory.");
            }

            var csvData = ReadCsvFiles(csvFilePaths, logger);

            logger.LogInformation($"CSV-gegevens: {JsonSerializer.Serialize(csvData, new JsonSerializerOptions { WriteIndented = true })}");
            // Logging all CSV data

            // Combineer gegevens op basis van relaties
            // Combine data based on relations
            var joinedData = JoinCsvDataBasedOnRelations(relations, csvData, logger);

            // Converteer gecombineerde data naar JSON
            // Convert the combined data into JSON
            var finalResult = GenerateJsonFromMappings(mapping, relations, csvData, joinedData, logger);

            // Schrijf de JSON-uitvoer naar een bestand
            // Write the JSON output to a file
            var jsonOutput = JsonSerializer.Serialize(finalResult, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(outputJsonPath, jsonOutput);

            logger.LogInformation($"JSON-uitvoer gegenereerd op: {outputJsonPath}");
            // JSON output generated successfully
            //Console.WriteLine(jsonOutput);
        }
        catch (Exception ex)
        {
            logger.LogError($"Er is een fout opgetreden: {ex.Message}");
            // An error occurred during processing
        }
    }

    // Methode om CSV-bestanden te lezen en de gegevens als een dictionary terug te geven
    // Method to read CSV files and return the data as a dictionary
    public static Dictionary<string, List<Dictionary<string, string>>> ReadCsvFiles(IEnumerable<string> csvFilePaths, ILogger logger)
    {
        var csvData = new Dictionary<string, List<Dictionary<string, string>>>();

        foreach (var filePath in csvFilePaths)
        {
            try
            {
                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                string fileNameWithExtension = Path.GetFileName(filePath);
                // Converteer de CSV-records naar een dynamische lijst
                // Convert CSV records into a dynamic list
                var records = csv.GetRecords<dynamic>().ToList();
                csvData[fileNameWithExtension] = records.Select(record => (IDictionary<string, object>)record)
                                            .Select(dict => dict.ToDictionary(k => k.Key, k => k.Value?.ToString() ?? string.Empty))
                                            .ToList();

                logger.LogInformation($"CSV-bestand succesvol gelezen: {fileNameWithExtension}");
                // Successfully read CSV file
            }
            catch (Exception ex)
            {
                logger.LogError($"Fout bij het lezen van CSV-bestand '{filePath}': {ex.Message}");
                // Error reading the CSV file
            }
        }

        return csvData;
    }

    // Methode om CSV-gegevens te combineren op basis van relaties
    // Method to combine CSV data based on relations
    public static Dictionary<string, List<Dictionary<string, object>>> JoinCsvDataBasedOnRelations(List<Relation> relations, Dictionary<string, List<Dictionary<string, string>>> csvData, ILogger logger)
    {
        // Create a list to store the final joined data
        // Dictionary to store the final result, organized by PK file
        var result = new Dictionary<string, List<Dictionary<string, object>>>();

        // Process each relation
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
                    logger.LogError($"Missing file: {primaryKeyFile}");
                    throw new Exception($"Missing file: {primaryKeyFile}");
                }

                var primaryKeyData = csvData[primaryKeyFile];
                logger.LogInformation($"Loaded {primaryKeyData.Count} PK records");

                // Initialize the result for the PK file if not already present
                if (!result.ContainsKey(primaryKeyFile))
                {
                    result[primaryKeyFile] = new List<Dictionary<string, object>>();
                }

                foreach (var pkRecord in primaryKeyData)
                {
                    var pkValue = pkRecord[primaryKeyField];
                    // Create a new record if not found
                    var enrichedRecord = pkRecord.ToDictionary(
                        kvp => kvp.Key,
                        kvp => (object)kvp.Value
                    );
                    result[primaryKeyFile].Add(enrichedRecord);

                }

            }
            else if (primaryKeyField != null && foreignKeyField != null)
            {
                // Validate CSV data
                if (!csvData.ContainsKey(primaryKeyFile) || !csvData.ContainsKey(foreignKeyFile))
                {
                    logger.LogError($"Missing file(s): {primaryKeyFile} or {foreignKeyFile}");
                    throw new Exception($"Missing file(s): {primaryKeyFile} or {foreignKeyFile}");
                }

                var primaryKeyData = csvData[primaryKeyFile];
                var foreignKeyData = csvData[foreignKeyFile];

                logger.LogInformation($"Loaded {primaryKeyData.Count} PK records and {foreignKeyData.Count} FK records");

                // Initialize the result for the PK file if not already present
                if (!result.ContainsKey(primaryKeyFile))
                {
                    result[primaryKeyFile] = new List<Dictionary<string, object>>();
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
                    var existingRecord = result[primaryKeyFile].FirstOrDefault(record => record[primaryKeyField].ToString() == pkValue);

                    if (existingRecord == null)
                    {
                        // Create a new record if not found
                        var enrichedRecord = pkRecord.ToDictionary(
                            kvp => kvp.Key,
                            kvp => (object)kvp.Value
                        );

                        // Add related FK records as a list (or empty if none found)
                        if (foreignKeyLookup.TryGetValue(pkValue, out var relatedFkRecords))
                        {
                            enrichedRecord[foreignKeyFile] = relatedFkRecords;
                        }
                        else
                        {
                            enrichedRecord[foreignKeyFile] = new List<Dictionary<string, string>>();
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
                                var existingRelatedRecords = (List<Dictionary<string, string>>)existingRecord[foreignKeyFile];
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


    // Methode om JSON te genereren uit mapping en gegevens
    // Method to generate JSON from mapping and data
    public static List<Dictionary<string, object>> GenerateJsonFromMappings(Mapping mapping, List<Relation> relations, Dictionary<string, List<Dictionary<string, string>>> csvData, Dictionary<string, List<Dictionary<string, object>>> joinedData, ILogger logger)
    {
        // Lijst om gegenereerde JSON-objecten op te slaan
        var resultJson = new List<Dictionary<string, object>>();

        // Loggen van de start van het proces
        logger.LogInformation("Start met het genereren van JSON-objecten op basis van de mapping.");

        var count = 0;
        var jsonObject = new Dictionary<string, object>();
        // Itereer door de hoofdvelden die zijn gedefinieerd in de mapping
        foreach (var fieldMapping in mapping.Fields)
        {
            if (fieldMapping.JSONField == null)
            {
                logger.LogError($"JSON-veld ontbreekt in de mapping voor '{fieldMapping.CSVField}'.");
                throw new Exception($"JSON-veld ontbreekt in de mapping voor '{fieldMapping.CSVField}'.");
            }

            if (fieldMapping.CSVFile == null || fieldMapping.CSVField == null)
            {
                logger.LogWarning($"CSV-bestand of CSV-veld ontbreekt in de mapping voor '{fieldMapping.JSONField}'.");
                jsonObject[fieldMapping.JSONField] = string.Empty;
                continue;
            }

            // TODO: HELP MET DENKEN - Hoe gaan we om met dit soort velden?, vraag aan het bedrijf??? 
            // LOGICA hiervoor moet nog aangepast worden misschien???
            // Controleer of het vereiste CSV-bestand aanwezig is in de CSV-gegevens
            if (!csvData.ContainsKey(fieldMapping.CSVFile))
            {
                logger.LogError($"CSV-bestand '{fieldMapping.CSVFile}' ontbreekt in de aangeleverde data.");
                throw new Exception($"CSV-bestand '{fieldMapping.CSVFile}' ontbreekt in de aangeleverde data.");
            }

            var currentCsvData = csvData[fieldMapping.CSVFile];

            // normale velden zijn velden zijn de toplayer dus die gaan op basis van de volgorde van de csv data (count) (wordt in voorbeelden van agro weinig gebruikt)
            jsonObject[fieldMapping.JSONField] = currentCsvData[count][fieldMapping.CSVField];


        }

        foreach (var nestedField in mapping.NestedFields)
        {

            logger.LogInformation($"Verwerken type {nestedField.JSONNestedType} velden voor '{nestedField.JSONNestedFieldName}'.");

            if (Enum.Parse<NestedType>(nestedField.JSONNestedType) == NestedType.Object)
            {
                // OVERLEGGEN MET TEAM: Hoe doen we dit? en waar komt deze data vandaan? // in huidige templates is dit niet nodig.... maar msischien wilt het bedrijf dit wel
                // TODO: Implementeer de verwerking van geneste objecten
            }
            else if (Enum.Parse<NestedType>(nestedField.JSONNestedType) == NestedType.Array)
            {
                // pkToJsonMapping is een Dictionary waarin:
                // - De sleutel (key) van de eerste Dictionary een Dictionary<string, string> is, 
                //   waarin de sleutel (key) een bestandsnaam van de PK (Primary Key) vertegenwoordigt 
                //   en de waarde (value) de waarde van die PK bevat.
                // - De waarde (value) van de eerste Dictionary een andere Dictionary<string, object> is, 
                //   die het bijbehorende gecreëerde JSON-object bevat.
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

                        // Controleer of het opgegeven CSV-bestand bestaat in de csvData
                        if (!csvData.ContainsKey(field.CSVFile))
                        {
                            logger.LogError($"CSV-bestand '{field.CSVFile}' niet gevonden.");
                            throw new Exception($"CSV-bestand '{field.CSVFile}' niet gevonden.");
                        }

                        logger.LogInformation($"Verwerken van veld '{field.CSVField}' in bestand '{field.CSVFile}'.");

                        // Zoek naar relaties die betrekking hebben op het csv-bestand van het veld
                        var fileFkRelations = relations.FindAll(x => x.ForeignKey.CSVFileName == field.CSVFile).ToList();
                        var filePkRelations = relations.FindAll(x => x.PrimaryKey.CSVFileName == field.CSVFile).ToList();

                        if (fileFkRelations.Any())
                        {
                            logger.LogInformation($"FK Relaties gevonden voor '{field.CSVField}' in bestand '{field.CSVFile}': {JsonSerializer.Serialize(fileFkRelations, new JsonSerializerOptions { WriteIndented = true })}");
                            Relation? correctRelation = null;
                            List<Dictionary<string, object>>? correctJoined = null;

                            // Zoek de juiste relatie en gejoinede data
                            foreach (var fileRelation in fileFkRelations)
                            {
                                var joinesForThisFile = joinedData.Where(x => x.Key == fileRelation.PrimaryKey.CSVFileName).ToList();
                                logger.LogInformation($"Joined data gevonden voor '{field.CSVFile}': {JsonSerializer.Serialize(joinesForThisFile, new JsonSerializerOptions { WriteIndented = true })}");

                                // Meerdere joins: zoek de juiste op basis van aanwezigheid van de file in de join
                                foreach (var joinForThisFile in joinesForThisFile)
                                {
                                    var isCorrectJoin = joinForThisFile.Value.Any(x => x.ContainsKey(field.CSVFile));
                                    if (isCorrectJoin)
                                    {

                                        correctJoined = joinForThisFile.Value;
                                        correctRelation = fileRelation;
                                        logger.LogInformation($"Juiste gejoinede data gevonden voor '{field.CSVFile}'.\n{JsonSerializer.Serialize(correctJoined, new JsonSerializerOptions { WriteIndented = true })}");
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
                                logger.LogError($"Geen gejoined data gevonden voor '{field.CSVFile}'.\nMaar had vwel verwacht");
                                throw new Exception($"Geen gejoined data gevonden voor '{field.CSVFile}'.\nMaar had wel verwacht");
                            }


                            // Verwerk de gejoinede data
                            foreach (var record in correctJoined)
                            {
                                // Haal de waarde van de primaire sleutel op
                                var primaryKeyValue = record[correctRelation.PrimaryKey.CSVField].ToString();
                                if (primaryKeyValue == null)
                                {
                                    throw new Exception($"Geen primary key value gevonden voor '{correctRelation.PrimaryKey.CSVField}' in bestand '{correctRelation.PrimaryKey.CSVFileName}'.");
                                }

                                var fileData = ((List<Dictionary<string, string>>)record[field.CSVFile]).FirstOrDefault();
                                if (fileData == null)
                                {
                                    throw new Exception($"Geen data gevonden voor '{field.CSVFile}' in gejoined record.");
                                }
                                if (!fileData.ContainsKey(field.CSVField))
                                {
                                    throw new Exception($"Geen data gevonden voor '{field.CSVField}' in gejoined record.");
                                }

                                // Controleer of er al een JSON-object bestaat voor deze sleutel
                                var existingKey = pkToJsonMapping.Keys.FirstOrDefault(pkDict => pkDict.ContainsValue(primaryKeyValue) && pkDict.ContainsKey(correctRelation.PrimaryKey.CSVFileName));
                                if (existingKey != null)
                                {
                                    // Haal het bestaande JSON-object op
                                    var existingJson = pkToJsonMapping[existingKey];

                                    // Voeg een nieuw veld toe aan het bestaande object,
                                    if (existingJson.ContainsKey(field.JSONField))
                                    {
                                        // Indien het veld al bestaat, kun je ervoor kiezen om de waarde bij te werken of toe te voegen (afhankelijk van je behoeften)
                                        existingJson[field.JSONField] = fileData[field.CSVField];
                                    }
                                    else
                                    {
                                        // Voeg het veld toe aan het bestaande JSON-object als het nog niet bestaat
                                        existingJson.Add(field.JSONField, fileData[field.CSVField]);
                                    }

                                }
                                else
                                {
                                    // Maak een nieuw JSON-object aan
                                    var jsonObjectNested = new Dictionary<string, object>();

                                    jsonObjectNested[field.JSONField] = fileData[field.CSVField];

                                    pkToJsonMapping[new Dictionary<string, string> { { correctRelation.PrimaryKey.CSVFileName, primaryKeyValue } }] = jsonObjectNested;

                                }
                            }

                        }
                        else if (filePkRelations.Any())
                        {
                            logger.LogInformation($"PK Relaties gevonden voor '{field.CSVField}' in bestand '{field.CSVFile}': {JsonSerializer.Serialize(filePkRelations, new JsonSerializerOptions { WriteIndented = true })}");
                            Relation? correctRelation = null;
                            List<Dictionary<string, object>>? correctJoined = null;

                            // Zoek de juiste relatie en gejoinede data
                            foreach (var fileRelation in filePkRelations)
                            {
                                // Zoek de juiste gejoinede data op basis van de Foreign Key
                                var joinesForThisFile = joinedData.Where(x => x.Key == fileRelation.PrimaryKey.CSVFileName).ToList();
                                logger.LogInformation($"Joined data gevonden voor '{field.CSVFile}': {JsonSerializer.Serialize(joinesForThisFile, new JsonSerializerOptions { WriteIndented = true })}");

                                foreach (var joinForThisFile in joinesForThisFile)
                                {
                                    var isCorrectJoin = joinForThisFile.Key == field.CSVFile;
                                    if (isCorrectJoin)
                                    {
                                        correctJoined = joinForThisFile.Value;
                                        correctRelation = fileRelation;
                                        logger.LogInformation($"Juiste gejoinede data gevonden voor '{field.CSVFile}'.\n{JsonSerializer.Serialize(correctJoined, new JsonSerializerOptions { WriteIndented = true })}");
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
                                throw new Exception($"Geen gejoined data gevonden voor '{field.CSVFile}' voor de PK-relatie.");
                            }

                            // Verwerk de gejoinede data
                            foreach (var fileData in correctJoined)
                            {
                                // Zoek naar de waarde van de primaire sleutel in de gejoinede data
                                var primaryKeyValue = fileData.GetValueOrDefault(correctRelation.PrimaryKey.CSVField).ToString();
                                if (primaryKeyValue == null)
                                {
                                    throw new Exception($"Geen primary key value gevonden voor '{correctRelation.PrimaryKey.CSVField}' in bestand '{correctRelation.PrimaryKey.CSVFileName}'.");
                                }

                                if (!fileData.ContainsKey(field.CSVField))
                                {
                                    throw new Exception($"Geen data gevonden voor '{field.CSVField}' in gejoined record.");
                                }

                                // Zoek of er al een JSON-object bestaat voor deze primaire sleutel
                                var existingKey = pkToJsonMapping.Keys.FirstOrDefault(pkDict => pkDict.ContainsKey(correctRelation.PrimaryKey.CSVFileName) && pkDict.ContainsValue(primaryKeyValue));
                                if (existingKey != null)
                                {
                                    // Haal het bestaande JSON-object op
                                    var existingJson = pkToJsonMapping[existingKey];

                                    // Voeg een nieuw veld toe aan het bestaande object 
                                    existingJson.Add(field.JSONField, fileData[field.CSVField]);
                                }
                                else
                                {
                                    // Maak een nieuw JSON-object aan als er nog geen bestaat
                                    var jsonObjectNested = new Dictionary<string, object>();
                                    jsonObjectNested[field.JSONField] = fileData[field.CSVField];

                                    // Voeg dit nieuwe JSON-object toe aan de mapping
                                    pkToJsonMapping[new Dictionary<string, string> { { correctRelation.PrimaryKey.CSVFileName, primaryKeyValue } }] = jsonObjectNested;
                                }
                            }
                        }
                        else
                        {

                            logger.LogWarning($"Geen relatie gevonden voor '{field.CSVField}' in bestand '{field.CSVFile}'.");

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
                                        // Haal het bestaande JSON-object op
                                        var existingJson = pkToJsonMapping[existingKey];



                                        existingJson.Add(field.JSONField, value); // Toevoegen van het veld

                                    }
                                    else
                                    {
                                        // Maak een nieuw JSON-object aan als er nog geen bestaat
                                        var jsonObjectNested = new Dictionary<string, object>();
                                        jsonObjectNested[field.JSONField] = value;

                                        // Voeg dit nieuwe JSON-object toe aan de mapping
                                        pkToJsonMapping[new Dictionary<string, string> { { field.CSVFile, primaryKeyValue } }] = jsonObjectNested;
                                    }
                                    rowNumber++;
                                }
                                else
                                {
                                    throw new Exception($"CSV-veld '{field.CSVField}' niet gevonden in {field.CSVFile}.");
                                }

                            }

                        }
                    }

                });

                if (nestedField.NestedFields.Count > 0)
                {
                    // Nested fields in nested fields is not supported // in huidige templates is dit niet nodig.... maar msischien wilt het bedrijf dit wel?
                    logger.LogWarning("Nested fields in nested fields is not supported\nAdding empty arrays");
                    foreach (var nested in nestedField.NestedFields)
                    {
                        foreach (var map in pkToJsonMapping)
                        {
                            map.Value.Add(nested.JSONNestedFieldName, new List<Dictionary<string, object>>());
                        }
                    }
                }


                // Voeg de velden toe die geen CSV-bestand hebben maar wel in de mapping zijn gedefinieerd
                foreach (var fieldWithoutCsv in jsonFieldsWithoutCsv)
                {
                    foreach (var map in pkToJsonMapping)
                    {
                        map.Value.Add(fieldWithoutCsv.JSONField, string.Empty);
                    }
                }

                // Voeg de gegenereerde JSON-objecten toe aan het hoofdobject
                jsonObject[nestedField.JSONNestedFieldName] = pkToJsonMapping.Values.ToList();
            }
            else
            {
                throw new Exception($"Onbekend type {nestedField.JSONNestedType} voor {nestedField.JSONNestedFieldName}");
            }
        }

        resultJson.Add(jsonObject);

        logger.LogInformation("JSON-objecten gegenereerd: " +
            JsonSerializer.Serialize(resultJson, new JsonSerializerOptions { WriteIndented = true }));
        return resultJson;

    }
}
