using System.Globalization;

namespace Chinnaphat_8272_L005.Helpers;

/// <summary>ISO dates for HTML/API exchange; display dates use the Thai Buddhist calendar.</summary>
public static class DateFormatHelper
{
    private static readonly CultureInfo IsoCulture = CultureInfo.InvariantCulture;

    public static string ToIsoDate(DateTime date) => date.ToString("yyyy-MM-dd", IsoCulture);

    public static string ToIsoDate(DateOnly date) => date.ToString("yyyy-MM-dd", IsoCulture);

    public static bool TryParseIsoDate(string? value, out DateOnly date) =>
        DateOnly.TryParseExact(value?.Trim(), "yyyy-MM-dd", IsoCulture, DateTimeStyles.None, out date);

    public static DateTime ToIsoDateTime(string value) =>
        DateTime.ParseExact(value.Trim(), "yyyy-MM-dd", IsoCulture, DateTimeStyles.None);
}
