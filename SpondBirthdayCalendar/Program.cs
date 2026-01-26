using Spond.API.Services;
using SpondBirthdayCalendar;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<SpondConfiguration>(
    builder.Configuration.GetSection("Spond"));

builder.Services.AddSingleton<SpondClient>();
builder.Services.AddScoped<CalendarService>();

builder.Services.AddLogging();

var app = builder.Build();

var spondConfig = app.Configuration.GetSection("Spond").Get<SpondConfiguration>();
var calendarPath = spondConfig?.CalendarPath ?? "/calendar.ics";

app.MapGet(calendarPath, async (CalendarService calendarService) =>
{
    try
    {
        var icsContent = await calendarService.GenerateBirthdayCalendarAsync();

        ArgumentNullException.ThrowIfNull(icsContent);

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
