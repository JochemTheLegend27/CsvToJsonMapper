namespace CsvToJsonWithMapping.Services
{
    public static class LogPublisher
    {
        // Declare the events
        public static event EventHandler<LogEventArgs>? LogMessageEvent;
        public static event EventHandler<ProgressEventArgs>? ProgressEvent;

        // Method to publish log message event
        public static void PublishLogMessage(string type, string message)
        {
            LogMessageEvent?.Invoke(null, new LogEventArgs { Type = type, Message = message });
        }

        // Method to publish progress event
        public static void PublishProgress(int current, int total)
        {
            ProgressEvent?.Invoke(null, new ProgressEventArgs { Current = current, Total = total });
        }
    }

    // Custom EventArgs for Log messages
    public class LogEventArgs : EventArgs
    {
        public string Type { get; set; }
        public string Message { get; set; }
    }

    // Custom EventArgs for Progress updates
    public class ProgressEventArgs : EventArgs
    {
        public int Current { get; set; }
        public int Total { get; set; }
    }
}
