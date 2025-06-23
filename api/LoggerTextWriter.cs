using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace FamilyBudgetApi
{
    // Redirects Console output to ILogger so that existing Console.WriteLine calls
    // are captured by the configured logging providers (e.g., Google Cloud Logging).
    public class LoggerTextWriter : TextWriter
    {
        private readonly ILogger _logger;
        private readonly LogLevel _level;

        public LoggerTextWriter(ILogger logger, LogLevel level)
        {
            _logger = logger;
            _level = level;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void WriteLine(string? value)
        {
            if (string.IsNullOrEmpty(value)) return;
            _logger.Log(_level, value);
        }
    }
}
