using System;
using System.Collections.Generic;
using Google.Cloud.Firestore;

namespace FamilyBudgetApi.Models
{
    /// <summary>
    /// Converts Firestore Timestamp fields to yyyy-MM-dd strings and back.
    /// </summary>
    public class FirestoreDateStringConverter : IFirestoreConverter<string?>
    {
        public object? ToFirestore(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            return DateTime.TryParse(value, out var dt)
                ? Timestamp.FromDateTime(dt.ToUniversalTime())
                : null;
        }

        public string? FromFirestore(object value)
        {
            return value switch
            {
                Timestamp ts => ts.ToDateTime().ToString("yyyy-MM-dd"),
                Dictionary<string, object> map when map.TryGetValue("seconds", out var sec) &&
                                               map.TryGetValue("nanoseconds", out var nano) =>
                    DateTimeOffset
                        .FromUnixTimeSeconds(Convert.ToInt64(sec))
                        .AddTicks(Convert.ToInt64(nano) / 100)
                        .UtcDateTime
                        .ToString("yyyy-MM-dd"),
                string s => s,
                _ => null
            };
        }
    }
}
