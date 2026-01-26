using Spond.API.Services;
using SpondBirthdayCalendar;

var builder = WebApplication.CreateBuilder(args);

var appsettingsPaths = new[] { "../config/appsettings.json", "appsettings.json" };
var path = appsettingsPaths.FirstOrDefault(File.Exists) ?? throw new FileNotFoundException("appsettings not found");
using var appsettingsStream = File.OpenRead(path);
var appsettingsBuilder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonStream(appsettingsStream);

var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                      ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
if (string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase))
{
    appsettingsBuilder.AddJsonFile("appsettings.Development.json", optional: true);
}

var appsettings = appsettingsBuilder.Build();
var spondConfig = new SpondConfiguration();
appsettings.GetSection("Spond").Bind(spondConfig);

builder.Services.AddSingleton(spondConfig);
builder.Services.AddSingleton<SpondClient>();
builder.Services.AddScoped<CalendarService>();

builder.Services.AddLogging();

var app = builder.Build();

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
