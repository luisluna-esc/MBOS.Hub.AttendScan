using System.Globalization;
using System.Text;

namespace AttendScan.Application.Services;

/// <summary>Reglas de nombres compartidas por el alta manual y la importación del Excel.</summary>
public static class PersonNames
{
    /// <summary>Sin espacios de más ni saltos de línea: "  Juan   Pérez \n" → "Juan Pérez". Vacío → null.</summary>
    public static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>"Paterno Materno Nombres" (sin materno: "Paterno Nombres").</summary>
    public static string FullName(string paternalSurname, string? maternalSurname, string givenNames) =>
        string.Join(' ', new[] { paternalSurname, maternalSurname, givenNames }.Select(Clean).Where(p => p is not null));

    /// <summary>
    /// Clave para reconocer a la misma persona aunque esté escrita distinto: sin tildes, sin
    /// mayúsculas, sin comas y sin importar el orden de las palabras ("Juan Carlos Pérez Gómez"
    /// = "Pérez Gómez Juan Carlos"). Con esto no se le hace doble carnet a nadie.
    /// </summary>
    public static string Key(string fullName)
    {
        var decomposed = fullName.Replace(',', ' ').ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var withoutMarks = new string(decomposed
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray());
        return string.Join(' ', withoutMarks.Split(' ', StringSplitOptions.RemoveEmptyEntries).Order(StringComparer.Ordinal));
    }
}
