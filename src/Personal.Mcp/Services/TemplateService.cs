using System.Globalization;
using System.Text;
using Fluid;
using Fluid.Values;
using Microsoft.Extensions.Logging;

namespace Personal.Mcp.Services;

/// <summary>
/// Service for rendering Liquid templates with context data.
/// Uses the Fluid library for Liquid template processing.
/// </summary>
public sealed class TemplateService : ITemplateService
{
    private readonly ILogger<TemplateService>? _logger;
    private readonly FluidParser _parser;
    private static readonly TemplateOptions _templateOptions;

    // Default templates
    private const string DefaultJournalTemplate = """
---
tags:
  - "journal/weekly/{{journal_day | iso_date: '%Y-W%V'}}"
notetype: weekly
noteVersion: 5
category: weekly
created: {{journal_day | iso_date: '%Y-%m-%d'}}T{{journal_day | iso_date: '%H:%M'}}
---
# Week {{journal_day | iso_date: '%V'}} in {{journal_day | iso_date: '%Y'}}

{{journal_day | iso_date: "%Y"}}

[[{{journal_day | iso_date: "%s" | minus : 604800 |  iso_date: '%Y-W%V'}}|↶ Previous]] ⋮ [[{{journal_day | iso_date: "%Y"}}|{{journal_day | iso_date: "%Y"}}]] › [[{{journal_day | iso_date: "%Y-%m"}}|{{journal_day | iso_date: "%b"}}]] › [[{{journal_day | iso_date: '%Y-W%V'}}|{{journal_day | iso_date: 'W%V'}}]] ⋮ [[{{journal_day | iso_date: "%s" | plus : 604800 |  iso_date: '%Y-W%V'}}|Following ↷]]

---

## Tasks This Week
- [ ] Task

## {{monday_iso_week | iso_date: "%e"}} {{monday_iso_week | iso_date: "%A"}}

""";

    private const string DefaultNoteTemplate = @"---
title: {{title}}
created: {{created_date | iso_date: '%Y-%m-%d'}}
tags: []
---

# {{title}}

";

    static TemplateService()
    {
        // Configure template options to be more permissive and user-friendly
        _templateOptions = new TemplateOptions
        {
            MemberAccessStrategy = new UnsafeMemberAccessStrategy(),
            MaxSteps = 10000 // Prevent infinite loops
        };

        _templateOptions.Filters.AddFilter("iso_date", static (input, arguments, context) =>
        {
            if (!input.TryGetDateTimeInput(context, out var value))
            {
                return NilValue.Instance;
            }

            if (arguments.At(0).IsNil())
            {
                return new DateTimeValue(value);
            }

            var format = arguments.At(0).ToStringValue();
            var result = new StringBuilder(64);
            ForStrf(value, format, result, context);

            return new StringValue(result.ToString());

        });
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateService"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    public TemplateService(ILogger<TemplateService>? logger = null)
    {
        _logger = logger;
        _parser = new FluidParser();
    }

    /// <inheritdoc />
    public string RenderTemplate(string template, Dictionary<string, object> context)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            _logger?.LogWarning("Attempted to render empty or null template");
            return string.Empty;
        }

        try
        {
            // Parse the template
            if (!_parser.TryParse(template, out var parsedTemplate, out var error))
            {
                var errorMsg = $"Failed to parse template: {error}";
                _logger?.LogError(errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            // Create template context with the provided data
            var templateContext = new TemplateContext(context, _templateOptions);

            // Render the template
            var result = parsedTemplate.Render(templateContext);
            _logger?.LogDebug("Successfully rendered template with {ContextCount} context values", context.Count);
            return result;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var errorMsg = $"Failed to render template: {ex.Message}";
            _logger?.LogError(ex, "Template rendering failed");
            throw new InvalidOperationException(errorMsg, ex);
        }
    }

    /// <inheritdoc />
    public bool ValidateTemplate(string template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return false;
        }

        try
        {
            return _parser.TryParse(template, out _, out var error);
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Template validation failed with exception");
            return false;
        }
    }

    /// <inheritdoc />
    public string GetDefaultJournalTemplate()
    {
        return DefaultJournalTemplate;
    }

    /// <inheritdoc />
    public string GetDefaultNoteTemplate()
    {
        return DefaultNoteTemplate;
    }

    /// <inheritdoc />
    public void SetCultureInfo(CultureInfo cultureInfo)
    {
        _templateOptions.CultureInfo = cultureInfo;
    }

    /// <inheritdoc />
    public void SetTimeZone(TimeZoneInfo timeZoneInfo)
    {
        _templateOptions.TimeZone = timeZoneInfo;
    }

    private static void ForStrf(DateTimeOffset value, string format, StringBuilder result, TemplateContext context)
    {
        var percent = false;

        var removeLeadingZerosFlag = false;
        var useSpaceForPaddingFlag = false;
        var upperCaseFlag = false;
        var useColonsForZeeDirectiveFlag = false;
        int? width = null;

        for (var i = 0; i < format.Length; i++)
        {
            var c = format[i];
            if (!percent)
            {
                if (c == '%')
                {
                    percent = true;
                }
                else
                {
                    result.Append(c);
                }
            }
            else
            {
                // Zero or more flags (each is a character).
                switch (c)
                {
                    case '^': upperCaseFlag = true; continue;
                    case '-': removeLeadingZerosFlag = true; continue;
                    case '_': useSpaceForPaddingFlag = true; continue;
                    case ':': useColonsForZeeDirectiveFlag = true; continue;
                    default: break;
                }

                // An optional width specifier (an integer).

                if (char.IsDigit(c))
                {
                    width ??= 0;
                    width = width * 10 + (c - '0');
                    continue;
                }

                switch (c)
                {
                    case 'a':
                        string AbbreviatedDayName()
                        {
                            return context.CultureInfo.DateTimeFormat.AbbreviatedDayNames[(int)value.DayOfWeek];
                        }

                        var abbreviatedDayName = AbbreviatedDayName();
                        result.Append(upperCaseFlag ? abbreviatedDayName.ToUpper(context.CultureInfo) : abbreviatedDayName);
                        break;
                    case 'A':
                        {
                            var dayName = context.CultureInfo.DateTimeFormat.DayNames[(int)value.DayOfWeek];
                            result.Append(upperCaseFlag ? dayName.ToUpper(context.CultureInfo) : dayName);
                            break;
                        }
                    case 'b':
                        var abbreviatedMonthName = context.CultureInfo.DateTimeFormat.AbbreviatedMonthNames[value.Month - 1];
                        result.Append(upperCaseFlag ? abbreviatedMonthName.ToUpper(context.CultureInfo) : abbreviatedMonthName);
                        break;
                    case 'B':
                        {
                            var monthName = context.CultureInfo.DateTimeFormat.MonthNames[value.Month - 1];
                            result.Append(upperCaseFlag ? monthName.ToUpper(context.CultureInfo) : monthName);
                            break;
                        }
                    case 'c':
                        {
                            // c is defined as "%a %b %e %T %Y" but it's also supposed to be locale aware, so we are using the 
                            // C# standard format instead
                            result.Append(upperCaseFlag ? value.ToString("F", context.CultureInfo).ToUpper(context.CultureInfo) : value.ToString("F", context.CultureInfo));
                            break;
                        }
                    case 'C': result.Append(Format(value.Year / 100, 2)); break;
                    case 'd': result.Append(Format(value.Day, 2)); break;
                    case 'D':
                        {
                            var sb = new StringBuilder();
                            ForStrf(value, "%m/%d/%y", sb, context);
                            result.Append(upperCaseFlag ? sb.ToString().ToUpper(context.CultureInfo) : sb.ToString());
                            break;
                        }
                    case 'e':
                        useSpaceForPaddingFlag = true; result.Append(Format(value.Day, 2));
                        break;
                    case 'F':
                        {
                            var sb = new StringBuilder();
                            ForStrf(value, "%Y-%m-%d", sb, context);
                            result.Append(upperCaseFlag ? sb.ToString().ToUpper(context.CultureInfo) : sb.ToString());
                            break;
                        }
                    case 'g':
                        {
                            result.Append(Format(ISOWeek.GetYear(value.DateTime) % 100));
                            break;
                        }
                    case 'G':
                        {
                            result.Append(Format(ISOWeek.GetYear(value.DateTime)));
                            break;
                        }
                    case 'h':
                        ForStrf(value, "%b", result, context);
                        break;
                    case 'H':
                        result.Append(value.ToString("HH"));
                        break;
                    case 'I':
                        {
                            var hour = value.Hour switch
                            {
                                0 => 12,
                                <= 12 => value.Hour,
                                _ => value.Hour - 12
                            };

                            result.Append(Format(hour, 2));
                            break;
                        }
                    case 'j': result.Append(Format(value.DayOfYear, 3)); break;
                    case 'k': result.Append(value.Hour); break;
                    case 'l':
                        {
                            useSpaceForPaddingFlag = true;
                            var hour = value.Hour switch
                            {
                                0 => 12,
                                <= 12 => value.Hour,
                                _ => value.Hour - 12
                            };

                            result.Append(Format(hour, 2));
                            break;
                        }
                    case 'L': result.Append(Format(value.Millisecond, 3)); break;
                    case 'm': result.Append(Format(value.Month, 2)); break;
                    case 'M':
                        result.Append(Format(value.Minute, 2));
                        break;
                    case 'n': result.Append(new String('\n', width ?? 1)); break;
                    case 'N':
                        width ??= 9;
                        var v = (value.Ticks % 10000000).ToString(context.CultureInfo);
                        result.Append(v.Length >= width ? v.Substring(0, width.Value) : v.PadRight(width.Value, '0'));
                        break;
                    case 'p': result.Append(value.ToString("tt", context.CultureInfo).ToUpper(context.CultureInfo)); break;
                    case 'P': result.Append(value.ToString("tt", context.CultureInfo).ToLower(context.CultureInfo)); break;
                    case 'r':
                        {
                            var sb = new StringBuilder();
                            ForStrf(value, "%I:%M:%S %p", sb, context);
                            result.Append(upperCaseFlag ? sb.ToString().ToUpper(context.CultureInfo) : sb.ToString());
                            break;
                        }
                    case 'R':
                        {
                            var sb = new StringBuilder();
                            ForStrf(value, "%H:%M", sb, context);
                            result.Append(upperCaseFlag ? sb.ToString().ToUpper(context.CultureInfo) : sb.ToString());
                            break;
                        }
                    case 's': result.Append(Format(value.ToUnixTimeSeconds())); break;
                    case 'S':
                        result.Append(Format(value.Second, 2));
                        break;
                    case 't': result.Append(new String('\t', width ?? 1)); break;
                    case 'T':
                        {
                            var sb = new StringBuilder();
                            ForStrf(value, "%H:%M:%S", sb, context);
                            result.Append(upperCaseFlag ? sb.ToString().ToUpper(context.CultureInfo) : sb.ToString());
                            break;
                        }
                    case 'u': result.Append(value.DayOfWeek switch { DayOfWeek.Sunday => 7, _ => (int)value.DayOfWeek }); break;
                    case 'U':
                        {
                            var week = context.CultureInfo.Calendar.GetWeekOfYear(value.DateTime, CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday);
                            if (week >= 52 && value.DateTime.Month == 1)
                            {
                                week = 0;
                            }
                            result.Append(Format(week, 2));
                            break;
                        }
                    case 'v':
                        {
                            var sb = new StringBuilder();
                            ForStrf(value, "%e-%b-%Y", sb, context);
                            result.Append(upperCaseFlag ? sb.ToString().ToUpper(context.CultureInfo) : sb.ToString());
                            break;
                        }
                    case 'V':
                        {
                            result.Append(Format(ISOWeek.GetWeekOfYear(value.DateTime), 2));
                            break;
                        }
                    case 'w': result.Append(((int)value.DayOfWeek).ToString(context.CultureInfo)); break;
                    case 'W':
                        {
                            var week = context.CultureInfo.Calendar.GetWeekOfYear(value.DateTime, CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday);
                            if (week >= 52 && value.DateTime.Month == 1)
                            {
                                week = 0;
                            }
                            result.Append(Format(week, 2));
                            break;
                        }
                    case 'x':
                        {
                            // x is defined as "%m/%d/%y" but it's also supposed to be locale aware, so we are using the 
                            // C# short date pattern standard format instead

                            result.Append(upperCaseFlag ? value.ToString("d", context.CultureInfo).ToUpper(context.CultureInfo) : value.ToString("d", context.CultureInfo));
                            break;
                        }
                    case 'X':
                        {
                            // X is defined as "%T" but it's also supposed to be locale aware, so we are using the 
                            // C# short time pattern standard format instead

                            result.Append(upperCaseFlag ? value.ToString("t", context.CultureInfo).ToUpper(context.CultureInfo) : value.ToString("t", context.CultureInfo));
                            break;
                        }
                    case 'y':
                        result.Append(Format(value.Year % 100, 2));
                        break;
                    case 'Y':
                        result.Append(Format(value.Year, 4));
                        break;
                    case 'z':
                        {
                            var zzz = value.ToString("zzz", context.CultureInfo);
                            result.Append(useColonsForZeeDirectiveFlag ? zzz : zzz.Replace(":", ""));
                            break;
                        }
                    case 'Z':
                        result.Append(value.ToString("zzz", context.CultureInfo));
                        break;
                    case '%': result.Append('%'); break;
                    case '+':
                        {
                            var sb = new StringBuilder();
                            ForStrf(value, "%a %b %e %H:%M:%S %Z %Y", sb, context);
                            result.Append(upperCaseFlag ? sb.ToString().ToUpper(context.CultureInfo) : sb.ToString());
                            break;
                        }
                    default: result.Append('%').Append(c); break;
                }

                percent = false;
                removeLeadingZerosFlag = false;
                useSpaceForPaddingFlag = false;
                upperCaseFlag = false;
                useColonsForZeeDirectiveFlag = false;
                width = null;

                string Format(long value, int defaultWidth = 0)
                {
                    var stringValue = value.ToString(context.CultureInfo);

                    if (removeLeadingZerosFlag)
                    {
                        return stringValue.TrimStart('0');
                    }

                    return stringValue.PadLeft(width == null ? defaultWidth : width.Value, useSpaceForPaddingFlag ? ' ' : '0');
                }
            }
        }
    }

}