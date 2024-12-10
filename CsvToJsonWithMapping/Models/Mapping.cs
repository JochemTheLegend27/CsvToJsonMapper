namespace CsvToJsonWithMapping.Models
{
    public class Mapping
    {
        public List<FieldMapping> Fields { get; set; } = new();
        public List<NestedMapping> NestedFields { get; set; } = new();
    }
}
