using System.Text;

namespace Laundry.Patterns.Singleton
{
    public enum EventLevel  // Changed from LogLevel to EventLevel
    {
        Info,
        Warning,
        Error,
        Critical
    }

    public sealed class OrderLogger
    {
        private static readonly Lazy<OrderLogger> _instance = new(() => new OrderLogger());
        private readonly List<LogEntry> _logs = new();
        private readonly object _lock = new();
        private readonly string _logFilePath;

        private OrderLogger()
        {
            _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", $"order_log_{DateTime.Now:yyyyMMdd}.txt");
            var logDir = Path.GetDirectoryName(_logFilePath);
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);
        }

        public static OrderLogger Instance => _instance.Value;

        public void LogEvent(string message, EventLevel level = EventLevel.Info)  // Changed to EventLevel
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Message = message,
                Level = level,
                UserId = GetCurrentUserId()
            };

            lock (_lock)
            {
                _logs.Add(entry);
                SaveToFile(entry);
            }

            // Console output for debugging
            Console.WriteLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{level}] - {message}");
        }

        private void SaveToFile(LogEntry entry)
        {
            try
            {
                File.AppendAllText(_logFilePath, $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss}|{entry.Level}|{entry.Message}|User:{entry.UserId}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write log: {ex.Message}");
            }
        }

        private static string GetCurrentUserId()
        {
            // Get from HttpContext if available
            return "System";
        }

        public List<LogEntry> GetLogs(DateTime? from = null, DateTime? to = null, EventLevel? level = null)  // Changed to EventLevel
        {
            lock (_lock)
            {
                var query = _logs.AsEnumerable();
                if (from.HasValue) query = query.Where(l => l.Timestamp >= from.Value);
                if (to.HasValue) query = query.Where(l => l.Timestamp <= to.Value);
                if (level.HasValue) query = query.Where(l => l.Level == level.Value);
                return query.ToList();
            }
        }

        public string GetLogsAsString(DateTime? from = null, DateTime? to = null)
        {
            var logs = GetLogs(from, to);
            var sb = new StringBuilder();
            foreach (var log in logs)
            {
                sb.AppendLine($"[{log.Timestamp:yyyy-MM-dd HH:mm:ss}] [{log.Level}] - {log.Message}");
            }
            return sb.ToString();
        }

        public List<LogEntry> GetLogEntries()
        {
            lock (_lock)
            {
                return _logs.OrderByDescending(l => l.Timestamp).ToList();
            }
        }

        public List<LogEntry> GetRecentLogs(int count = 20)
        {
            lock (_lock)
            {
                return _logs.OrderByDescending(l => l.Timestamp).Take(count).ToList();
            }
        }

        public void ClearLogs()
        {
            lock (_lock)
            {
                _logs.Clear();

                // Also clear the log file
                try
                {
                    if (File.Exists(_logFilePath))
                        File.WriteAllText(_logFilePath, string.Empty);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to clear log file: {ex.Message}");
                }

                LogEvent("Logs cleared by administrator", EventLevel.Info);
            }
        }

        public List<LogEntry> SearchLogs(string searchTerm)
        {
            lock (_lock)
            {
                return _logs.Where(l => l.Message.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                            .OrderByDescending(l => l.Timestamp)
                            .ToList();
            }
        }

        public Dictionary<EventLevel, int> GetLogStatistics()
        {
            lock (_lock)
            {
                return _logs.GroupBy(l => l.Level)
                            .ToDictionary(g => g.Key, g => g.Count());
            }
        }
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; } = string.Empty;
        public EventLevel Level { get; set; }  // Changed to EventLevel
        public string UserId { get; set; } = string.Empty;
    }
}