using System.IO.Compression;
using AttendScan.Application.Interfaces.People;
using AttendScan.core.Entities.People;
using AttendScan.core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AttendScan.Application.Services;

public class PersonQrService : IPersonQrService
{
    private readonly IUnitOfWork _unitOfWork;

    public PersonQrService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<QrFile?> GetQr(long personId)
    {
        var person = await _unitOfWork.Repository<Person>().Query().FirstOrDefaultAsync(p => p.PersonId == personId);
        return person is null ? null : new QrFile(CardQr.Png(person.CardCode), FileName(person), "image/png");
    }

    public async Task<QrFile> GetAllQrZip()
    {
        var persons = await _unitOfWork.Repository<Person>().Query()
            .OrderBy(p => p.ListNumber == null)
            .ThenBy(p => p.ListNumber)
            .ThenBy(p => p.PaternalSurname)
            .ThenBy(p => p.MaternalSurname)
            .ThenBy(p => p.GivenNames)
            .ToListAsync();

        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var person in persons)
            {
                var entry = zip.CreateEntry(FileName(person), CompressionLevel.Fastest);
                await using var stream = entry.Open();
                await stream.WriteAsync(CardQr.Png(person.CardCode));
            }
        }

        return new QrFile(buffer.ToArray(), "carnets-qr.zip", "application/zip");
    }

    /// <summary>"001 Pérez Gómez Juan Carlos AS1-83Y5-08SA-D.png": ordena igual que la lista y se reconoce a simple vista.</summary>
    private static string FileName(Person person)
    {
        var number = person.ListNumber is { } n ? $"{n:000} " : string.Empty;
        var name = string.Concat(person.FullName.Select(c => Path.GetInvalidFileNameChars().Contains(c) || c == '"' ? '_' : c));
        return $"{number}{name} {person.CardCode}.png";
    }
}
