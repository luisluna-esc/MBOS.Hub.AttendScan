using System.Globalization;
using System.Text;
using ClosedXML.Excel;

namespace AttendScan.Application.Services;

/// <summary>Una fila de la lista de personas, tal como viene del Excel (textos ya sin espacios de más).</summary>
public sealed class PersonExcelRow
{
    public int RowNumber { get; init; }
    public int? ListNumber { get; init; }
    public string? Region { get; init; }
    public string? District { get; init; }
    public string? Church { get; init; }
    public string? PaternalSurname { get; init; }
    public string? MaternalSurname { get; init; }
    public string? GivenNames { get; init; }
    public string? Position { get; init; }
    public string? DelegateType { get; init; }
    public int? Age { get; init; }
    public string? Gender { get; init; }
    public List<string> Errors { get; } = [];
}

/// <summary>
/// Lee la lista de delegados (names_revisado.xlsx). Las columnas se buscan por su título, no por
/// posición, así que el orden de las columnas puede cambiar. Obligatorias: "Apellido paterno",
/// "Apellido materno" y "Nombres".
/// </summary>
public static class PersonExcelReader
{
    private static readonly Dictionary<string, string[]> Headers = new()
    {
        ["number"] = ["n°", "nº", "no", "nro", "numero"],
        ["region"] = ["region"],
        ["district"] = ["distrito"],
        ["church"] = ["nombre de iglesia", "iglesia"],
        ["paternal"] = ["apellido paterno", "paterno"],
        ["maternal"] = ["apellido materno", "materno"],
        ["given"] = ["nombres"],
        ["position"] = ["funcion"],
        ["type"] = ["tipo de delegado", "tipo"],
        ["age"] = ["edad"],
        ["gender"] = ["genero", "sexo"],
    };

    private static readonly string[] Required = ["paternal", "maternal", "given"];

    /// <summary>Filas del Excel; error de formato (sin las columnas obligatorias) como excepción con mensaje para el usuario.</summary>
    public static List<PersonExcelRow> Read(Stream excel)
    {
        using var workbook = new XLWorkbook(excel);
        var sheet = workbook.Worksheets.First();

        var (headerRow, columns) = FindHeader(sheet)
            ?? throw new InvalidDataException("The Excel must have the columns \"Apellido paterno\", \"Apellido materno\" and \"Nombres\" (use names_revisado.xlsx).");

        var rows = new List<PersonExcelRow>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? headerRow;
        for (var r = headerRow + 1; r <= lastRow; r++)
        {
            string? Text(string field) => columns.TryGetValue(field, out var c) ? PersonNames.Clean(sheet.Cell(r, c).GetFormattedString()) : null;

            var paternal = Text("paternal");
            var given = Text("given");
            var maternal = Text("maternal");
            if (paternal is null && given is null && maternal is null)
            {
                continue; // fila vacía
            }

            var row = new PersonExcelRow
            {
                RowNumber = r,
                ListNumber = ParseInt(Text("number")),
                Region = Text("region"),
                District = Text("district"),
                Church = Text("church"),
                PaternalSurname = paternal,
                MaternalSurname = maternal,
                GivenNames = given,
                Position = Text("position"),
                DelegateType = Text("type"),
                Age = ParseInt(Text("age")),
                Gender = Text("gender")?.ToUpperInvariant(),
            };

            if (paternal is null) row.Errors.Add("Falta el apellido paterno.");
            if (given is null) row.Errors.Add("Faltan los nombres.");
            if (Text("age") is { } age && row.Age is null) row.Errors.Add($"Edad no válida: \"{age}\".");
            if (row.Gender is not (null or "M" or "F")) row.Errors.Add($"Género no válido: \"{row.Gender}\" (debe ser M o F).");
            rows.Add(row);
        }

        return rows;
    }

    private static (int Row, Dictionary<string, int> Columns)? FindHeader(IXLWorksheet sheet)
    {
        // El título de la lista ocupa las primeras filas; los encabezados están cerca del inicio.
        var lastColumn = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        for (var r = 1; r <= 10; r++)
        {
            var columns = new Dictionary<string, int>();
            for (var c = 1; c <= lastColumn; c++)
            {
                var header = Simplify(sheet.Cell(r, c).GetString());
                foreach (var (field, names) in Headers)
                {
                    if (!columns.ContainsKey(field) && names.Contains(header))
                    {
                        columns[field] = c;
                    }
                }
            }

            if (Required.All(columns.ContainsKey))
            {
                return (r, columns);
            }
        }

        return null;
    }

    private static int? ParseInt(string? value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number) && number == Math.Floor(number)
            ? (int)number
            : null;

    /// <summary>"  REGIÓN " → "region": sin tildes, sin mayúsculas, sin espacios de más.</summary>
    private static string Simplify(string value)
    {
        var decomposed = (PersonNames.Clean(value) ?? string.Empty).ToLowerInvariant().Normalize(NormalizationForm.FormD);
        return new string(decomposed.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
    }
}
