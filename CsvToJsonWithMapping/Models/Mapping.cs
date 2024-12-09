using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CsvToJsonWithMapping.Models
{
    public class Mapping
    {
        public List<FieldMapping> Fields { get; set; } = new();
        public List<NestedMapping> NestedFields { get; set; } = new();
    }

    public class FieldMapping
    {
        public string CSVField { get; set; }
        public string CSVFile { get; set; }
        [Required(ErrorMessage = "JSONField is required")]
        public string JSONField { get; set; }
        public Validations Validations { get; set; } = new();
        public Dictionary<string, object>? ConversionRules { get; set; }
    }

    public class Validations
    {
        public bool Required { get; set; } = false;
        public object? DefaultValue { get; set; } = null;
        public string Type { get; set; } = "String";
        // if fieldtype is a sting its the min characters if its a int its the min value
        public double? Min { get; set; } = null;
        // if fieldtype is a sting its the max characters if its a int its the max value
        public double? Max { get; set; } = null;

        public bool ValidationsNeedToPass = false;

    }

    public enum NestedType
    {
        [EnumMember(Value = "Object")]
        Object,
        [EnumMember(Value = "Array")]
        Array
    }

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
