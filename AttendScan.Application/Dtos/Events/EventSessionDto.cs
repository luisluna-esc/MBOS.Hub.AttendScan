namespace AttendScan.Application.Dtos.Events;

public class EventSessionDto
{
    public long EventSessionId { get; set; }
    public long EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime SessionDate { get; set; }
    public bool IsOpen { get; set; }
    /// <summary>Solo lectura: cuántas personas marcaron asistencia en la sesión.</summary>
    public int AttendanceCount { get; set; }
    public DateTime RegistrationDate { get; set; }
}
