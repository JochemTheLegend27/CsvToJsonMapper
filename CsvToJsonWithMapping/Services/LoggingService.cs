namespace CsvToJsonWithMapping.Services
{
    public class LoggingService
    {
        private readonly Dictionary<string, List<string>> _log = new();
        private double _percentageComplete;

        // Event for when a log message is added
        public event Action<string, string>? OnLogMessageAdded;

        // Event for when progress is updated
        public event Action<double>? OnProgressUpdated;

        public LoggingService()
        {
            // Subscribe to the events of interest
            LogPublisher.LogMessageEvent += HandleLogMessageEvent;
            LogPublisher.ProgressEvent += HandleProgressEvent;
        }

        private void HandleLogMessageEvent(object sender, LogEventArgs e)
        {
            Log(e.Type, e.Message);
        }

        // Handle progress update event
        private void HandleProgressEvent(object sender, ProgressEventArgs e)
        {
            LogProgressAsync(e.Current, e.Total).Wait();
        }

        // Method to add log message and trigger the event
        private void Log(string type, string message)
        {
            // Thread-safe log message addition

            if (!_log.ContainsKey(type))
            {
                _log[type] = new List<string>();
            }
            _log[type].Add(message);


            // Trigger the event asynchronously so that listeners (UI or other) can react
            OnLogMessageAdded?.Invoke(type, message);
        }

        // Method to log an error message

        // Method to return all logs
        public Dictionary<string, List<string>> GetLogs()
        {
            return new Dictionary<string, List<string>>(_log);

        }

        // Clear logs and reset progress
        public void ClearLogs()
        {
            _log.Clear();
            _percentageComplete = 0;
        }

        // Method to update progress and trigger the progress event
        private async Task LogProgressAsync(int current, int total)
        {
            _percentageComplete = (double)current / total * 100;
            OnProgressUpdated?.Invoke(_percentageComplete);
        }

        // Get current progress
        public double GetProgress()
        {
            return _percentageComplete;
        }

        // Reset logs and progress
        public void Reset()
        {
            _percentageComplete = 0;
            _log.Clear();
        }
    }
}