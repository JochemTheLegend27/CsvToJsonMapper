namespace CsvToJsonWithMapping.Models
{
    public class Validations
    {
        public bool Required { get; set; } = false;
        public object? DefaultValue { get; set; } = null;
        public string Type { get; set; } = "String";
        public double? Min { get; set; } = null;
        public double? Max { get; set; } = null;

        public bool ValidationsNeedToPass { get; set; } = false;

    }
}
