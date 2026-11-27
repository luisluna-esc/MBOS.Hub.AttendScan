using AttendScan.Application.Dtos.Security;
using AttendScan.core.Entities.ResponseApi.DisplayFormat;
using AttendScan.core.QueryFilters.Pagination;
using AttendScan.core.QueryFilters.Security;

namespace AttendScan.Application.Interfaces.Security;

public interface IPermissionService
{
    Task<ResponseGetObject> GetAllPermissions(PaginationQueryFilter paginationQueryFilter, PermissionQueryFilter permissionQueryFilter);
    Task<ResponsePost> InsertPermission(PermissionDto permissionDto);
    Task<ResponsePost> UpdatePermission(long id, PermissionDto permissionDto);
    Task<ResponsePost> DeletePermission(long id);
}