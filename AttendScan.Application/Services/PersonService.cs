using System.Net;
using AttendScan.Application.Dtos.People;
using AttendScan.Application.Interfaces.People;
using AttendScan.Application.Interfaces.Security;
using AttendScan.core.Configuration;
using AttendScan.core.Entities.People;
using AttendScan.core.Entities.ResponseApi.Details;
using AttendScan.core.Entities.ResponseApi.DisplayFormat;
using AttendScan.core.Interfaces;
using AttendScan.core.QueryFilters.Pagination;
using AttendScan.core.QueryFilters.People;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AttendScan.Application.Services;

public class PersonService : IPersonService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly PaginationOptions _paginationOptions;
    private readonly IValidator<PersonDto> _validator;
    private readonly ICurrentUserService _currentUserService;

    public PersonService(IUnitOfWork unitOfWork, PaginationOptions paginationOptions, IValidator<PersonDto> validator,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _paginationOptions = paginationOptions;
        _validator = validator;
        _currentUserService = currentUserService;
    }

    public async Task<ResponseGetObject> GetAllPersons(PaginationQueryFilter paginationQueryFilter, PersonQueryFilter personQueryFilter)
    {
        var pageSize = paginationQueryFilter.PageSize > 0 ? paginationQueryFilter.PageSize : _paginationOptions.InitialPageSize;
        var pageNumber = paginationQueryFilter.PageNumber > 0 ? paginationQueryFilter.PageNumber : _paginationOptions.InitialPageNumber;

        var query = _unitOfWork.Repository<Person>().Query();

        if (personQueryFilter.PersonId.HasValue)
        {
            query = query.Where(p => p.PersonId == personQueryFilter.PersonId.Value);
        }

        if (!string.IsNullOrWhiteSpace(personQueryFilter.CardCode))
        {
            var cardCode = CardCodes.Normalize(personQueryFilter.CardCode) ?? personQueryFilter.CardCode.Trim().ToUpperInvariant();
            query = query.Where(p => p.CardCode == cardCode);
        }

        if (!string.IsNullOrWhiteSpace(personQueryFilter.District))
        {
            query = query.Where(p => p.District == personQueryFilter.District.Trim());
        }

        if (!string.IsNullOrWhiteSpace(personQueryFilter.DelegateType))
        {
            query = query.Where(p => p.DelegateType == personQueryFilter.DelegateType.Trim());
        }

        if (!string.IsNullOrWhiteSpace(paginationQueryFilter.SearchCriteria))
        {
            var search = paginationQueryFilter.SearchCriteria.Trim().ToLower();
            var searchCode = CardCodes.Normalize(paginationQueryFilter.SearchCriteria);
            query = query.Where(p => p.FullName.ToLower().Contains(search)
                                     || (p.District != null && p.District.ToLower().Contains(search))
                                     || p.CardCode == searchCode);
        }

        var totalRecords = await query.CountAsync();

        var persons = await query
            .OrderBy(p => p.PaternalSurname)
            .ThenBy(p => p.MaternalSurname)
            .ThenBy(p => p.GivenNames)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new ResponseGetObject
        {
            Data = new PagedResult<PersonDto>
            {
                Items = persons.Select(ToDto),
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            Messages = [],
            StatusCode = HttpStatusCode.OK
        };
    }

    public async Task<ResponsePost> InsertPerson(PersonDto personDto)
    {
        Normalize(personDto);
        var errors = await Validate(personDto, null);
        if (errors.Count > 0)
        {
            return new ResponsePost { Id = 0, Messages = errors.ToArray(), StatusCode = HttpStatusCode.BadRequest };
        }

        var repository = _unitOfWork.Repository<Person>();
        var cardCode = await CardCodeGenerator.NextUnique(repository.Query(), new HashSet<string>());
        if (cardCode is null)
        {
            return new ResponsePost
            {
                Id = 0,
                Messages = [new Message { Type = MessageType.Error, Description = "Could not generate a unique card code. Try again." }],
                StatusCode = HttpStatusCode.InternalServerError
            };
        }

        // El CardCode del DTO se ignora siempre: solo el servidor lo genera.
        var person = new Person { CardCode = cardCode, RegistrationDate = DateTime.UtcNow, AppUserId = _currentUserService.AppUserId };
        Apply(personDto, person);

        await repository.AddAsync(person);
        await _unitOfWork.SaveChangesAsync();

        return new ResponsePost
        {
            Id = person.PersonId,
            Messages = [new Message { Type = MessageType.Success, Description = "Person created successfully." }],
            StatusCode = HttpStatusCode.Created
        };
    }

    public async Task<ResponsePost> UpdatePerson(long id, PersonDto personDto)
    {
        Normalize(personDto);
        var errors = await Validate(personDto, id);
        if (errors.Count > 0)
        {
            return new ResponsePost { Id = id, Messages = errors.ToArray(), StatusCode = HttpStatusCode.BadRequest };
        }

        var repository = _unitOfWork.Repository<Person>();
        var person = await repository.GetByIdAsync(id);

        if (person is null)
        {
            return new ResponsePost
            {
                Id = id,
                Messages = [new Message { Type = MessageType.Error, Description = "Person not found." }],
                StatusCode = HttpStatusCode.NotFound
            };
        }

        // CardCode no se toca: está impreso en el carnet y es permanente.
        Apply(personDto, person);

        repository.Update(person);
        await _unitOfWork.SaveChangesAsync();

        return new ResponsePost
        {
            Id = person.PersonId,
            Messages = [new Message { Type = MessageType.Success, Description = "Person updated successfully." }],
            StatusCode = HttpStatusCode.OK
        };
    }

    private async Task<List<Message>> Validate(PersonDto personDto, long? currentId)
    {
        var validationResult = await _validator.ValidateAsync(personDto);
        var errors = validationResult.Errors
            .Select(e => new Message { Type = MessageType.Error, Description = e.ErrorMessage })
            .ToList();

        if (errors.Count == 0)
        {
            // Una persona, un carnet: no se crea otra con el mismo nombre (aunque esté en otro orden o sin tildes).
            var key = PersonNames.Key(PersonNames.FullName(personDto.PaternalSurname, personDto.MaternalSurname, personDto.GivenNames));
            var others = await _unitOfWork.Repository<Person>().Query()
                .Where(p => p.PersonId != currentId)
                .Select(p => p.FullName)
                .ToListAsync();
            if (others.Any(name => PersonNames.Key(name) == key))
            {
                errors.Add(new Message { Type = MessageType.Error, Description = "A person with this name already exists." });
            }
        }

        return errors;
    }

    private static void Normalize(PersonDto dto)
    {
        dto.PaternalSurname = PersonNames.Clean(dto.PaternalSurname) ?? string.Empty;
        dto.MaternalSurname = PersonNames.Clean(dto.MaternalSurname);
        dto.GivenNames = PersonNames.Clean(dto.GivenNames) ?? string.Empty;
        dto.Region = PersonNames.Clean(dto.Region);
        dto.District = PersonNames.Clean(dto.District);
        dto.Church = PersonNames.Clean(dto.Church);
        dto.Position = PersonNames.Clean(dto.Position);
        dto.DelegateType = PersonNames.Clean(dto.DelegateType);
        dto.Gender = PersonNames.Clean(dto.Gender)?.ToUpperInvariant();
    }

    private static void Apply(PersonDto dto, Person person)
    {
        person.ListNumber = dto.ListNumber;
        person.Region = dto.Region;
        person.District = dto.District;
        person.Church = dto.Church;
        person.PaternalSurname = dto.PaternalSurname;
        person.MaternalSurname = dto.MaternalSurname;
        person.GivenNames = dto.GivenNames;
        person.FullName = PersonNames.FullName(dto.PaternalSurname, dto.MaternalSurname, dto.GivenNames);
        person.Position = dto.Position;
        person.DelegateType = dto.DelegateType;
        person.Age = dto.Age;
        person.Gender = dto.Gender;
    }

    internal static PersonDto ToDto(Person person) => new()
    {
        PersonId = person.PersonId,
        CardCode = person.CardCode,
        ListNumber = person.ListNumber,
        Region = person.Region,
        District = person.District,
        Church = person.Church,
        PaternalSurname = person.PaternalSurname,
        MaternalSurname = person.MaternalSurname,
        GivenNames = person.GivenNames,
        FullName = person.FullName,
        Position = person.Position,
        DelegateType = person.DelegateType,
        Age = person.Age,
        Gender = person.Gender,
        RegistrationDate = person.RegistrationDate
    };
}
