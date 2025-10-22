using System.Globalization;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Personal.Mcp.Services;

namespace Personal.Mcp.Tests.Tools;

/// <summary>
/// Unit tests for the TemplateService class.
/// </summary>
public class TemplateServiceTests
{
    private readonly TemplateService _templateService;
    private readonly Mock<ILogger<TemplateService>> _mockLogger;

    public TemplateServiceTests()
    {
        _mockLogger = new Mock<ILogger<TemplateService>>();
        _templateService = new TemplateService(_mockLogger.Object);
    }

    public class RenderTemplateTests : TemplateServiceTests
    {
        [Fact]
        public void RenderTemplate_WithSimpleVariable_ReturnsRenderedContent()
        {
            // Arrange
            var template = "Hello, {{name}}!";
            var context = new Dictionary<string, object>
            {
                ["name"] = "World"
            };

            // Act
            var result = _templateService.RenderTemplate(template, context);

            // Assert
            result.Should().Be("Hello, World!");
        }

        [Fact]
        public void RenderTemplate_WithMultipleVariables_ReturnsRenderedContent()
        {
            // Arrange
            var template = "{{greeting}}, {{name}}! You are {{age}} years old.";
            var context = new Dictionary<string, object>
            {
                ["greeting"] = "Hello",
                ["name"] = "Alice",
                ["age"] = 30
            };

            // Act
            var result = _templateService.RenderTemplate(template, context);

            // Assert
            result.Should().Be("Hello, Alice! You are 30 years old.");
        }

        [Fact]
        public void RenderTemplate_WithDateTimeVariable_FormatsDateCorrectly()
        {
            // Arrange
            var template = "Date: {{date | date: '%Y-%m-%d'}}";
            var testDate = new DateTime(2025, 10, 21);
            var context = new Dictionary<string, object>
            {
                ["date"] = testDate
            };

            // Act
            var result = _templateService.RenderTemplate(template, context);

            // Assert
            result.Should().Be("Date: 2025-10-21");
        }

        [Fact]
        public void RenderTemplate_WithConditional_RendersCorrectly()
        {
            // Arrange
            var template = @"{% if show_message %}
Message: {{message}}
{% endif %}";
            var context = new Dictionary<string, object>
            {
                ["show_message"] = true,
                ["message"] = "Hello"
            };

            // Act
            var result = _templateService.RenderTemplate(template, context);

            // Assert
            result.Should().Contain("Message: Hello");
        }

        [Fact]
        public void RenderTemplate_WithConditionalFalse_DoesNotRenderContent()
        {
            // Arrange
            var template = @"{% if show_message %}
Message: {{message}}
{% endif %}";
            var context = new Dictionary<string, object>
            {
                ["show_message"] = false,
                ["message"] = "Hello"
            };

            // Act
            var result = _templateService.RenderTemplate(template, context);

            // Assert
            result.Should().NotContain("Message: Hello");
        }

        [Fact]
        public void RenderTemplate_WithLoop_RendersAllItems()
        {
            // Arrange
            var template = @"{% for item in items %}
- {{item}}
{% endfor %}";
            var context = new Dictionary<string, object>
            {
                ["items"] = new[] { "Apple", "Banana", "Cherry" }
            };

            // Act
            var result = _templateService.RenderTemplate(template, context);

            // Assert
            result.Should().Contain("- Apple");
            result.Should().Contain("- Banana");
            result.Should().Contain("- Cherry");
        }

        [Fact]
        public void RenderTemplate_WithMissingVariable_RendersEmpty()
        {
            // Arrange
            var template = "Hello, {{missing_variable}}!";
            var context = new Dictionary<string, object>();

            // Act
            var result = _templateService.RenderTemplate(template, context);

            // Assert
            result.Should().Be("Hello, !");
        }

        [Fact]
        public void RenderTemplate_WithEmptyTemplate_ReturnsEmpty()
        {
            // Arrange
            var template = "";
            var context = new Dictionary<string, object>();

            // Act
            var result = _templateService.RenderTemplate(template, context);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void RenderTemplate_WithNullTemplate_ReturnsEmpty()
        {
            // Arrange
            string template = null!;
            var context = new Dictionary<string, object>();

            // Act
            var result = _templateService.RenderTemplate(template, context);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void RenderTemplate_WithInvalidSyntax_ThrowsInvalidOperationException()
        {
            // Arrange
            var template = "{{invalid}}{{";
            var context = new Dictionary<string, object>();

            // Act
            Action act = () => _templateService.RenderTemplate(template, context);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Failed to parse template:*");
        }

        [Fact]
        public void RenderTemplate_WithComplexObject_AccessesProperties()
        {
            // Arrange
            var template = "Name: {{user.name}}, Email: {{user.email}}";
            var context = new Dictionary<string, object>
            {
                ["user"] = new { name = "John Doe", email = "john@example.com" }
            };

            // Act
            var result = _templateService.RenderTemplate(template, context);

            // Assert
            result.Should().Be("Name: John Doe, Email: john@example.com");
        }

        [Fact]
        public void RenderTemplate_WithFilters_AppliesCorrectly()
        {
            // Arrange
            var template = "{{name | upcase}}";
            var context = new Dictionary<string, object>
            {
                ["name"] = "alice"
            };

            // Act
            var result = _templateService.RenderTemplate(template, context);

            // Assert
            result.Should().Be("ALICE");
        }

        [Fact]
        public void RenderTemplate_WithEmptyContext_RendersTemplateWithoutVariables()
        {
            // Arrange
            var template = "Static text without variables.";
            var context = new Dictionary<string, object>();

            // Act
            var result = _templateService.RenderTemplate(template, context);

            // Assert
            result.Should().Be("Static text without variables.");
        }
    }

    public class ValidateTemplateTests : TemplateServiceTests
    {
        [Fact]
        public void ValidateTemplate_WithValidTemplate_ReturnsTrue()
        {
            // Arrange
            var template = "Hello, {{name}}!";

            // Act
            var result = _templateService.ValidateTemplate(template);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidateTemplate_WithComplexValidTemplate_ReturnsTrue()
        {
            // Arrange
            var template = @"{% for item in items %}
- {{item}}
{% endfor %}";

            // Act
            var result = _templateService.ValidateTemplate(template);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidateTemplate_WithInvalidSyntax_ReturnsFalse()
        {
            // Arrange
            var template = "{{invalid}}{{";

            // Act
            var result = _templateService.ValidateTemplate(template);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateTemplate_WithEmptyTemplate_ReturnsFalse()
        {
            // Arrange
            var template = "";

            // Act
            var result = _templateService.ValidateTemplate(template);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateTemplate_WithNullTemplate_ReturnsFalse()
        {
            // Arrange
            string template = null!;

            // Act
            var result = _templateService.ValidateTemplate(template);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateTemplate_WithWhitespaceOnlyTemplate_ReturnsFalse()
        {
            // Arrange
            var template = "   ";

            // Act
            var result = _templateService.ValidateTemplate(template);

            // Assert
            result.Should().BeFalse();
        }
    }

    public class GetDefaultTemplatesTests : TemplateServiceTests
    {
        [Fact]
        public void GetDefaultJournalTemplate_ReturnsNonEmptyString()
        {
            // Act
            var result = _templateService.GetDefaultJournalTemplate();

            // Assert
            result.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void GetDefaultJournalTemplate_ContainsExpectedVariables()
        {
            // Act
            var result = _templateService.GetDefaultJournalTemplate();

            // Assert
            result.Should().Contain("{{monday_iso_week | iso_date: '%Y-W%V'}}");
        }

        [Fact]
        public void GetDefaultJournalTemplate_IsValidLiquidTemplate()
        {
            // Act
            var template = _templateService.GetDefaultJournalTemplate();
            var isValid = _templateService.ValidateTemplate(template);

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void GetDefaultJournalTemplate_CanBeRendered()
        {
            // Arrange
            var template = _templateService.GetDefaultJournalTemplate();
            var journalDay = new DateTime(2025, 10, 21);
            var monday = ISOWeek.ToDateTime(2025, 43, DayOfWeek.Monday);

            var context = new Dictionary<string, object>
            {
                ["week_number"] = 43,
                ["year"] = journalDay.Year,
                ["day_number"] = journalDay.Day,
                ["journal_day"] = journalDay,
                ["created_date"] = new DateTime(2025, 1, 1),
                ["monday_iso_week"] = monday

            };

            // Act
            var result = _templateService.RenderTemplate(template, context);

            // Assert
            result.Should().Contain("Week 43 in 2025");
            result.Should().Contain("## 20 Monday");
        }

        [Theory]
        [InlineData("%G", "2022", "2023-01-01 12:00:00")]
        [InlineData("%G", "2024", "2024-01-01 12:00:00")]
        [InlineData("%g", "22", "2023-01-01 12:00:00")]
        [InlineData("%g", "24", "2024-01-01 12:00:00")]
        [InlineData("%V", "31")]
        [InlineData("%W", "52", "2016-12-31T12:00:00")] // Saturday 12/31
        [InlineData("%W", "00", "2017-01-01T12:00:00")] // Sunday 01/01 - still not first week of the year
        [InlineData("%W", "01", "2017-01-02T12:00:00")] // Monday 01/02 - week begins on a Monday (for %W)
        [InlineData("%W", "25", "2022-06-26T00:00:00")]
        [InlineData("%V", "48", "2025-11-24T00:00:00")]
        [InlineData("%V", "01", "2024-12-30T00:00:00")]
        public async Task FormatDateInISO_CanBeRendered(string format, string expected, string dateTime = "2017-08-01T17:04:36.123456789+08:00")
        {
            // This test sets the CultureInfo.DateTimeFormat so it's not impacted by changes in ICU
            // see https://github.com/dotnet/runtime/issues/95620
            var enUsCultureInfo = new CultureInfo("en-US", useUserOverride: false);
            enUsCultureInfo.DateTimeFormat.FullDateTimePattern = "dddd, MMMM d, yyyy h:mm:ss tt";
            _templateService.SetCultureInfo(enUsCultureInfo);
            _templateService.SetTimeZone(TimeZoneInfo.Utc);

            var template = $"{{{{journal_day | iso_date: '{format}'}}}}";
            var journalDay = DateTime.Parse(dateTime);

            var context = new Dictionary<string, object>
            {
                ["journal_day"] = journalDay,
            };

            // Act
            var result = _templateService.RenderTemplate(template, context);

            result.Should().Be(expected, $"for format '{format}' on date '{journalDay}'");

        }        

        [Fact]
        public void GetDefaultNoteTemplate_ReturnsNonEmptyString()
        {
            // Act
            var result = _templateService.GetDefaultNoteTemplate();

            // Assert
            result.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void GetDefaultNoteTemplate_ContainsExpectedVariables()
        {
            // Act
            var result = _templateService.GetDefaultNoteTemplate();

            // Assert
            result.Should().Contain("{{title}}");
            result.Should().Contain("created_date");
        }

        [Fact]
        public void GetDefaultNoteTemplate_IsValidLiquidTemplate()
        {
            // Act
            var template = _templateService.GetDefaultNoteTemplate();
            var isValid = _templateService.ValidateTemplate(template);

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void GetDefaultNoteTemplate_CanBeRendered()
        {
            // Arrange
            var template = _templateService.GetDefaultNoteTemplate();
            var context = new Dictionary<string, object>
            {
                ["title"] = "My Note",
                ["created_date"] = new DateTime(2025, 10, 21)
            };

            // Act
            var result = _templateService.RenderTemplate(template, context);

            // Assert
            result.Should().Contain("title: My Note");
            result.Should().Contain("created: 2025-10-21");
            result.Should().Contain("# My Note");
        }

        [Fact]
        public void GetDefaultNoteTemplate_ContainsFrontmatter()
        {
            // Act
            var result = _templateService.GetDefaultNoteTemplate();

            // Assert
            result.Should().StartWith("---");
            result.Should().Contain("tags: []");
        }
    }

    public class EdgeCaseTests : TemplateServiceTests
    {
        [Theory]
        [InlineData("{{name}}", "John", "John")]
        [InlineData("{{ name }}", "John", "John")]
        [InlineData("{{  name  }}", "John", "John")]
        public void RenderTemplate_WithVariousWhitespace_HandlesCorrectly(string template, string value, string expected)
        {
            // Arrange
            var context = new Dictionary<string, object>
            {
                ["name"] = value
            };

            // Act
            var result = _templateService.RenderTemplate(template, context);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void RenderTemplate_WithSpecialCharacters_RendersCorrectly()
        {
            // Arrange
            var template = "Name: {{name}}";
            var context = new Dictionary<string, object>
            {
                ["name"] = "O'Brien & Associates <test@example.com>"
            };

            // Act
            var result = _templateService.RenderTemplate(template, context);

            // Assert
            result.Should().Be("Name: O'Brien & Associates <test@example.com>");
        }

        [Fact]
        public void RenderTemplate_WithNestedLoops_RendersCorrectly()
        {
            // Arrange
            var template = @"{% for group in groups %}
Group: {{group.name}}
{% for item in group.items %}  - {{item}}
{% endfor %}
{% endfor %}";
            var context = new Dictionary<string, object>
            {
                ["groups"] = new[]
                {
                    new { name = "A", items = new[] { "A1", "A2" } },
                    new { name = "B", items = new[] { "B1", "B2" } }
                }
            };

            // Act
            var result = _templateService.RenderTemplate(template, context);

            // Assert
            result.Should().Contain("Group: A");
            result.Should().Contain("- A1");
            result.Should().Contain("- A2");
            result.Should().Contain("Group: B");
            result.Should().Contain("- B1");
            result.Should().Contain("- B2");
        }

        [Fact]
        public void RenderTemplate_WithNumericValues_RendersCorrectly()
        {
            // Arrange
            var template = "Integer: {{int_val}}, Float: {{float_val}}, Boolean: {{bool_val}}";
            var context = new Dictionary<string, object>
            {
                ["int_val"] = 42,
                ["float_val"] = 3.14,
                ["bool_val"] = true
            };

            // Act
            var result = _templateService.RenderTemplate(template, context);

            // Assert
            result.Should().Contain("Integer: 42");
            result.Should().Contain("Float: 3.14");
            result.Should().Contain("Boolean: true");
        }

        [Fact]
        public void RenderTemplate_WithMultilineTemplate_PreservesFormatting()
        {
            // Arrange
            var template = @"# Header

Paragraph 1

## Subheader

- Item 1
- Item 2

Paragraph 2: {{value}}";
            var context = new Dictionary<string, object>
            {
                ["value"] = "test"
            };

            // Act
            var result = _templateService.RenderTemplate(template, context);

            // Assert
            result.Should().Contain("# Header");
            result.Should().Contain("## Subheader");
            result.Should().Contain("- Item 1");
            result.Should().Contain("Paragraph 2: test");
        }
    }
}
