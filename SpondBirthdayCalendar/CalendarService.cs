using System.Globalization;
using System.Runtime.Versioning;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Spond.API.Services;
using SpondBirthdayCalendar.Resources;
using Calendar = Ical.Net.Calendar;

namespace SpondBirthdayCalendar;

public class CalendarService(
    SpondConfiguration spondConfiguration,
    CalendarConfiguration calendarConfiguration,
    SpondClient spondClient,
    ILogger<CalendarService> logger)
{

    public async Task<string?> GenerateBirthdayCalendarAsync()
    {
        var cultureInfo = new CultureInfo(calendarConfiguration.Language);
        try
        {
            logger.LogInformation("Starting to generate birthday calendar for group {GroupId}", spondConfiguration.GroupId);

            // Authenticate with Spond
            if (!await spondClient.LoginWithEmail(spondConfiguration.Username, spondConfiguration.Password)
                && !await spondClient.LoginWithPhoneNumber(spondConfiguration.Username, spondConfiguration.Password))
                return null;
            logger.LogInformation("Successfully logged in to Spond");

            // Fetch all groups
            var groups = await spondClient.GetGroups();
            var group = groups?.FirstOrDefault(g => g.Id == spondConfiguration.GroupId);
            
            if (group is null)
            {
                logger.LogError("Group with ID {GroupId} not found", spondConfiguration.GroupId);
                throw new InvalidOperationException($"Group with ID {spondConfiguration.GroupId} not found");
            }

            logger.LogInformation("Retrieved group: {GroupName} with {MemberCount} members", 
                group.Name, group.Members?.Count ?? 0);

            // Create calendar
            var calendar = new Calendar
            {
                ProductId = $"-//Spond {Translations.ResourceManager.GetString(nameof(Translations.BirthdayCalendar), cultureInfo)}//{cultureInfo.TwoLetterISOLanguageName}",
                Version = "2.0"
            };

            // Add birthday events for each member
            if (group.Members is not null)
            {
                foreach (var member in spondConfiguration.IgnoreAdmins ? group.Members.Where(m => m.Respondent) : group.Members)
                {
                    if (!member.Birthday.HasValue) continue;
                    var birthday = member.Birthday.Value;
                    var memberName = $"{member.FirstName} {member.LastName}".Trim();
                        
                    if (string.IsNullOrWhiteSpace(memberName) && member.Profile is not null)
                    {
                        memberName = $"{member.Profile.FirstName} {member.Profile.LastName}".Trim();
                    }

                    logger.LogDebug("Adding birthday event for {MemberName} on {Birthday:MM-dd}", memberName, birthday);

                    var calendarEvent = new CalendarEvent
                    {
                        Summary = $"{Translations.ResourceManager.GetString(nameof(Translations.Birthday), cultureInfo)} {memberName}",
                        Start = new CalDateTime(birthday, false),
                        RecurrenceRules = new List<RecurrencePattern>
                        {
                            new(FrequencyType.Yearly)
                        }
                    };

                    calendar.Events.Add(calendarEvent);
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
