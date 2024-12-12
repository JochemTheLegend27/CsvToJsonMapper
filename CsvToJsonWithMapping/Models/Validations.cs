namespace CsvToJsonWithMapping.Models
{
    public class Validations
    {
        public bool Required { get; set; } = false;
        public object? DefaultValue { get; set; } = null;
        public string Type { get; set; } = "String";
        // if fieldtype is a sting it is the min characters if it is a int its the min value
        public double? Min { get; set; } = null;
        // if fieldtype is a sting it is the max characters if it is a int its the max value
        public double? Max { get; set; } = null;

        public bool ValidationsNeedToPass { get; set; } = false;

    }
}
