using System.Text.RegularExpressions;
using AttendScan.Application.Services;

namespace AttendScan.Tests;

/// <summary>
/// El código del carnet va impreso y no se puede corregir: estas pruebas fijan el formato y el
/// dígito verificador. Si alguna falla después de un cambio, los carnets ya impresos podrían dejar
/// de leerse — no "arreglar" la prueba, revertir el cambio.
/// </summary>
public partial class CardCodesTests
{
    [GeneratedRegex("^AS1-[0-9A-HJKMNP-TV-Z]{4}-[0-9A-HJKMNP-TV-Z]{4}-[0-9A-HJKMNP-TV-Z]$")]
    private static partial Regex OfficialFormat();

    // Valores calculados con una implementación independiente (Python) del mismo Luhn mod 32.
    // Fijan el algoritmo: los carnets impresos dependen de que estos resultados no cambien nunca.
    [Theory]
    [InlineData("AS1-0000-0000-0")]
    [InlineData("AS1-ZZZZ-ZZZZ-8")]
    [InlineData("AS1-7KQ3-M9XD-R")]
    [InlineData("AS1-ABCD-EFGH-V")]
    [InlineData("AS1-1234-5678-8")]
    public void Known_codes_are_valid(string code)
    {
        Assert.Equal(code, CardCodes.Normalize(code));
    }

    [Theory]
    [InlineData("AS1-0000-0000-")]
    [InlineData("AS1-ZZZZ-ZZZZ-")]
    [InlineData("AS1-7KQ3-M9XD-")]
    public void Only_one_check_character_is_valid(string withoutCheck)
    {
        var valid = CardCodes.Alphabet.Where(c => CardCodes.IsValid(withoutCheck + c)).ToList();
        Assert.Single(valid);
    }

    [Fact]
    public void Generated_codes_have_the_official_format_and_are_valid()
    {
        for (var i = 0; i < 10_000; i++)
        {
            var code = CardCodes.Generate();
            Assert.Matches(OfficialFormat(), code);
            Assert.Equal(CardCodes.Length, code.Length);
            Assert.Equal(code, CardCodes.Normalize(code));
        }
    }

    [Fact]
    public void Generated_codes_do_not_repeat()
    {
        var codes = Enumerable.Range(0, 100_000).Select(_ => CardCodes.Generate()).ToList();
        Assert.Equal(codes.Count, codes.Distinct().Count());
    }

    [Fact]
    public void Any_single_wrong_character_is_rejected()
    {
        for (var n = 0; n < 200; n++)
        {
            var code = CardCodes.Generate();
            for (var position = 0; position < code.Length; position++)
            {
                if (code[position] == '-')
                {
                    continue;
                }

                foreach (var replacement in CardCodes.Alphabet.Where(c => c != code[position]))
                {
                    var typo = code[..position] + replacement + code[(position + 1)..];
                    Assert.False(CardCodes.IsValid(typo), $"{typo} (from {code}) should be invalid");
                }
            }
        }
    }

    [Theory]
    [InlineData("as1-7kq3-m9xd-r")]          // minúsculas
    [InlineData("AS17KQ3M9XDR")]             // sin guiones
    [InlineData("  AS1 7KQ3 M9XD R  ")]      // con espacios
    [InlineData("AS1-7KQ3-M9XD-R\n")]        // salto de línea que agregan algunos lectores
    public void Hand_typed_variants_normalize_to_the_official_code(string input)
    {
        Assert.Equal("AS1-7KQ3-M9XD-R", CardCodes.Normalize(input));
    }

    [Fact]
    public void Confusable_letters_are_read_as_digits()
    {
        // O se lee 0; I y L se leen 1 (alfabeto Crockford).
        Assert.Equal("AS1-0000-0000-0", CardCodes.Normalize("AS1-OOOO-oooo-O"));
        Assert.Equal("AS1-1234-5678-8", CardCodes.Normalize("ASI-I234-5678-8"));
        Assert.Equal("AS1-1234-5678-8", CardCodes.Normalize("ASl-L234-5678-8"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("https://example.com/AS1-7KQ3-M9XD-R")] // URL u otro QR cualquiera
    [InlineData("AS2-7KQ3-M9XD-R")]                     // otro prefijo
    [InlineData("AS1-7KQ3-M9XD")]                       // sin verificador
    [InlineData("AS1-7KQ3-M9XD-RR")]                    // un carácter de más
    [InlineData("AS1-7KQ3-M9XU-R")]                     // U no existe en el alfabeto
    [InlineData("AS1-7KQ3-M9XD-S")]                     // verificador equivocado
    [InlineData("AS1-K7Q3-M9XD-R")]                     // dos caracteres vecinos invertidos (error típico al tipear)
    [InlineData("AS1-7KQ3-M9DX-R")]
    [InlineData("AS1-7Q3K-M9XD-R")]
    // Límite conocido del Luhn: no detecta grupos completos intercambiados (AS1-M9XD-7KQ3-R). Al
    // escanear no pasa (el QR tiene su propia corrección de errores), y al tipear, ese código casi
    // seguro no es de nadie: el escáner responde "carnet desconocido", nunca registra a otra persona.
    public void Foreign_or_damaged_codes_are_rejected(string? input)
    {
        Assert.Null(CardCodes.Normalize(input));
    }
}
