using NodaTime.TimeZones;
using System.Collections.Generic;
using System.Linq;

namespace Lombiq.OrchardCoreApiClient.Constants;

public static class Timezones
{
    private static IList<string> _timezoneIds;
    public static IList<string> TimezoneIds => _timezoneIds ??= GetTimeZoneIds();

    public static IList<string> GetTimeZoneIds() => TzdbDateTimeZoneSource.Default
        .ZoneLocations
        .Select(timeZone => timeZone.ZoneId)
        .ToList();
}
