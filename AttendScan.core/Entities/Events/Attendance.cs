namespace AttendScan.core.Entities.Events;

/// <summary>
/// Asistencia de una persona a una sesión. Única por (EventSessionId, PersonId): la base de datos
/// lo garantiza aunque dos celulares escaneen el mismo carnet en el mismo instante.
/// </summary>
public class Attendance
{
    public long AttendanceId { get; set; }
    public long EventSessionId { get; set; }
    public long PersonId { get; set; }
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Usuario que escaneó.</summary>
    public long? AppUserId { get; set; }
}
