using System.Security.Cryptography;

namespace AttendScan.Application.Services;

/// <summary>
/// Código que va impreso (en QR y en texto) en cada carnet. Formato: <c>AS1-XXXX-XXXX-C</c>
/// <list type="bullet">
/// <item><c>AS1</c>: identifica a AttendScan, versión 1 del formato. Un QR que no empiece así no es nuestro.</item>
/// <item><c>XXXX-XXXX</c>: 8 caracteres al azar (32^8 ≈ 1,1 billones de combinaciones), no se pueden adivinar ni seguir en secuencia.</item>
/// <item><c>C</c>: dígito verificador (Luhn mod 32): detecta cualquier carácter mal escrito y casi todo par de caracteres invertidos.</item>
/// </list>
/// Alfabeto Crockford base32: sin I, L, O ni U, así nadie confunde 1/I/L ni 0/O al leerlo o
/// tipearlo; al tipear se aceptan igual (I/L → 1, O → 0). Solo mayúsculas, dígitos y guion: el
/// QR queda en modo alfanumérico, que es el más chico y el más fácil de leer.
///
/// IMPORTANTE: los carnets impresos no se pueden cambiar. No modificar Alphabet, Prefix ni el
/// cálculo del verificador; un formato nuevo va con otro prefijo (AS2) y AS1 se sigue aceptando.
/// </summary>
public static class CardCodes
{
    public const string Prefix = "AS1";
    public const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    private const int PayloadLength = 8;
    private const int GroupLength = 4;

    /// <summary>Largo del código con guiones: "AS1-XXXX-XXXX-C".</summary>
    public const int Length = 15;

    /// <summary>Código nuevo al azar (criptográficamente seguro), ya con su verificador.</summary>
    public static string Generate()
    {
        var payload = new char[PayloadLength];
        for (var i = 0; i < PayloadLength; i++)
        {
            payload[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return Format(new string(payload));
    }

    /// <summary>
    /// Lo leído del QR (o tipeado) en su forma oficial "AS1-XXXX-XXXX-C", o null si no es un
    /// código válido nuestro (otro QR, un carácter mal, verificador que no cuadra).
    /// </summary>
    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var compact = new string(input
            .Where(c => c != '-' && !char.IsWhiteSpace(c))
            .Select(c => char.ToUpperInvariant(c) switch
            {
                'O' => '0',
                'I' or 'L' => '1',
                var upper => upper,
            })
            .ToArray());

        if (compact.Length != Prefix.Length + PayloadLength + 1 || !compact.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return null;
        }

        var body = compact[Prefix.Length..];
        if (body.Any(c => Alphabet.IndexOf(c) < 0))
        {
            return null;
        }

        var payload = body[..PayloadLength];
        return CheckCharacter(payload) == body[PayloadLength] ? Format(payload) : null;
    }

    public static bool IsValid(string? input) => Normalize(input) is not null;

    private static string Format(string payload) =>
        $"{Prefix}-{payload[..GroupLength]}-{payload[GroupLength..]}-{CheckCharacter(payload)}";

    /// <summary>Luhn mod N (N = 32) sobre los 8 caracteres al azar.</summary>
    internal static char CheckCharacter(string payload)
    {
        var n = Alphabet.Length;
        var factor = 2;
        var sum = 0;

        for (var i = payload.Length - 1; i >= 0; i--)
        {
            var addend = factor * Alphabet.IndexOf(payload[i]);
            factor = factor == 2 ? 1 : 2;
            sum += addend / n + addend % n;
        }

        return Alphabet[(n - sum % n) % n];
    }
}
