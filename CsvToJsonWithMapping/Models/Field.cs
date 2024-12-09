using System.ComponentModel.DataAnnotations;

namespace CsvToJsonWithMapping.Models
{
    public class Field
    {
        [Required(ErrorMessage = "CSVField is required")]
        public string CSVField { get; set; }
        [Required(ErrorMessage = "CSVFileName is required")]
        public string CSVFileName { get; set; }
    }
}
