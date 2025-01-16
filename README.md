# CSV to JSON with Mapping
The **CSV to JSON Mapping** tool simplifies converting raw CSV files, such as database exports, into a single, well-structured JSON file. Using easy-to-create `mapping.json` & `relations.json` files, you can define how the data should be transformed and how relationships between CSV files (primary and foreign keys) should be linked. The tool also provides validations to ensure reliable and accurate results. This makes it perfect for working with large datasets or complex data structures.

## Capabilities
- **Dynamic CSV Mapping**: Converts flat CSV data into JSON, adhering to customizable mapping rules.
- **Data Validation**: Ensures fields meet specified criteria.
- **Relational Joins**: Supports merging data across multiple CSVs, mimicking relational database functionality with primary and foreign key relationships.
- **Error Logging and Debugging**: Tracks progress and flags errors or warnings with a built-in logging service.
- **Scalability and Performance**: Handles large datasets efficiently using in-memory data structures and lazy evaluations.

---

## Key Advantages
- **Flexibility**: Adapts to varying CSV schemas and mapping requirements.
- **Comprehensive Validation**: Prevents invalid data from entering the final output.
- **Modularity**: Built as a collection of independent services for better maintainability and extensibility.
- **High Performance**: Optimized to minimize memory usage and enhance processing speed for large-scale datasets.

---

## How Does It Work?
- **Input Data**: Takes raw CSV files, mapping configurations, and relationship definitions as input.
- **Data Processing Pipeline**: Orchestrates the entire flow, starting from reading CSVs, joining related data, and validating fields, to ultimately generating the desired JSON output.
- **Intermediate Results**: Maintains enriched and validated data in memory for downstream processes.
- **Output**: Produces validated, nested JSON files.

---

## Usage Instructions

### Prerequisites
- .NET 8.0 or later  
- Input CSV files, a mapping file, and a relations file.

### Steps

1. **File Placement**  
   Place the required files in the following directories relative to the application's base directory:  
   - **CSV Files**: Place all input CSV files in the `CsvFiles` directory.  
   - **Mapping File**: Place `mapping.json` in the `CsvToJsonMappings` directory.  
   - **Relations File**: Place `relations.json` in the `CsvToJsonMappings` directory.  

2. **Output Location**  
   The processed **JSON output** will be generated in the root directory as `finalOutput.json`.  
   Additionally, **logs** (including warnings, errors, and information) will be saved in a CSV file named `logs.csv` in the root directory.

3. **Run the Application**  
   Execute the application. It will:
   - Automatically process the files placed in the specified directories.
   - Display progress updates in the console.
   - Write the output JSON and logs to the files as described above.

### Directory Structure:
```
CsvToJsonWithMapping/
├── CsvFiles/
│   └── example1.csv
│   └── example2.csv
├── CsvToJsonMappings/
│   └── mapping.json
│   └── relations.json
├── finalOutput.json
├── logs.csv
└── Program.cs
```

---