namespace AttendScan.core.Entities.Events;

/// <summary>
/// Sesión de un evento (ej. "Día 1 - Mañana"). Una persona puede marcar asistencia una sola vez
/// por sesión. Solo se escanea en sesiones abiertas (IsOpen).
/// </summary>
public class EventSession
{
    public long EventSessionId { get; set; }
    public long EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime SessionDate { get; set; }
    public bool IsOpen { get; set; } = true;
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
    public long? AppUserId { get; set; }
}
