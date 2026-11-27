namespace AttendScan.Application.Dtos.Events;

public class AttendanceDto
{
    public long AttendanceId { get; set; }
    public long EventSessionId { get; set; }
    public long PersonId { get; set; }
    public string CardCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? District { get; set; }
    public string? DelegateType { get; set; }
    public DateTime ScannedAt { get; set; }
}
