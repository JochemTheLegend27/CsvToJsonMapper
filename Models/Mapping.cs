using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
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
