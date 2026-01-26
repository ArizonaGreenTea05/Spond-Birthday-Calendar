using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Spond.API.Services;

namespace SpondBirthdayCalendar;

public class CalendarService(
    SpondConfiguration configuration,
    SpondClient spondClient,
    ILogger<CalendarService> logger)
{

    public async Task<string?> GenerateBirthdayCalendarAsync()
    {
        try
        {
            logger.LogInformation("Starting to generate birthday calendar for group {GroupId}", configuration.GroupId);

            // Authenticate with Spond
            if (!await spondClient.LoginWithEmail(configuration.Username, configuration.Password)
                && !await spondClient.LoginWithPhoneNumber(configuration.Username, configuration.Password))
                return null;
            logger.LogInformation("Successfully logged in to Spond");

            // Fetch all groups
            var groups = await spondClient.GetGroups();
            var group = groups?.FirstOrDefault(g => g.Id == configuration.GroupId);
            
            if (group is null)
            {
                logger.LogError("Group with ID {GroupId} not found", configuration.GroupId);
                throw new InvalidOperationException($"Group with ID {configuration.GroupId} not found");
            }

            logger.LogInformation("Retrieved group: {GroupName} with {MemberCount} members", 
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

                        logger.LogDebug("Adding birthday event for {MemberName} on {Birthday:MM-dd}", 
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

            logger.LogInformation("Created calendar with {EventCount} birthday events", calendar.Events.Count);

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
            logger.LogError(ex, "Error generating birthday calendar");
            throw;
        }
    }
}
