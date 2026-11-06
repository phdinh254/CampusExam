using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CampusExam.Infrastructure.Persistence;

internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => ToUtc(value),
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
{
    // Giờ Local được đổi sang UTC; Unspecified coi như đã là UTC.
    internal static DateTime ToUtc(DateTime value) =>
        value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}

internal sealed class UtcNullableDateTimeConverter() : ValueConverter<DateTime?, DateTime?>(
    value => value.HasValue ? UtcDateTimeConverter.ToUtc(value.Value) : value,
    value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value);
