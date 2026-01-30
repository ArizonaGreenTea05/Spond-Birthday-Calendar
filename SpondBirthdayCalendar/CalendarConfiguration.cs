namespace SpondBirthdayCalendar;

public class CalendarConfiguration
{
    public string Language { get; set; } = "en";

    public string? CustomTitle { get; set; }

    public string? CustomDescription { get; set; }

    public Dictionary<string, string> CustomTitlePerGroup { get; set; } = [];

    public Dictionary<string, string> CustomDescriptionPerGroup { get; set; } = [];

    public Dictionary<string, string> CustomTitlePerSubGroup { get; set; } = [];

    public Dictionary<string, string> CustomDescriptionPerSubGroup { get; set; } = [];
}
