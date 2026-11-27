namespace AttendScan.Application.Dtos.People;

/// <summary>
/// Resultado de leer (y, con commit, importar) el Excel de personas. Si hay alguna fila con error
/// no se importa nada: se corrige el Excel y se vuelve a subir.
/// </summary>
public class PersonImportResultDto
{
    public bool Committed { get; set; }
    public int TotalRows { get; set; }
    public int NewCount { get; set; }
    public int ExistingCount { get; set; }
    public int ErrorCount { get; set; }
    public List<PersonImportRowDto> Rows { get; set; } = [];
}

public class PersonImportRowDto
{
    /// <summary>Fila de Excel (como se ve en Excel, empezando en 1).</summary>
    public int RowNumber { get; set; }
    /// <summary>New: se creará. Existing: ya existe (no se crea otro carnet). Error: ver Errors.</summary>
    public string Status { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? District { get; set; }
    public string? DelegateType { get; set; }
    /// <summary>Código del carnet: el ya existente, o el nuevo después de importar.</summary>
    public string? CardCode { get; set; }
    public List<string> Errors { get; set; } = [];
}

public static class PersonImportStatus
{
    public const string New = "New";
    public const string Existing = "Existing";
    public const string Error = "Error";
}
