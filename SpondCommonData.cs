using Spond.API.Interfaces;
using static Spond.API.Enums;

namespace SpondBirthdayCalendar;

public class SpondCommonData : ICommonData
{
    public string LoginTokenPropertyName { get; set; } = "loginToken";
    public string BaseUrl { get; set; } = "https://api.spond.com/core/v1/";
    public string LoginUrl { get; set; } = "login";
    public string UserUrl { get; set; } = "user";
    public string GroupsUrl { get; set; } = "groups/";

    public string GetEventsUrl(DateTime minEndTime, DateTime maxEndTime, bool? includeComments, bool? includeHidden, bool? addProfileInfo, bool? scheduled, Order? order, int? max)
    {
        return BuildEventsUrl(null, null, minEndTime, maxEndTime, includeComments, includeHidden, addProfileInfo, scheduled, order, max);
    }

    public string GetEventsUrl(string groupId, DateTime minEndTime, DateTime maxEndTime, bool? includeComments, bool? includeHidden, bool? addProfileInfo, bool? scheduled, Order? order, int? max)
    {
        return BuildEventsUrl(groupId, null, minEndTime, maxEndTime, includeComments, includeHidden, addProfileInfo, scheduled, order, max);
    }

    public string GetEventsUrl(string groupId, string subGroupId, DateTime minEndTime, DateTime maxEndTime, bool? includeComments, bool? includeHidden, bool? addProfileInfo, bool? scheduled, Order? order, int? max)
    {
        return BuildEventsUrl(groupId, subGroupId, minEndTime, maxEndTime, includeComments, includeHidden, addProfileInfo, scheduled, order, max);
    }

    private string BuildEventsUrl(string? groupId, string? subGroupId, DateTime minEndTime, DateTime maxEndTime, bool? includeComments, bool? includeHidden, bool? addProfileInfo, bool? scheduled, Order? order, int? max)
    {
        var queryParams = new List<string>
        {
            $"minEndTimestamp={((DateTimeOffset)minEndTime).ToUnixTimeSeconds()}",
            $"maxEndTimestamp={((DateTimeOffset)maxEndTime).ToUnixTimeSeconds()}"
        };

        if (includeComments.HasValue)
            queryParams.Add($"includeComments={includeComments.Value.ToString().ToLower()}");
        if (includeHidden.HasValue)
            queryParams.Add($"includeHidden={includeHidden.Value.ToString().ToLower()}");
        if (addProfileInfo.HasValue)
            queryParams.Add($"addProfileInfo={addProfileInfo.Value.ToString().ToLower()}");
        if (scheduled.HasValue)
            queryParams.Add($"scheduled={scheduled.Value.ToString().ToLower()}");
        if (order.HasValue)
            queryParams.Add($"order={order.Value.ToString().ToUpper()}");
        if (max.HasValue)
            queryParams.Add($"max={max.Value}");
        if (!string.IsNullOrEmpty(groupId))
            queryParams.Add($"groupId={groupId}");
        if (!string.IsNullOrEmpty(subGroupId))
            queryParams.Add($"subGroupId={subGroupId}");

        return $"sponds/?{string.Join("&", queryParams)}";
    }
}
