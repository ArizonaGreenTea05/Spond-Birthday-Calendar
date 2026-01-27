namespace SpondBirthdayCalendar;

public class SpondConfiguration
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public List<SpondGroupInformation> Groups { get; set; } = [];
    public string CalendarPath { get; set; } = "/calendar.ics";
    public bool IgnoreAdmins { get; set; }
}
