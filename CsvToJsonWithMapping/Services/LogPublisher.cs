using CsvToJsonWithMapping.Models.Logging;

namespace CsvToJsonWithMapping.Services
{
    public static class LogPublisher
    {
        public static event EventHandler<LogEventArgs>? LogMessageEvent;
        public static event EventHandler<ProgressEventArgs>? ProgressEvent;

        public static void PublishLogMessage(string type, string message)
        {
            LogMessageEvent?.Invoke(null, new LogEventArgs { Type = type, Message = message });
        }

        public static void PublishProgress(int current, int total)
        {
            ProgressEvent?.Invoke(null, new ProgressEventArgs { Current = current, Total = total });
        }
    }
}
