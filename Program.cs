using SpondBirthdayCalendar;

var builder = WebApplication.CreateBuilder(args);

// Configure Spond settings
builder.Services.Configure<SpondConfiguration>(
    builder.Configuration.GetSection("Spond"));

// Register the calendar service
builder.Services.AddScoped<CalendarService>();

// Add logging
builder.Services.AddLogging();

var app = builder.Build();

// Map the calendar endpoint
var spondConfig = app.Configuration.GetSection("Spond").Get<SpondConfiguration>();
var calendarPath = spondConfig?.CalendarPath ?? "/calendar.ics";

app.MapGet(calendarPath, async (CalendarService calendarService) =>
{
    try
    {
        var icsContent = await calendarService.GenerateBirthdayCalendarAsync();
        
        return Results.Text(
            icsContent, 
            contentType: "text/calendar", 
            statusCode: 200);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to generate calendar");
        return Results.Problem(
            detail: "Failed to generate calendar. Please check the logs for more details.",
            statusCode: 500);
    }
});

app.Run();
