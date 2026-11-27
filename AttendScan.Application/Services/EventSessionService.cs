using System.Net;
using AttendScan.Application.Dtos.Events;
using AttendScan.Application.Interfaces.Events;
using AttendScan.Application.Interfaces.Security;
using AttendScan.core.Configuration;
using AttendScan.core.Entities.Events;
using AttendScan.core.Entities.ResponseApi.Details;
using AttendScan.core.Entities.ResponseApi.DisplayFormat;
using AttendScan.core.Interfaces;
using AttendScan.core.QueryFilters.Events;
using AttendScan.core.QueryFilters.Pagination;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AttendScan.Application.Services;

public class EventSessionService : IEventSessionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly PaginationOptions _paginationOptions;
    private readonly IValidator<EventSessionDto> _validator;
    private readonly ICurrentUserService _currentUserService;

    public EventSessionService(IUnitOfWork unitOfWork, PaginationOptions paginationOptions, IValidator<EventSessionDto> validator,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _paginationOptions = paginationOptions;
        _validator = validator;
        _currentUserService = currentUserService;
    }

    public async Task<ResponseGetObject> GetAllEventSessions(PaginationQueryFilter paginationQueryFilter, EventSessionQueryFilter eventSessionQueryFilter)
    {
        var pageSize = paginationQueryFilter.PageSize > 0 ? paginationQueryFilter.PageSize : _paginationOptions.InitialPageSize;
        var pageNumber = paginationQueryFilter.PageNumber > 0 ? paginationQueryFilter.PageNumber : _paginationOptions.InitialPageNumber;

        var query = _unitOfWork.Repository<EventSession>().Query();

        if (eventSessionQueryFilter.EventId.HasValue)
        {
            query = query.Where(s => s.EventId == eventSessionQueryFilter.EventId.Value);
        }

        if (eventSessionQueryFilter.EventSessionId.HasValue)
        {
            query = query.Where(s => s.EventSessionId == eventSessionQueryFilter.EventSessionId.Value);
        }

        if (eventSessionQueryFilter.IsOpen.HasValue)
        {
            query = query.Where(s => s.IsOpen == eventSessionQueryFilter.IsOpen.Value);
        }

        if (!string.IsNullOrWhiteSpace(paginationQueryFilter.SearchCriteria))
        {
            var search = paginationQueryFilter.SearchCriteria.Trim().ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(search));
        }

        var totalRecords = await query.CountAsync();

        var attendances = _unitOfWork.Repository<Attendance>().Query();
        var sessions = await query
            .OrderBy(s => s.SessionDate)
            .ThenBy(s => s.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new EventSessionDto
            {
                EventSessionId = s.EventSessionId,
                EventId = s.EventId,
                Name = s.Name,
                SessionDate = s.SessionDate,
                IsOpen = s.IsOpen,
                AttendanceCount = attendances.Count(a => a.EventSessionId == s.EventSessionId),
                RegistrationDate = s.RegistrationDate
            })
            .ToListAsync();

        return new ResponseGetObject
        {
            Data = new PagedResult<EventSessionDto>
            {
                Items = sessions,
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            Messages = [],
            StatusCode = HttpStatusCode.OK
        };
    }

    public async Task<ResponsePost> InsertEventSession(EventSessionDto eventSessionDto)
    {
        var errors = await Validate(eventSessionDto);
        if (errors.Count > 0)
        {
            return new ResponsePost { Id = 0, Messages = errors.ToArray(), StatusCode = HttpStatusCode.BadRequest };
        }

        var entity = new EventSession
        {
            EventId = eventSessionDto.EventId,
            Name = eventSessionDto.Name.Trim(),
            SessionDate = eventSessionDto.SessionDate.Date,
            IsOpen = eventSessionDto.IsOpen,
            RegistrationDate = DateTime.UtcNow,
            AppUserId = _currentUserService.AppUserId
        };

        await _unitOfWork.Repository<EventSession>().AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        return new ResponsePost
        {
            Id = entity.EventSessionId,
            Messages = [new Message { Type = MessageType.Success, Description = "Session created successfully." }],
            StatusCode = HttpStatusCode.Created
        };
    }

    public async Task<ResponsePost> UpdateEventSession(long id, EventSessionDto eventSessionDto)
    {
        var errors = await Validate(eventSessionDto);
        if (errors.Count > 0)
        {
            return new ResponsePost { Id = id, Messages = errors.ToArray(), StatusCode = HttpStatusCode.BadRequest };
        }

        var repository = _unitOfWork.Repository<EventSession>();
        var entity = await repository.GetByIdAsync(id);

        if (entity is null)
        {
            return NotFound(id);
        }

        // EventId no se cambia: la asistencia ya tomada pertenece a este evento.
        entity.Name = eventSessionDto.Name.Trim();
        entity.SessionDate = eventSessionDto.SessionDate.Date;
        entity.IsOpen = eventSessionDto.IsOpen;

        repository.Update(entity);
        await _unitOfWork.SaveChangesAsync();

        return new ResponsePost
        {
            Id = entity.EventSessionId,
            Messages = [new Message { Type = MessageType.Success, Description = "Session updated successfully." }],
            StatusCode = HttpStatusCode.OK
        };
    }

    public async Task<ResponsePost> DeleteEventSession(long id)
    {
        var repository = _unitOfWork.Repository<EventSession>();
        var entity = await repository.GetByIdAsync(id);

        if (entity is null)
        {
            return NotFound(id);
        }

        // Con asistencia registrada no se borra: se cierra la sesión en su lugar.
        if (await _unitOfWork.Repository<Attendance>().Query().AnyAsync(a => a.EventSessionId == id))
        {
            return new ResponsePost
            {
                Id = id,
                Messages = [new Message { Type = MessageType.Error, Description = "The session has attendance records and cannot be deleted. Close it instead." }],
                StatusCode = HttpStatusCode.Conflict
            };
        }

        repository.Remove(entity);
        await _unitOfWork.SaveChangesAsync();

        return new ResponsePost
        {
            Id = id,
            Messages = [new Message { Type = MessageType.Success, Description = "Session deleted successfully." }],
            StatusCode = HttpStatusCode.OK
        };
    }

    private async Task<List<Message>> Validate(EventSessionDto eventSessionDto)
    {
        var validationResult = await _validator.ValidateAsync(eventSessionDto);
        var errors = validationResult.Errors
            .Select(e => new Message { Type = MessageType.Error, Description = e.ErrorMessage })
            .ToList();

        if (eventSessionDto.EventId > 0 &&
            !await _unitOfWork.Repository<Event>().Query().AnyAsync(e => e.EventId == eventSessionDto.EventId))
        {
            errors.Add(new Message { Type = MessageType.Error, Description = "EventId does not reference an existing event." });
        }

        return errors;
    }

    private static ResponsePost NotFound(long id) => new()
    {
        Id = id,
        Messages = [new Message { Type = MessageType.Error, Description = "Session not found." }],
        StatusCode = HttpStatusCode.NotFound
    };
}
