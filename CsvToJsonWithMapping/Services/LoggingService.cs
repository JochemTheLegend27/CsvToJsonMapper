using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvToJsonWithMapping.Services
{
    public class LoggingService
    {
        private readonly Dictionary<string, List<string>> _log = new();

        public void Log(string type, string message)
        {
            if (!_log.ContainsKey(type))
            {
                _log[type] = new List<string>();
            }

            _log[type].Add(message);
        }

        public Dictionary<string, List<string>> GetLogs()
        {
            return _log;
        }

        public void ClearLogs()
        {
            _log.Clear();
        }
    }
}
