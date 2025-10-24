---
applyTo: "**/*.cs"
---

# C# Date and Time Handling

## Critical Rules

1. **Use `DateTimeOffset`**, not `DateTime`
2. **Use `TimeProvider`** via dependency injection for testability

---

## TimeProvider Pattern (Preferred)

**For classes using DI**, inject `TimeProvider` instead of calling `DateTimeOffset.Now` directly:

```csharp
// ✅ BEST - Testable with TimeProvider (primary constructor)
public class OrderService(TimeProvider timeProvider)
{
    public void ProcessOrder()
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        // or timeProvider.GetLocalNow() for local time
    }
}

// ✅ BEST - Testable with TimeProvider (traditional constructor)
public class OrderService
{
    private readonly TimeProvider _timeProvider;
    
    public OrderService(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }
    
    public void ProcessOrder()
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
    }
}

// ❌ AVOID - Hard to test
public class OrderService
{
    public void ProcessOrder()
    {
        var now = DateTimeOffset.Now;  // Can't control in tests
    }
}
```

**Register in DI** (`Program.cs`):
```csharp
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
```

**Test with FakeTimeProvider** (requires `Microsoft.Extensions.TimeProvider.Testing`):
```csharp
[Fact]
public void ProcessOrder_CreatesOrderWithSpecificTimestamp()
{
    // Arrange
    var fakeTime = new FakeTimeProvider();
    fakeTime.SetUtcNow(new DateTimeOffset(2025, 10, 24, 14, 30, 0, TimeSpan.Zero));
    var service = new OrderService(fakeTime);
    
    // Act
    var order = service.ProcessOrder();
    
    // Assert
    order.Timestamp.Should().Be(fakeTime.GetUtcNow());
}
```

---

## DateTimeOffset Patterns

### When DI Is Available (Services, Tools)

| Operation | Code |
|-----------|------|
| Get current UTC time | `timeProvider.GetUtcNow()` |
| Get current local time | `timeProvider.GetLocalNow()` |

### When DI Is NOT Available (Static methods, utilities)

| Operation | Code |
|-----------|------|
| Current time | `DateTimeOffset.Now` |
| UTC time | `DateTimeOffset.UtcNow` |

Add comment: `// TODO: Consider TimeProvider for testability`

### General Operations (Same Everywhere)

| Operation | Code |
|-----------|------|
| Today's date | `DateTimeOffset.Now.LocalDateTime.Date` |
| Create specific date | `new DateTimeOffset(2025, 10, 24, 0, 0, 0, TimeSpan.Zero)` |
| Parse string | `DateTimeOffset.Parse(str)` |
| Try parse | `DateTimeOffset.TryParse(str, out var result)` |
| Format | `dto.ToString("yyyy-MM-dd")` |
| ISO 8601 | `dto.ToString("o")` |
| Add time | `dto.AddDays(7)` |
| Compare | `dto1 > dto2` |

---

## Special Cases

### ISO Week (returns DateTime)
```csharp
var dt = ISOWeek.ToDateTime(year, week, DayOfWeek.Monday);
var dto = new DateTimeOffset(dt, TimeSpan.Zero);
```

### Date-Only Operations
```csharp
var dateOnly = DateTimeOffset.Now.LocalDateTime.Date;
```

### File System Dates
```csharp
var fileInfo = new FileInfo(path);
var lastModified = new DateTimeOffset(fileInfo.LastWriteTime, DateTimeOffset.Now.Offset);
```

### External APIs Requiring DateTime
```csharp
// REQUIRED: ExternalApi needs DateTime
var result = api.Process(dateTimeOffset.DateTime);
```

---

## When DateTime Is Acceptable

**ONLY** use `DateTime` when:
1. External library explicitly requires it (add comment)
2. Framework method returns it (convert to DateTimeOffset immediately)

---

## Resources

- Migration guide: `.github/prompts/migrate-datetime-to-datetimeoffset.md`
- TimeProvider overview: https://learn.microsoft.com/en-us/dotnet/standard/datetime/timeprovider-overview
- Blog post: https://blog.nimblepros.com/blogs/finally-an-abstraction-for-time-in-net/

---

**Last Updated**: October 24, 2025
