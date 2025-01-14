using CsvToJsonWithMapping.Models.Logging;

namespace CsvToJsonWithMapping.Services
{
    public class LoggingService
    {
        private readonly Dictionary<string, List<string>> _log = new();
        private double _percentageComplete;

        public event Action<string, string>? OnLogMessageAdded;

        public event Action<double>? OnProgressUpdated;

        public LoggingService()
        {
            LogPublisher.LogMessageEvent += HandleLogMessageEvent;
            LogPublisher.ProgressEvent += HandleProgressEvent;
        }

        private void HandleLogMessageEvent(object sender, LogEventArgs e)
        {
            Log(e.Type, e.Message);
        }

        private void HandleProgressEvent(object sender, ProgressEventArgs e)
        {
            LogProgressAsync(e.Current, e.Total).Wait();
        }

        private void Log(string type, string message)
        {
            if (!_log.ContainsKey(type))
            {
                _log[type] = new List<string>();
            }
            _log[type].Add(message);

            OnLogMessageAdded?.Invoke(type, message);
        }

        public Dictionary<string, List<string>> GetLogs()
        {
            return new Dictionary<string, List<string>>(_log);

        }

        public void ClearLogs()
        {
            _log.Clear();
            _percentageComplete = 0;
        }

        private async Task LogProgressAsync(int current, int total)
        {
            _percentageComplete = (double)current / total * 100;
            OnProgressUpdated?.Invoke(_percentageComplete);
        }

        public double GetProgress()
        {
            return _percentageComplete;
        }

        public void Reset()
        {
            _percentageComplete = 0;
            _log.Clear();
        }
    }
}