using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Globalization;

namespace Personal.Mcp.Tools;

/// <summary>
/// Tools for date calculations and conversions to help with accurate date handling
/// in journal entries and other date-sensitive operations.
/// </summary>
[McpServerToolType]
public static class DateUtilityTools
{
    [McpServerTool, Description("Calculate an exact date from a relative description (e.g., 'last Wednesday', 'next Monday', 'two weeks ago'). Returns the date and day of the week for verification.")]
    public static object CalculateRelativeDate(
        TimeProvider timeProvider,
        [Description("The relative date description (e.g., 'last Wednesday', 'next Friday', 'yesterday', 'two weeks ago')")] string relativeDateDescription,
        [Description("Optional reference date in YYYY-MM-DD format. If not provided, uses today's date.")] string? referenceDate = null)
    {
        DateTimeOffset reference = timeProvider.GetLocalNow().Date;
        if (!string.IsNullOrWhiteSpace(referenceDate) && DateTimeOffset.TryParse(referenceDate, out var parsed))
        {
            reference = parsed.Date;
        }

        var description = relativeDateDescription.ToLowerInvariant().Trim();
        DateTimeOffset? calculatedDate = null;

        try
        {
            // Handle specific keywords
            if (description == "today")
            {
                calculatedDate = reference;
            }
            else if (description == "yesterday")
            {
                calculatedDate = reference.AddDays(-1);
            }
            else if (description == "tomorrow")
            {
                calculatedDate = reference.AddDays(1);
            }
            // Handle "last/previous [day of week]"
            else if (description.StartsWith("last ") || description.StartsWith("previous "))
            {
                var dayName = description.Replace("last ", "").Replace("previous ", "").Trim();
                calculatedDate = GetLastDayOfWeek(reference, dayName);
            }
            // Handle "next [day of week]"
            else if (description.StartsWith("next "))
            {
                var dayName = description.Replace("next ", "").Trim();
                calculatedDate = GetNextDayOfWeek(reference, dayName);
            }
            // Handle "X days ago"
            else if (description.Contains("days ago"))
            {
                var parts = description.Split(' ');
                if (parts.Length >= 2 && int.TryParse(parts[0], out var days))
                {
                    calculatedDate = reference.AddDays(-days);
                }
            }
            // Handle "X weeks ago"
            else if (description.Contains("weeks ago") || description.Contains("week ago"))
            {
                var parts = description.Split(' ');
                if (parts.Length >= 2 && int.TryParse(parts[0], out var weeks))
                {
                    calculatedDate = reference.AddDays(-weeks * 7);
                }
            }

            if (calculatedDate.HasValue)
            {
                return new
                {
                    success = true,
                    inputDescription = relativeDateDescription,
                    referenceDate = reference.ToString("yyyy-MM-dd"),
                    referenceDayOfWeek = reference.ToString("dddd"),
                    calculatedDate = calculatedDate.Value.ToString("yyyy-MM-dd"),
                    calculatedDayOfWeek = calculatedDate.Value.ToString("dddd"),
                    daysFromReference = (calculatedDate.Value - reference).Days,
                    isoWeek = ISOWeek.GetWeekOfYear(calculatedDate.Value.DateTime),
                    isoYear = ISOWeek.GetYear(calculatedDate.Value.DateTime)
                };
            }

            return new
            {
                success = false,
                error = $"Could not parse relative date description: '{relativeDateDescription}'",
                hint = "Try formats like 'last Wednesday', 'next Monday', 'yesterday', '2 days ago', 'three weeks ago'"
            };
        }
        catch (Exception ex)
        {
            return new
            {
                success = false,
                error = $"Error calculating date: {ex.Message}"
            };
        }
    }

    [McpServerTool, Description("Get information about a specific date including day of week, ISO week number, and relative position to today.")]
    public static object GetDateInfo(
        TimeProvider timeProvider,
        [Description("The date to get information about in YYYY-MM-DD format")] string date)
    {
        if (!DateTimeOffset.TryParse(date, out var targetDate))
        {
            return new
            {
                success = false,
                error = $"Invalid date format: '{date}'. Use YYYY-MM-DD format."
            };
        }

        var today = timeProvider.GetLocalNow().Date;
        var daysDifference = (targetDate.Date - today).Days;

        string relativeDescription;
        if (daysDifference == 0)
        {
            relativeDescription = "today";
        }
        else if (daysDifference == 1)
        {
            relativeDescription = "tomorrow";
        }
        else if (daysDifference == -1)
        {
            relativeDescription = "yesterday";
        }
        else if (daysDifference > 0)
        {
            relativeDescription = $"in {daysDifference} days";
        }
        else
        {
            relativeDescription = $"{Math.Abs(daysDifference)} days ago";
        }

        return new
        {
            success = true,
            date = targetDate.ToString("yyyy-MM-dd"),
            dayOfWeek = targetDate.ToString("dddd"),
            dayOfMonth = targetDate.Day,
            month = targetDate.ToString("MMMM"),
            year = targetDate.Year,
            isoWeek = ISOWeek.GetWeekOfYear(targetDate.DateTime),
            isoYear = ISOWeek.GetYear(targetDate.DateTime),
            dayOfYear = targetDate.DayOfYear,
            relativeToToday = relativeDescription,
            daysFromToday = daysDifference,
            today = today.ToString("yyyy-MM-dd")
        };
    }

    [McpServerTool, Description("Get all dates in the current week (or a specific week) with their day names. Useful for planning journal entries.")]
    public static object GetWeekDates(
        TimeProvider timeProvider,
        [Description("Optional date in YYYY-MM-DD format to get the week for. Defaults to current week.")] string? date = null)
    {
        DateTimeOffset targetDate = timeProvider.GetLocalNow().Date;
        if (!string.IsNullOrWhiteSpace(date) && DateTimeOffset.TryParse(date, out var parsed))
        {
            targetDate = parsed.Date;
        }

        var isoYear = ISOWeek.GetYear(targetDate.DateTime);
        var isoWeek = ISOWeek.GetWeekOfYear(targetDate.DateTime);

        // Get Monday of the ISO week
        var mondayDT = ISOWeek.ToDateTime(isoYear, isoWeek, DayOfWeek.Monday);
        var monday = new DateTimeOffset(mondayDT, timeProvider.GetLocalNow().Offset);

        var weekDates = new List<object>();
        for (int i = 0; i < 7; i++)
        {
            var day = monday.AddDays(i);
            weekDates.Add(new
            {
                date = day.ToString("yyyy-MM-dd"),
                dayOfWeek = day.ToString("dddd"),
                dayNumber = day.Day,
                isToday = day.Date == timeProvider.GetLocalNow().Date
            });
        }

        return new
        {
            success = true,
            isoWeek,
            isoYear,
            weekDescription = $"Week {isoWeek} of {isoYear}",
            dates = weekDates
        };
    }

    /// <summary>
    /// Get the most recent occurrence of a specific day of the week, going backwards from the reference date.
    /// </summary>
    private static DateTimeOffset GetLastDayOfWeek(DateTimeOffset reference, string dayName)
    {
        if (!Enum.TryParse<DayOfWeek>(dayName, ignoreCase: true, out var targetDay))
        {
            throw new ArgumentException($"Invalid day name: {dayName}");
        }

        // Start from yesterday and go backwards
        var date = reference.AddDays(-1);
        
        // Keep going back until we find the target day
        while (date.DayOfWeek != targetDay)
        {
            date = date.AddDays(-1);
        }

        return date;
    }

    /// <summary>
    /// Get the next occurrence of a specific day of the week, going forwards from the reference date.
    /// </summary>
    private static DateTimeOffset GetNextDayOfWeek(DateTimeOffset reference, string dayName)
    {
        if (!Enum.TryParse<DayOfWeek>(dayName, ignoreCase: true, out var targetDay))
        {
            throw new ArgumentException($"Invalid day name: {dayName}");
        }

        // Start from tomorrow and go forwards
        var date = reference.AddDays(1);
        
        // Keep going forward until we find the target day
        while (date.DayOfWeek != targetDay)
        {
            date = date.AddDays(1);
        }

        return date;
    }
}
