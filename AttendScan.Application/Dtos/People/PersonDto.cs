namespace AttendScan.Application.Dtos.People;

public class PersonDto
{
    public long PersonId { get; set; }
    /// <summary>Solo lectura: lo genera el servidor al crear y nunca cambia. Se ignora si llega en un POST/PUT.</summary>
    public string CardCode { get; set; } = string.Empty;
    public int? ListNumber { get; set; }
    public string? Region { get; set; }
    public string? District { get; set; }
    public string? Church { get; set; }
    public string PaternalSurname { get; set; } = string.Empty;
    public string? MaternalSurname { get; set; }
    public string GivenNames { get; set; } = string.Empty;
    /// <summary>Solo lectura: "Paterno Materno Nombres".</summary>
    public string FullName { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? DelegateType { get; set; }
    public int? Age { get; set; }
    public string? Gender { get; set; }
    public DateTime RegistrationDate { get; set; }
}
