using Personal.Mcp.Tools;
using System.Globalization;
using System.Text.Json;

namespace Personal.Mcp.Tests.Tools;

public class DateUtilityToolsTests
{
    // Helper method to extract properties from anonymous objects returned by tools
    private static JsonElement GetResultAsJson(object result)
    {
        var json = JsonSerializer.Serialize(result);
        return JsonDocument.Parse(json).RootElement;
    }

    public class CalculateRelativeDateTests
    {
        [Theory]
        [InlineData("2025-10-19", "last Wednesday", "2025-10-15", "Wednesday")] // THIS IS THE KEY TEST - Sunday to last Wednesday
        [InlineData("2025-10-19", "last Monday", "2025-10-13", "Monday")]
        [InlineData("2025-10-19", "last Thursday", "2025-10-16", "Thursday")]
        [InlineData("2025-10-15", "last Wednesday", "2025-10-08", "Wednesday")] // Wednesday to previous Wednesday  
        [InlineData("2025-10-19", "next Monday", "2025-10-20", "Monday")]
        [InlineData("2025-10-19", "next Wednesday", "2025-10-22", "Wednesday")]
        public void CalculateRelativeDate_WithDayOfWeek_ReturnsCorrectDate(
            string referenceDate, string description, string expectedDate, string expectedDayOfWeek)
        {
            // Act
            var result = DateUtilityTools.CalculateRelativeDate(description, referenceDate);
            var json = GetResultAsJson(result);

            // Assert
            json.GetProperty("success").GetBoolean().Should().BeTrue();
            json.GetProperty("calculatedDate").GetString().Should().Be(expectedDate);
            json.GetProperty("calculatedDayOfWeek").GetString().Should().Be(expectedDayOfWeek);
        }

        [Theory]
        [InlineData("2025-10-19", "yesterday", "2025-10-18")]
        [InlineData("2025-10-19", "today", "2025-10-19")]
        [InlineData("2025-10-19", "tomorrow", "2025-10-20")]
        public void CalculateRelativeDate_WithSimpleKeywords_ReturnsCorrectDate(
            string referenceDate, string description, string expectedDate)
        {
            // Act
            var result = DateUtilityTools.CalculateRelativeDate(description, referenceDate);
            var json = GetResultAsJson(result);

            // Assert
            json.GetProperty("success").GetBoolean().Should().BeTrue();
            json.GetProperty("calculatedDate").GetString().Should().Be(expectedDate);
        }

        [Fact]
        public void CalculateRelativeDate_WithInvalidDescription_ReturnsError()
        {
            // Act
            var result = DateUtilityTools.CalculateRelativeDate("invalid description", "2025-10-19");
            var json = GetResultAsJson(result);

            // Assert
            json.GetProperty("success").GetBoolean().Should().BeFalse();
            json.GetProperty("error").GetString().Should().Contain("Could not parse");
        }
    }

    public class GetDateInfoTests
    {
        [Theory]
        [InlineData("2025-10-15", "Wednesday", 42, 2025)]
        [InlineData("2025-10-19", "Sunday", 42, 2025)]
        public void GetDateInfo_WithValidDate_ReturnsCorrectInformation(
            string date, string expectedDayOfWeek, int expectedWeek, int expectedIsoYear)
        {
            // Act
            var result = DateUtilityTools.GetDateInfo(date);
            var json = GetResultAsJson(result);

            // Assert
            json.GetProperty("success").GetBoolean().Should().BeTrue();
            json.GetProperty("dayOfWeek").GetString().Should().Be(expectedDayOfWeek);
            json.GetProperty("isoWeek").GetInt32().Should().Be(expectedWeek);
            json.GetProperty("isoYear").GetInt32().Should().Be(expectedIsoYear);
        }

        [Fact]
        public void GetDateInfo_WithInvalidDate_ReturnsError()
        {
            // Act
            var result = DateUtilityTools.GetDateInfo("invalid-date");
            var json = GetResultAsJson(result);

            // Assert
            json.GetProperty("success").GetBoolean().Should().BeFalse();
            json.GetProperty("error").GetString().Should().Contain("Invalid date format");
        }
    }

    public class GetWeekDatesTests
    {
        [Fact]
        public void GetWeekDates_WithSpecificDate_ReturnsCorrectWeek()
        {
            // Arrange - Oct 15, 2025 is in week 42
            var testDate = "2025-10-15";

            // Act
            var result = DateUtilityTools.GetWeekDates(testDate);
            var json = GetResultAsJson(result);

            // Assert
            json.GetProperty("success").GetBoolean().Should().BeTrue();
            json.GetProperty("isoWeek").GetInt32().Should().Be(42);
            json.GetProperty("isoYear").GetInt32().Should().Be(2025);

            // Week 42 of 2025 starts on Monday Oct 13
            var dates = json.GetProperty("dates").EnumerateArray().ToList();
            dates.Should().HaveCount(7);
            dates[0].GetProperty("date").GetString().Should().Be("2025-10-13"); // Monday
            dates[0].GetProperty("dayOfWeek").GetString().Should().Be("Monday");
            dates[6].GetProperty("date").GetString().Should().Be("2025-10-19"); // Sunday
            dates[6].GetProperty("dayOfWeek").GetString().Should().Be("Sunday");
        }
    }

    public class IntegrationTests
    {
        [Fact]
        public void DateUtilityTools_RealWorldScenario_LastWednesdayFromOct19()
        {
            // This is the exact scenario from the user's bug report
            // User said "last Wednesday" on October 19, 2025 (Sunday)
            // Expected: October 15, 2025 (Wednesday)
            // Bug: Incorrectly calculated as October 16, 2025 (Thursday)

            // Act
            var result = DateUtilityTools.CalculateRelativeDate("last Wednesday", "2025-10-19");
            var json = GetResultAsJson(result);

            // Assert
            json.GetProperty("success").GetBoolean().Should().BeTrue("calculation should succeed");
            json.GetProperty("calculatedDate").GetString().Should().Be("2025-10-15", "last Wednesday from Oct 19 is Oct 15");
            json.GetProperty("calculatedDayOfWeek").GetString().Should().Be("Wednesday", "Oct 15, 2025 is a Wednesday");
            json.GetProperty("daysFromReference").GetInt32().Should().Be(-4, "4 days before Sunday Oct 19");
        }

        [Theory]
        [InlineData("2025-10-19", "last Monday", "2025-10-13", "Monday")]    // 6 days back
        [InlineData("2025-10-19", "last Tuesday", "2025-10-14", "Tuesday")]   // 5 days back
        [InlineData("2025-10-19", "last Wednesday", "2025-10-15", "Wednesday")] // 4 days back
        [InlineData("2025-10-19", "last Thursday", "2025-10-16", "Thursday")]  // 3 days back
        [InlineData("2025-10-19", "last Friday", "2025-10-17", "Friday")]     // 2 days back
        [InlineData("2025-10-19", "last Saturday", "2025-10-18", "Saturday")] // 1 day back
        public void DateUtilityTools_AllDaysOfWeek_FromSundayOct19(
            string referenceDate, string description, string expectedDate, string expectedDay)
        {
            // Act
            var result = DateUtilityTools.CalculateRelativeDate(description, referenceDate);
            var json = GetResultAsJson(result);

            // Assert
            json.GetProperty("success").GetBoolean().Should().BeTrue();
            json.GetProperty("calculatedDate").GetString().Should().Be(expectedDate);
            json.GetProperty("calculatedDayOfWeek").GetString().Should().Be(expectedDay);
        }
    }
}
