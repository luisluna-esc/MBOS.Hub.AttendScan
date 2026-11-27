namespace AttendScan.Application.Dtos.Events;

public class EventDto
{
    public long EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime RegistrationDate { get; set; }
}
