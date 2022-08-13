using NodaTime.TimeZones;
using System.Collections.Generic;
using System.Linq;

namespace Lombiq.OrchardCoreApiClient.Constants;

public static class Timezones
{
    public static IList<string> TimezoneIds => GetTimeZoneIds();

    public static IList<string> GetTimeZoneIds() => TzdbDateTimeZoneSource.Default
        .ZoneLocations
        .Select(timeZone => timeZone.ZoneId)
        .ToList();
}
