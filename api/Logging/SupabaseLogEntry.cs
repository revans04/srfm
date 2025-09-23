using System;

namespace FamilyBudgetApi.Logging;

/// <summary>
/// Represents a single log entry destined for the Supabase/PostgreSQL sink.
/// </summary>
public sealed record SupabaseLogEntry(
    DateTime TimestampUtc,
    string Level,
    string Category,
    string Message,
    string? Exception);

