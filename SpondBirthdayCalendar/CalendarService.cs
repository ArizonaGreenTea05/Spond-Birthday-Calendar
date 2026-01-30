using System.Globalization;
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
            logger.LogInformation("Starting to generate birthday calendar for groups [{GroupIds}]", string.Join(", ", spondConfiguration.Groups.Select(g => $"{g.GroupId}[{string.Join(",", g.SubGroupIds)}]")));

            // Authenticate with Spond
            if (!await spondClient.LoginWithEmail(spondConfiguration.Username, spondConfiguration.Password)
                && !await spondClient.LoginWithPhoneNumber(spondConfiguration.Username, spondConfiguration.Password))
                return null;
            logger.LogInformation("Successfully logged in to Spond");

            // Create calendar
            var calendar = new Calendar
            {
                ProductId = $"-//Spond {Translations.ResourceManager.GetString(nameof(Translations.BirthdayCalendar), cultureInfo)}//{cultureInfo.TwoLetterISOLanguageName}",
                Version = "2.0"
            };

            // Fetch all groups
            var groups = await spondClient.GetGroups();

            foreach (var (group, info) in groups.Select(g => (g, spondConfiguration.Groups.FirstOrDefault(gi => gi.GroupId == g.Id))).Where(g => g.Item2 is not null))
            {
                logger.LogInformation("Retrieved group: {GroupName} with {MemberCount} members", group.Name, group.Members?.Count ?? 0);

                // Add birthday events for each member
                if (group.Members is null) continue;

                var members = info!.SubGroupIds.Count <= 0 ? group.Members : group.Members.Where(m => m.SubGroups.Any(sg => info.SubGroupIds.Contains(sg)));

                foreach (var member in spondConfiguration.IgnoreAdmins ? members.Where(m => m.Respondent) : members)
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
                        Summary = GetEventTitle(cultureInfo, [group.Id], member.SubGroups, memberName),
                        Description = GetEventDescription(cultureInfo, [group.Id], member.SubGroups, memberName),
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

    private string GetEventTitle(CultureInfo cultureInfo, List<string> groups, List<string> subGroups, string memberName)
    {
        var eventTitle = calendarConfiguration.CustomTitlePerSubGroup.FirstOrDefault(i => subGroups.Contains(i.Key)).Value;
        if (string.IsNullOrWhiteSpace(eventTitle)) eventTitle = calendarConfiguration.CustomTitlePerGroup.FirstOrDefault(i => groups.Contains(i.Key)).Value;
        if (string.IsNullOrWhiteSpace(eventTitle) && calendarConfiguration.CustomTitle is not null) eventTitle = string.Format(calendarConfiguration.CustomTitle, memberName);
        if (string.IsNullOrWhiteSpace(eventTitle)) eventTitle = $"{Translations.ResourceManager.GetString(nameof(Translations.Birthday), cultureInfo)} {memberName}";
        return string.Format(eventTitle, memberName);
    }

    private string? GetEventDescription(CultureInfo cultureInfo, List<string> groups, List<string> subGroups, string memberName)
    {
        var eventDescription = calendarConfiguration.CustomDescriptionPerSubGroup.FirstOrDefault(i => subGroups.Contains(i.Key)).Value;
        if (string.IsNullOrWhiteSpace(eventDescription)) eventDescription = calendarConfiguration.CustomDescriptionPerGroup.FirstOrDefault(i => groups.Contains(i.Key)).Value;
        if (string.IsNullOrWhiteSpace(eventDescription) && calendarConfiguration.CustomDescription is not null) eventDescription = string.Format(calendarConfiguration.CustomDescription, memberName);
        if (string.IsNullOrWhiteSpace(eventDescription)) eventDescription = null;
        return eventDescription is null ? null : string.Format(eventDescription, memberName);
    }
}
