using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvToJsonWithMapping.Services
{
    public static class LoggingService
    {
        private static readonly Dictionary<string, List<string>> _log = new();

        public static void Log(string type, string message)
        {
            if (!_log.ContainsKey(type))
            {
                _log[type] = new List<string>();
            }

            _log[type].Add(message);
        }

        public static Dictionary<string, List<string>> GetLogs()
        {
            return _log;
        }

        public static void ClearLogs()
        {
            _log.Clear();
        }

        public static void LogError(string category, string message)
        {
            Log($"{category} - Error", message);
        }

        public static void LogWarning(string category, string message)
        {
            Log($"{category} - Warning", message);
        }

        public static void DisplayLogs()
        {
            foreach (var logEntry in _log)
            {
                Console.WriteLine($"{logEntry.Key}:");
                foreach (var message in logEntry.Value)
                {
                    Console.WriteLine($" - {message}");
                }
            }
        }


    }
}
