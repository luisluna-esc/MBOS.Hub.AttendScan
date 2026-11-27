namespace AttendScan.core.QueryFilters.Events;

public class EventSessionQueryFilter
{
    public long? EventId { get; set; }
    public long? EventSessionId { get; set; }
    public bool? IsOpen { get; set; }
}
