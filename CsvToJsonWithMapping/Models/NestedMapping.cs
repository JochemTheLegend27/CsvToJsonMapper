using System.ComponentModel.DataAnnotations;

namespace CsvToJsonWithMapping.Models
{
    public class NestedMapping
    {
        [Required(ErrorMessage = "JSONNestedFieldName is required")]
        public string JSONNestedFieldName { get; set; }
        [Required(ErrorMessage = "JSONNestedType is required")]
        public string JSONNestedType { get; set; }
        public List<FieldMapping> Fields { get; set; } = new();
        public List<NestedMapping> NestedFields { get; set; } = new();
    }
}
