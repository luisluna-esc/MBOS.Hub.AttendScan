namespace AttendScan.Application.Dtos.Events;

/// <summary>Resultado de escanear un carnet. Status es uno de los valores de <see cref="ScanStatus"/>.</summary>
public class ScanResultDto
{
    public string Status { get; set; } = string.Empty;
    public long? PersonId { get; set; }
    public string? FullName { get; set; }
    public string? CardCode { get; set; }
    /// <summary>Hora de la asistencia: la nueva, o la que ya existía si ya estaba registrado.</summary>
    public DateTime? ScannedAt { get; set; }
    /// <summary>Total de asistentes de la sesión después de este escaneo.</summary>
    public int AttendanceCount { get; set; }
}

public static class ScanStatus
{
    /// <summary>Asistencia registrada ahora.</summary>
    public const string Registered = "Registered";
    /// <summary>La persona ya tenía asistencia en esta sesión: no se duplica.</summary>
    public const string AlreadyRegistered = "AlreadyRegistered";
    /// <summary>El código tiene formato válido pero no corresponde a ningún carnet.</summary>
    public const string UnknownCard = "UnknownCard";
    /// <summary>No es un código de AttendScan (otro QR, o el verificador no cuadra).</summary>
    public const string InvalidCode = "InvalidCode";
}
