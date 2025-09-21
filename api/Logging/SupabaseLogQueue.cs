using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FamilyBudgetApi.Logging;

/// <summary>
/// Bounded channel used to decouple request thread logging from database I/O.
/// </summary>
public class SupabaseLogQueue
{
    private readonly Channel<SupabaseLogEntry> _channel;

    public SupabaseLogQueue(int capacity = 1024)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        };
        _channel = Channel.CreateBounded<SupabaseLogEntry>(options);
    }

    public bool TryWrite(SupabaseLogEntry entry) => _channel.Writer.TryWrite(entry);

    public ValueTask WriteAsync(SupabaseLogEntry entry, CancellationToken cancellationToken) =>
        _channel.Writer.WriteAsync(entry, cancellationToken);

    public ValueTask<SupabaseLogEntry> ReadAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAsync(cancellationToken);

    public bool TryRead(out SupabaseLogEntry entry) => _channel.Reader.TryRead(out entry);

    public bool HasPendingEntries => !_channel.Reader.Completion.IsCompleted;

    public ChannelReader<SupabaseLogEntry> Reader => _channel.Reader;
}
