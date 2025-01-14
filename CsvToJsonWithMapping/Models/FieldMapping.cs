using System.ComponentModel.DataAnnotations;

namespace CsvToJsonWithMapping.Models
{
    public class FieldMapping
    {
        public string CSVField { get; set; }
        public string CSVFile { get; set; }
        [Required(ErrorMessage = "JSONField is required")]
        public string JSONField { get; set; }
        public Validations Validations { get; set; } = new();
        public Dictionary<string, object>? ConversionRules { get; set; }
    }
}
