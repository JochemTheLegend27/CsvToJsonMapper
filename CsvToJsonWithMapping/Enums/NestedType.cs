using System.Runtime.Serialization;

namespace CsvToJsonWithMapping.Enums
{
    public enum NestedType
    {
        [EnumMember(Value = "Object")]
        Object,
        [EnumMember(Value = "Array")]
        Array
    }
}
