using NodaTime.TimeZones;
using System.Collections.Generic;
using System.Linq;

namespace Lombiq.OrchardCoreApiClient.Constants;

public static class Timezones
{
    public static IList<string> GetTimezoneIds()
    {
        var nodaTimezones = new List<string>();

        TzdbDateTimeZoneSource.Default
            .ZoneLocations
            .ToList()
            .ForEach(tz => nodaTimezones.Add(tz.ZoneId));

        return nodaTimezones;
    }
}
