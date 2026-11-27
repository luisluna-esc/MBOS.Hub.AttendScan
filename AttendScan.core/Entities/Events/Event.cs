namespace AttendScan.core.Entities.Events;

/// <summary>Evento (ej. un congreso). La asistencia se toma por cada una de sus sesiones.</summary>
public class Event
{
    public long EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
    public long? AppUserId { get; set; }
}
