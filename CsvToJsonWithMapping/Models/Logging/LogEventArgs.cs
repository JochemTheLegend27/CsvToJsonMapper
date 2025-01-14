namespace CsvToJsonWithMapping.Models.Logging
{
    public class LogEventArgs : EventArgs
    {
        public string Type { get; set; }
        public string Message { get; set; }
    }
}
