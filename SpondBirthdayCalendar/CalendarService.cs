using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.Extensions.Options;
using Spond.API.Services;
using Microsoft.Extensions.Logging;

namespace SpondBirthdayCalendar;

public class CalendarService
{
    private readonly SpondConfiguration _configuration;
    private readonly SpondClient _spondClient;
    private readonly ILogger<CalendarService> _logger;

    public CalendarService(IOptions<SpondConfiguration> configuration, SpondClient spondClient, ILogger<CalendarService> logger)
    {
        _configuration = configuration.Value;
        _spondClient = spondClient;
        _logger = logger;
    }

    public async Task<string?> GenerateBirthdayCalendarAsync()
    {
        try
        {
            _logger.LogInformation("Starting to generate birthday calendar for group {GroupId}", _configuration.GroupId);

            // Authenticate with Spond
            if (!await _spondClient.LoginWithEmail(_configuration.Username, _configuration.Password)
                && !await _spondClient.LoginWithPhoneNumber(_configuration.Username, _configuration.Password))
                return null;
            _logger.LogInformation("Successfully logged in to Spond");

            // Fetch all groups
            var groups = await _spondClient.GetGroups();
            var group = groups?.FirstOrDefault(g => g.Id == _configuration.GroupId);
            
            if (group == null)
            {
                _logger.LogError("Group with ID {GroupId} not found", _configuration.GroupId);
                throw new InvalidOperationException($"Group with ID {_configuration.GroupId} not found");
            }

            _logger.LogInformation("Retrieved group: {GroupName} with {MemberCount} members", 
                group.Name, group.Members?.Count ?? 0);

            // Create calendar
            var calendar = new Calendar
            {
                ProductId = "-//Spond Birthday Calendar//EN",
                Version = "2.0"
            };

            // Add birthday events for each member
            if (group.Members != null)
            {
                foreach (var member in group.Members)
                {
                    if (member.Birthday.HasValue)
                    {
                        var birthday = member.Birthday.Value;
                        var memberName = $"{member.FirstName} {member.LastName}".Trim();
                        
                        if (string.IsNullOrWhiteSpace(memberName) && member.Profile != null)
                        {
                            memberName = $"{member.Profile.FirstName} {member.Profile.LastName}".Trim();
                        }

                        _logger.LogDebug("Adding birthday event for {MemberName} on {Birthday:MM-dd}", 
                            memberName, birthday);

                        var calendarEvent = new CalendarEvent
                        {
                            Summary = $"{memberName}'s Birthday",
                            Start = new CalDateTime(birthday),
                            RecurrenceRules = new List<RecurrencePattern>
                            {
                                new RecurrencePattern(FrequencyType.Yearly)
                            }
                        };

                        calendar.Events.Add(calendarEvent);
                    }
                }
            }

            _logger.LogInformation("Created calendar with {EventCount} birthday events", calendar.Events.Count);

            // Serialize to ICS format
            var serializer = new CalendarSerializer();
            var icsContent = serializer.SerializeToString(calendar);

            if (string.IsNullOrEmpty(icsContent))
            {
                throw new InvalidOperationException("Failed to serialize calendar to ICS format");
            }

            return icsContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating birthday calendar");
            throw;
        }
    }
}
