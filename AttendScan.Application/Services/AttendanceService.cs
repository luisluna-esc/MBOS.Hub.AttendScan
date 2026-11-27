using System.Net;
using AttendScan.Application.Dtos.Events;
using AttendScan.Application.Interfaces.Events;
using AttendScan.Application.Interfaces.Security;
using AttendScan.core.Configuration;
using AttendScan.core.Entities.Events;
using AttendScan.core.Entities.People;
using AttendScan.core.Entities.ResponseApi.Details;
using AttendScan.core.Entities.ResponseApi.DisplayFormat;
using AttendScan.core.Interfaces;
using AttendScan.core.QueryFilters.Events;
using AttendScan.core.QueryFilters.Pagination;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AttendScan.Application.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly PaginationOptions _paginationOptions;
    private readonly IValidator<ScanRequestDto> _validator;
    private readonly ICurrentUserService _currentUserService;

    public AttendanceService(IUnitOfWork unitOfWork, PaginationOptions paginationOptions, IValidator<ScanRequestDto> validator,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _paginationOptions = paginationOptions;
        _validator = validator;
        _currentUserService = currentUserService;
    }

    public async Task<ResponseGetObject> GetAllAttendances(PaginationQueryFilter paginationQueryFilter, AttendanceQueryFilter attendanceQueryFilter)
    {
        var pageSize = paginationQueryFilter.PageSize > 0 ? paginationQueryFilter.PageSize : _paginationOptions.InitialPageSize;
        var pageNumber = paginationQueryFilter.PageNumber > 0 ? paginationQueryFilter.PageNumber : _paginationOptions.InitialPageNumber;

        var query =
            from a in _unitOfWork.Repository<Attendance>().Query()
            join p in _unitOfWork.Repository<Person>().Query() on a.PersonId equals p.PersonId
            select new { a, p };

        if (attendanceQueryFilter.EventSessionId.HasValue)
        {
            query = query.Where(x => x.a.EventSessionId == attendanceQueryFilter.EventSessionId.Value);
        }

        if (attendanceQueryFilter.PersonId.HasValue)
        {
            query = query.Where(x => x.a.PersonId == attendanceQueryFilter.PersonId.Value);
        }

        if (!string.IsNullOrWhiteSpace(paginationQueryFilter.SearchCriteria))
        {
            var search = paginationQueryFilter.SearchCriteria.Trim().ToLower();
            var searchCode = CardCodes.Normalize(paginationQueryFilter.SearchCriteria);
            query = query.Where(x => x.p.FullName.ToLower().Contains(search) || x.p.CardCode == searchCode);
        }

        var totalRecords = await query.CountAsync();

        // Lo último escaneado primero: es lo que quiere ver quien está en la puerta.
        var items = await query
            .OrderByDescending(x => x.a.ScannedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AttendanceDto
            {
                AttendanceId = x.a.AttendanceId,
                EventSessionId = x.a.EventSessionId,
                PersonId = x.p.PersonId,
                CardCode = x.p.CardCode,
                FullName = x.p.FullName,
                District = x.p.District,
                DelegateType = x.p.DelegateType,
                ScannedAt = x.a.ScannedAt
            })
            .ToListAsync();

        return new ResponseGetObject
        {
            Data = new PagedResult<AttendanceDto>
            {
                Items = items,
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            Messages = [],
            StatusCode = HttpStatusCode.OK
        };
    }

    /// <summary>
    /// Registra la asistencia del carnet escaneado en la sesión. Un carnet desconocido, un QR ajeno
    /// o una persona ya registrada NO son errores HTTP: responden 200 con su Status, para que el
    /// escáner muestre el aviso y siga escaneando. Solo la sesión inexistente o cerrada es error.
    /// </summary>
    public async Task<ResponseGetObject> Scan(ScanRequestDto scanRequestDto)
    {
        var validationResult = await _validator.ValidateAsync(scanRequestDto);
        if (!validationResult.IsValid)
        {
            return Error(HttpStatusCode.BadRequest, validationResult.Errors.Select(e => e.ErrorMessage).ToArray());
        }

        var sessionId = scanRequestDto.EventSessionId;
        var session = await _unitOfWork.Repository<EventSession>().Query()
            .FirstOrDefaultAsync(s => s.EventSessionId == sessionId);
        if (session is null)
        {
            return Error(HttpStatusCode.NotFound, "Session not found.");
        }

        var eventIsActive = await _unitOfWork.Repository<Event>().Query()
            .AnyAsync(e => e.EventId == session.EventId && e.IsActive);
        if (!session.IsOpen || !eventIsActive)
        {
            return Error(HttpStatusCode.Conflict, "The session is closed.");
        }

        // 1. ¿Es un código nuestro? (prefijo, alfabeto y dígito verificador)
        var cardCode = CardCodes.Normalize(scanRequestDto.Code);
        if (cardCode is null)
        {
            return await Result(new ScanResultDto { Status = ScanStatus.InvalidCode }, sessionId);
        }

        // 2. ¿De quién es el carnet?
        var person = await _unitOfWork.Repository<Person>().Query()
            .FirstOrDefaultAsync(p => p.CardCode == cardCode);
        if (person is null)
        {
            return await Result(new ScanResultDto { Status = ScanStatus.UnknownCard, CardCode = cardCode }, sessionId);
        }

        // 3. ¿Ya marcó en esta sesión? Entonces no se duplica: se avisa con la hora original.
        var existing = await FindAttendance(sessionId, person.PersonId);
        if (existing is not null)
        {
            return await Result(AlreadyRegistered(person, existing), sessionId);
        }

        // 4. Registrar.
        var attendance = new Attendance
        {
            EventSessionId = sessionId,
            PersonId = person.PersonId,
            ScannedAt = DateTime.UtcNow,
            AppUserId = _currentUserService.AppUserId
        };

        try
        {
            await _unitOfWork.Repository<Attendance>().AddAsync(attendance);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Dos celulares escanearon el mismo carnet a la vez: el índice único
            // (event_session_id, person_id) dejó pasar solo a uno. Para este es "ya registrado".
            existing = await FindAttendance(sessionId, person.PersonId);
            if (existing is null)
            {
                throw;
            }

            return await Result(AlreadyRegistered(person, existing), sessionId);
        }

        return await Result(new ScanResultDto
        {
            Status = ScanStatus.Registered,
            PersonId = person.PersonId,
            FullName = person.FullName,
            CardCode = person.CardCode,
            ScannedAt = attendance.ScannedAt
        }, sessionId, HttpStatusCode.Created);
    }

    private Task<Attendance?> FindAttendance(long sessionId, long personId) =>
        _unitOfWork.Repository<Attendance>().Query()
            .FirstOrDefaultAsync(a => a.EventSessionId == sessionId && a.PersonId == personId);

    private static ScanResultDto AlreadyRegistered(Person person, Attendance existing) => new()
    {
        Status = ScanStatus.AlreadyRegistered,
        PersonId = person.PersonId,
        FullName = person.FullName,
        CardCode = person.CardCode,
        ScannedAt = existing.ScannedAt
    };

    private async Task<ResponseGetObject> Result(ScanResultDto result, long sessionId, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        result.AttendanceCount = await _unitOfWork.Repository<Attendance>().Query()
            .CountAsync(a => a.EventSessionId == sessionId);

        return new ResponseGetObject { Data = result, Messages = [], StatusCode = statusCode };
    }

    private static ResponseGetObject Error(HttpStatusCode statusCode, params string[] descriptions) => new()
    {
        Data = new object(),
        Messages = descriptions.Select(d => new Message { Type = MessageType.Error, Description = d }).ToArray(),
        StatusCode = statusCode
    };
}
