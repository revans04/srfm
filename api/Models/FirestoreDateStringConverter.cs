using System;
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
                string s => s,
                _ => null
            };
        }
    }
}
