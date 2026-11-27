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

public class EventService : IEventService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly PaginationOptions _paginationOptions;
    private readonly IValidator<EventDto> _validator;
    private readonly ICurrentUserService _currentUserService;

    public EventService(IUnitOfWork unitOfWork, PaginationOptions paginationOptions, IValidator<EventDto> validator,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _paginationOptions = paginationOptions;
        _validator = validator;
        _currentUserService = currentUserService;
    }

    public async Task<ResponseGetObject> GetAllEvents(PaginationQueryFilter paginationQueryFilter, EventQueryFilter eventQueryFilter)
    {
        var pageSize = paginationQueryFilter.PageSize > 0 ? paginationQueryFilter.PageSize : _paginationOptions.InitialPageSize;
        var pageNumber = paginationQueryFilter.PageNumber > 0 ? paginationQueryFilter.PageNumber : _paginationOptions.InitialPageNumber;

        var query = _unitOfWork.Repository<Event>().Query();

        if (eventQueryFilter.EventId.HasValue)
        {
            query = query.Where(e => e.EventId == eventQueryFilter.EventId.Value);
        }

        if (eventQueryFilter.IsActive.HasValue)
        {
            query = query.Where(e => e.IsActive == eventQueryFilter.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(paginationQueryFilter.SearchCriteria))
        {
            var search = paginationQueryFilter.SearchCriteria.Trim().ToLower();
            query = query.Where(e => e.Name.ToLower().Contains(search));
        }

        var totalRecords = await query.CountAsync();

        var events = await query
            .OrderByDescending(e => e.StartDate)
            .ThenBy(e => e.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new ResponseGetObject
        {
            Data = new PagedResult<EventDto>
            {
                Items = events.Select(ToDto),
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            Messages = [],
            StatusCode = HttpStatusCode.OK
        };
    }

    public async Task<ResponsePost> InsertEvent(EventDto eventDto)
    {
        var errors = await Validate(eventDto);
        if (errors.Count > 0)
        {
            return new ResponsePost { Id = 0, Messages = errors.ToArray(), StatusCode = HttpStatusCode.BadRequest };
        }

        var entity = new Event
        {
            Name = eventDto.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(eventDto.Description) ? null : eventDto.Description.Trim(),
            StartDate = eventDto.StartDate.Date,
            EndDate = eventDto.EndDate?.Date,
            IsActive = eventDto.IsActive,
            RegistrationDate = DateTime.UtcNow,
            AppUserId = _currentUserService.AppUserId
        };

        await _unitOfWork.Repository<Event>().AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        return new ResponsePost
        {
            Id = entity.EventId,
            Messages = [new Message { Type = MessageType.Success, Description = "Event created successfully." }],
            StatusCode = HttpStatusCode.Created
        };
    }

    public async Task<ResponsePost> UpdateEvent(long id, EventDto eventDto)
    {
        var errors = await Validate(eventDto);
        if (errors.Count > 0)
        {
            return new ResponsePost { Id = id, Messages = errors.ToArray(), StatusCode = HttpStatusCode.BadRequest };
        }

        var repository = _unitOfWork.Repository<Event>();
        var entity = await repository.GetByIdAsync(id);

        if (entity is null)
        {
            return NotFound(id);
        }

        entity.Name = eventDto.Name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(eventDto.Description) ? null : eventDto.Description.Trim();
        entity.StartDate = eventDto.StartDate.Date;
        entity.EndDate = eventDto.EndDate?.Date;
        entity.IsActive = eventDto.IsActive;

        repository.Update(entity);
        await _unitOfWork.SaveChangesAsync();

        return new ResponsePost
        {
            Id = entity.EventId,
            Messages = [new Message { Type = MessageType.Success, Description = "Event updated successfully." }],
            StatusCode = HttpStatusCode.OK
        };
    }

    public async Task<ResponsePost> DeleteEvent(long id)
    {
        var repository = _unitOfWork.Repository<Event>();
        var entity = await repository.GetByIdAsync(id);

        if (entity is null)
        {
            return NotFound(id);
        }

        // Con sesiones no se borra: se perdería su asistencia. Se desactiva el evento en su lugar.
        if (await _unitOfWork.Repository<EventSession>().Query().AnyAsync(s => s.EventId == id))
        {
            return new ResponsePost
            {
                Id = id,
                Messages = [new Message { Type = MessageType.Error, Description = "The event has sessions and cannot be deleted. Deactivate it instead." }],
                StatusCode = HttpStatusCode.Conflict
            };
        }

        repository.Remove(entity);
        await _unitOfWork.SaveChangesAsync();

        return new ResponsePost
        {
            Id = id,
            Messages = [new Message { Type = MessageType.Success, Description = "Event deleted successfully." }],
            StatusCode = HttpStatusCode.OK
        };
    }

    private async Task<List<Message>> Validate(EventDto eventDto)
    {
        var validationResult = await _validator.ValidateAsync(eventDto);
        return validationResult.Errors
            .Select(e => new Message { Type = MessageType.Error, Description = e.ErrorMessage })
            .ToList();
    }

    private static ResponsePost NotFound(long id) => new()
    {
        Id = id,
        Messages = [new Message { Type = MessageType.Error, Description = "Event not found." }],
        StatusCode = HttpStatusCode.NotFound
    };

    private static EventDto ToDto(Event entity) => new()
    {
        EventId = entity.EventId,
        Name = entity.Name,
        Description = entity.Description,
        StartDate = entity.StartDate,
        EndDate = entity.EndDate,
        IsActive = entity.IsActive,
        RegistrationDate = entity.RegistrationDate
    };
}
