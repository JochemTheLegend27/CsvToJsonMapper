namespace CsvToJsonWithMapping.Models.Logging
{
    public class ProgressEventArgs : EventArgs
    {
        public int Current { get; set; }
        public int Total { get; set; }
    }
}
