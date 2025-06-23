using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;

namespace FamilyBudgetApi.Logging;

public class LoggerTextWriter : TextWriter
{
    private readonly ILogger _logger;
    private readonly LogLevel _logLevel;
    private readonly StringBuilder _buffer = new StringBuilder();

    public LoggerTextWriter(ILogger logger, LogLevel logLevel)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logLevel = logLevel;
    }

    public override void Write(char value)
    {
        if (value == '\n' && _buffer.Length > 0)
        {
            Flush();
        }
        else
        {
            _buffer.Append(value);
        }
    }

    public override void Write(string? value)
    {
        if (value != null)
        {
            if (value.Contains("\n") && _buffer.Length > 0)
            {
                Flush();
            }
            _buffer.Append(value);
        }
    }

    public override void Flush()
    {
        if (_buffer.Length > 0)
        {
            _logger.Log(_logLevel, _buffer.ToString().Trim());
            _buffer.Clear();
        }
    }

    public override Encoding Encoding => Encoding.UTF8;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Flush();
        }
        base.Dispose(disposing);
    }
}