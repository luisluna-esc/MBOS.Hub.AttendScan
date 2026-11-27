using AttendScan.Application.Services;

namespace AttendScan.Tests;

public class PersonImportTests
{
    /// <summary>
    /// Lista real de delegados. No se copia al repositorio (tiene datos personales): si el archivo
    /// no está en esta máquina, la prueba no hace nada. Se puede cambiar con ATTENDSCAN_NAMES_XLSX.
    /// </summary>
    private static readonly string RealListPath =
        Environment.GetEnvironmentVariable("ATTENDSCAN_NAMES_XLSX") ?? @"X:\explicacion sistema\AttendScan\names_revisado.xlsx";

    [Theory]
    // Nombres inventados: el repositorio es público, aquí no va nadie de la lista real.
    [InlineData("Pérez Gómez Juan Carlos", "Juan Carlos Pérez Gómez")]
    [InlineData("Sánchez José", "sanchez  jose")]
    [InlineData("Rossi Félix", "Rossi, Felix")]
    public void Same_person_written_differently_has_the_same_key(string a, string b)
    {
        Assert.Equal(PersonNames.Key(a), PersonNames.Key(b));
    }

    [Theory]
    [InlineData("Rojas Vega Andrés", "Rojas Vega Andrea")]
    [InlineData("Torres Torres Luis", "Torres Luis")]
    public void Different_people_have_different_keys(string a, string b)
    {
        Assert.NotEqual(PersonNames.Key(a), PersonNames.Key(b));
    }

    [Fact]
    public void Full_name_is_paternal_maternal_given()
    {
        Assert.Equal("Pérez Pérez Juan Carlos", PersonNames.FullName("Pérez", "Pérez", "Juan Carlos"));
        Assert.Equal("Rojas Ana", PersonNames.FullName("Rojas", null, "Ana"));
        Assert.Equal("de la Vega Ana", PersonNames.FullName(" de  la Vega ", "  ", "Ana\n"));
    }

    [Fact]
    public void Real_list_reads_244_people_without_errors_or_duplicates()
    {
        if (!File.Exists(RealListPath))
        {
            return;
        }

        using var stream = File.OpenRead(RealListPath);
        var rows = PersonExcelReader.Read(stream);

        Assert.Equal(244, rows.Count);
        Assert.All(rows, row => Assert.Empty(row.Errors));

        var keys = rows.Select(r => PersonNames.Key(PersonNames.FullName(r.PaternalSurname!, r.MaternalSurname, r.GivenNames!))).ToList();
        Assert.Equal(keys.Count, keys.Distinct().Count());

        // Cada fila con sus columnas en su lugar. Sin nombrar a nadie: el repositorio es público.
        Assert.Equal(rows.Count, rows.Select(r => r.ListNumber).Distinct().Count());
        Assert.Equal(1, rows[0].ListNumber);
        Assert.All(rows, r =>
        {
            Assert.False(string.IsNullOrWhiteSpace(r.PaternalSurname));
            Assert.False(string.IsNullOrWhiteSpace(r.GivenNames));
            Assert.False(string.IsNullOrWhiteSpace(r.District));
            Assert.False(string.IsNullOrWhiteSpace(r.DelegateType));
            Assert.True(r.Gender is null or "M" or "F");
            Assert.True(r.Age is null or (>= 1 and <= 120));
        });
        Assert.Equal(28, rows.Count(r => r.MaternalSurname is null)); // los de un solo apellido
    }

    [Fact]
    public void Original_list_without_split_columns_is_rejected()
    {
        var original = Path.Combine(Path.GetDirectoryName(RealListPath)!, "corregido_MBOS_Cuadro_general_delegados_2026 (1).xlsx");
        if (!File.Exists(original))
        {
            return;
        }

        using var stream = File.OpenRead(original);
        Assert.Throws<InvalidDataException>(() => PersonExcelReader.Read(stream));
    }
}
