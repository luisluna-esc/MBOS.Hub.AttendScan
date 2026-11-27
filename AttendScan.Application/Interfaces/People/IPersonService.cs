using AttendScan.Application.Dtos.People;
using AttendScan.core.Entities.ResponseApi.DisplayFormat;
using AttendScan.core.QueryFilters.Pagination;
using AttendScan.core.QueryFilters.People;

namespace AttendScan.Application.Interfaces.People;

/// <summary>Sin Delete a propósito: borrar una persona anularía su carnet, y los carnets nunca se anulan.</summary>
public interface IPersonService
{
    Task<ResponseGetObject> GetAllPersons(PaginationQueryFilter paginationQueryFilter, PersonQueryFilter personQueryFilter);
    Task<ResponsePost> InsertPerson(PersonDto personDto);
    Task<ResponsePost> UpdatePerson(long id, PersonDto personDto);
}
