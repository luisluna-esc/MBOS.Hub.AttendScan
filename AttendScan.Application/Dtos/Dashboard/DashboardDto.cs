namespace AttendScan.Application.Dtos.Dashboard;

/// <summary>Resumen para el Inicio: personas por género, tipo y región, y la asistencia de una sesión.</summary>
public class DashboardDto
{
    public int TotalPersons { get; set; }
    /// <summary>Sesión cuya asistencia se cuenta (null = solo personas, sin asistencia).</summary>
    public long? EventSessionId { get; set; }
    /// <summary>Personas con asistencia en la sesión (null si no se pidió sesión).</summary>
    public int? Attended { get; set; }
    /// <summary>Clave "M", "F" o "" (sin dato).</summary>
    public List<DashboardGroupDto> ByGender { get; set; } = [];
    public List<DashboardGroupDto> ByDelegateType { get; set; } = [];
    /// <summary>Clave "" = sin región (MBOS, UB, instituciones e invitados).</summary>
    public List<DashboardGroupDto> ByRegion { get; set; } = [];
}

public class DashboardGroupDto
{
    public string Key { get; set; } = string.Empty;
    public int Total { get; set; }
    /// <summary>Cuántos de este grupo ya marcaron asistencia en la sesión (null si no se pidió sesión).</summary>
    public int? Attended { get; set; }
}
