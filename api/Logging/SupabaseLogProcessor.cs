using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Npgsql;
using FamilyBudgetApi.Services;

namespace FamilyBudgetApi.Logging;

/// <summary>
/// Background worker that drains the log queue and persists entries in batches.
/// </summary>
public class SupabaseLogProcessor : BackgroundService
{
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(1);
    private const int BatchSize = 50;

    private readonly SupabaseLogQueue _queue;
    private readonly SupabaseDbService _dbService;

    public SupabaseLogProcessor(SupabaseLogQueue queue, SupabaseDbService dbService)
    {
        _queue = queue;
        _dbService = dbService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batch = new List<SupabaseLogEntry>(BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var entry = await _queue.ReadAsync(stoppingToken);
                batch.Add(entry);

                var flushDeadline = DateTime.UtcNow + FlushInterval;

                while (batch.Count < BatchSize)
                {
                    if (_queue.TryRead(out var additional))
                    {
                        batch.Add(additional);
                        continue;
                    }

                    var delay = flushDeadline - DateTime.UtcNow;
                    if (delay <= TimeSpan.Zero)
                    {
                        break;
                    }

                    var delayTask = Task.Delay(delay, stoppingToken);
                    var readTask = _queue.ReadAsync(stoppingToken).AsTask();
                    var completed = await Task.WhenAny(delayTask, readTask);

                    if (completed == readTask)
                    {
                        batch.Add(readTask.Result);
                        flushDeadline = DateTime.UtcNow + FlushInterval;
                    }
                    else
                    {
                        await delayTask; // observe cancellation if any
                        break;
                    }
                }

                await WriteBatchAsync(batch, stoppingToken);
                batch.Clear();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SupabaseLogProcessor] Error writing logs: {ex.Message}");
                DumpToConsole(batch);
                batch.Clear();
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        await FlushRemainingAsync(batch);
    }

    private async Task WriteBatchAsync(List<SupabaseLogEntry> entries, CancellationToken cancellationToken)
    {
        if (entries.Count == 0)
            return;

        await using var conn = await _dbService.GetOpenConnectionAsync(cancellationToken);
        await using var batch = new NpgsqlBatch(conn);

        foreach (var entry in entries)
        {
            var cmd = new NpgsqlBatchCommand("INSERT INTO logs (timestamp, level, category, message, exception) VALUES (@timestamp, @level, @category, @message, @exception)");
            cmd.Parameters.AddWithValue("@timestamp", entry.TimestampUtc);
            cmd.Parameters.AddWithValue("@level", entry.Level);
            cmd.Parameters.AddWithValue("@category", entry.Category);
            cmd.Parameters.AddWithValue("@message", entry.Message);
            cmd.Parameters.AddWithValue("@exception", (object?)entry.Exception ?? DBNull.Value);
            batch.BatchCommands.Add(cmd);
        }

        try
        {
            await batch.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01")
        {
            Console.WriteLine("[SupabaseLogProcessor] logs table missing; writing entries to console.");
            DumpToConsole(entries);
        }
    }

    private static void DumpToConsole(IReadOnlyCollection<SupabaseLogEntry> entries)
    {
        if (entries.Count == 0)
            return;

        foreach (var entry in entries)
        {
            var line = $"[{entry.TimestampUtc:o}] {entry.Level} {entry.Category}: {entry.Message}";
            if (!string.IsNullOrEmpty(entry.Exception))
            {
                line += $"\n{entry.Exception}";
            }
            Console.WriteLine(line);
        }
    }

    private async Task FlushRemainingAsync(List<SupabaseLogEntry> batch)
    {
        while (_queue.TryRead(out var entry))
        {
            batch.Add(entry);
            if (batch.Count >= BatchSize)
            {
                try
                {
                    await WriteBatchAsync(batch, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SupabaseLogProcessor] Error flushing remaining logs: {ex.Message}");
                    DumpToConsole(batch);
                }
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            try
            {
                await WriteBatchAsync(batch, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SupabaseLogProcessor] Error flushing remaining logs: {ex.Message}");
                DumpToConsole(batch);
            }
        }
    }
}
