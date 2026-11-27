using AttendScan.core.Entities.People;
using Microsoft.EntityFrameworkCore;

namespace AttendScan.Application.Services;

internal static class CardCodeGenerator
{
    /// <summary>
    /// Intentos para conseguir un código que no exista. Con 32^8 combinaciones, que choque uno solo
    /// ya es casi imposible; si fallan todos, algo anda mal y es mejor no crear la persona.
    /// </summary>
    private const int MaxAttempts = 5;

    /// <summary>Código nuevo que no está en la base ni en <paramref name="taken"/> (los ya repartidos en este lote).</summary>
    public static async Task<string?> NextUnique(IQueryable<Person> persons, ISet<string> taken)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var code = CardCodes.Generate();
            if (!taken.Contains(code) && !await persons.AnyAsync(p => p.CardCode == code))
            {
                taken.Add(code);
                return code;
            }
        }

        return null;
    }
}
