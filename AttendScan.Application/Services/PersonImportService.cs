using System.Net;
using AttendScan.Application.Dtos.People;
using AttendScan.Application.Interfaces.People;
using AttendScan.Application.Interfaces.Security;
using AttendScan.core.Entities.People;
using AttendScan.core.Entities.ResponseApi.Details;
using AttendScan.core.Entities.ResponseApi.DisplayFormat;
using AttendScan.core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AttendScan.Application.Services;

public class PersonImportService : IPersonImportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public PersonImportService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ResponseGetObject> Import(Stream excel, bool commit)
    {
        List<PersonExcelRow> rows;
        try
        {
            rows = PersonExcelReader.Read(excel);
        }
        catch (InvalidDataException ex)
        {
            return Error(ex.Message);
        }
        catch (Exception)
        {
            return Error("The file could not be read. Upload the Excel (.xlsx) of the list.");
        }

        if (rows.Count == 0)
        {
            return Error("The Excel has no people.");
        }

        var repository = _unitOfWork.Repository<Person>();
        // Personas ya cargadas, por clave de nombre: volver a subir el mismo Excel no duplica a nadie.
        var existing = (await repository.Query().Select(p => new { p.FullName, p.CardCode }).ToListAsync())
            .GroupBy(p => PersonNames.Key(p.FullName))
            .ToDictionary(g => g.Key, g => g.First().CardCode);

        var result = new PersonImportResultDto { TotalRows = rows.Count };
        var seenInFile = new Dictionary<string, int>();
        var toCreate = new List<(PersonExcelRow Row, PersonImportRowDto Dto)>();

        foreach (var row in rows)
        {
            var fullName = PersonNames.FullName(row.PaternalSurname ?? string.Empty, row.MaternalSurname, row.GivenNames ?? string.Empty);
            var dto = new PersonImportRowDto
            {
                RowNumber = row.RowNumber,
                FullName = fullName,
                District = row.District,
                DelegateType = row.DelegateType,
                Errors = [.. row.Errors]
            };
            CheckLengths(row, dto.Errors);

            var key = PersonNames.Key(fullName);
            if (dto.Errors.Count == 0 && seenInFile.TryGetValue(key, out var firstRow))
            {
                dto.Errors.Add($"Repetido: es la misma persona de la fila {firstRow}. Una persona, un carnet: quite una de las dos filas.");
            }
            else if (dto.Errors.Count == 0)
            {
                seenInFile[key] = row.RowNumber;
            }

            if (dto.Errors.Count > 0)
            {
                dto.Status = PersonImportStatus.Error;
            }
            else if (existing.TryGetValue(key, out var cardCode))
            {
                dto.Status = PersonImportStatus.Existing;
                dto.CardCode = cardCode;
            }
            else
            {
                dto.Status = PersonImportStatus.New;
                toCreate.Add((row, dto));
            }

            result.Rows.Add(dto);
        }

        result.NewCount = result.Rows.Count(r => r.Status == PersonImportStatus.New);
        result.ExistingCount = result.Rows.Count(r => r.Status == PersonImportStatus.Existing);
        result.ErrorCount = result.Rows.Count(r => r.Status == PersonImportStatus.Error);

        // Todo o nada: con un solo error no se importa ninguna fila.
        if (!commit || result.ErrorCount > 0 || toCreate.Count == 0)
        {
            return new ResponseGetObject { Data = result, Messages = [], StatusCode = HttpStatusCode.OK };
        }

        await _unitOfWork.InTransactionAsync(async () =>
        {
            var taken = new HashSet<string>();
            var appUserId = _currentUserService.AppUserId;
            foreach (var (row, dto) in toCreate)
            {
                var cardCode = await CardCodeGenerator.NextUnique(repository.Query(), taken)
                    ?? throw new InvalidOperationException("Could not generate a unique card code.");
                await repository.AddAsync(new Person
                {
                    CardCode = cardCode,
                    ListNumber = row.ListNumber,
                    Region = row.Region,
                    District = row.District,
                    Church = row.Church,
                    PaternalSurname = row.PaternalSurname!,
                    MaternalSurname = row.MaternalSurname,
                    GivenNames = row.GivenNames!,
                    FullName = dto.FullName,
                    Position = row.Position,
                    DelegateType = row.DelegateType,
                    Age = row.Age,
                    Gender = row.Gender,
                    RegistrationDate = DateTime.UtcNow,
                    AppUserId = appUserId
                });
                dto.CardCode = cardCode;
            }

            await _unitOfWork.SaveChangesAsync();
            return true;
        });

        result.Committed = true;
        return new ResponseGetObject
        {
            Data = result,
            Messages = [new Message { Type = MessageType.Success, Description = $"{result.NewCount} people imported." }],
            StatusCode = HttpStatusCode.OK
        };
    }

    private static void CheckLengths(PersonExcelRow row, List<string> errors)
    {
        void Max(string? value, int max, string label)
        {
            if (value is not null && value.Length > max)
            {
                errors.Add($"{label} supera los {max} caracteres.");
            }
        }

        Max(row.PaternalSurname, 80, "Apellido paterno");
        Max(row.MaternalSurname, 80, "Apellido materno");
        Max(row.GivenNames, 120, "Nombres");
        Max(row.Region, 50, "Región");
        Max(row.District, 100, "Distrito");
        Max(row.Church, 150, "Iglesia");
        Max(row.Position, 200, "Función");
        Max(row.DelegateType, 50, "Tipo de delegado");
        if (row.Age is < 1 or > 120)
        {
            errors.Add("Edad fuera de rango (1 a 120).");
        }
    }

    private static ResponseGetObject Error(string description) => new()
    {
        Data = new object(),
        Messages = [new Message { Type = MessageType.Error, Description = description }],
        StatusCode = HttpStatusCode.BadRequest
    };
}
