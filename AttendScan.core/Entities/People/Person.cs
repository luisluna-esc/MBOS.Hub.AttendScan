namespace AttendScan.core.Entities.People;

/// <summary>
/// Persona con carnet, con los mismos datos de la lista de delegados (Excel). CardCode es lo único
/// que lleva el QR impreso: se genera una sola vez al crear la persona y nunca cambia (la base de
/// datos lo impide con un trigger). La persona tampoco se puede borrar: su carnet impreso dejaría
/// de servir, y los carnets nunca se anulan.
/// </summary>
public class Person
{
    public long PersonId { get; set; }
    public string CardCode { get; set; } = string.Empty;
    /// <summary>N° de la persona en la lista (Excel).</summary>
    public int? ListNumber { get; set; }
    public string? Region { get; set; }
    public string? District { get; set; }
    public string? Church { get; set; }
    public string PaternalSurname { get; set; } = string.Empty;
    public string? MaternalSurname { get; set; }
    public string GivenNames { get; set; } = string.Empty;
    /// <summary>"Paterno Materno Nombres", armado siempre desde las tres partes.</summary>
    public string FullName { get; set; } = string.Empty;
    /// <summary>Función (ej. Anciano, Pastor Distrital).</summary>
    public string? Position { get; set; }
    /// <summary>Tipo de delegado (ej. Delegado Regular, Invitados).</summary>
    public string? DelegateType { get; set; }
    public int? Age { get; set; }
    /// <summary>"M" o "F".</summary>
    public string? Gender { get; set; }
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
    public long? AppUserId { get; set; }
}
