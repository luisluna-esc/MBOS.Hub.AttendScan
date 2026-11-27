using AttendScan.core.Entities.ResponseApi.DisplayFormat;

namespace AttendScan.Application.Interfaces.Dashboard;

public interface IDashboardService
{
    Task<ResponseGetObject> GetSummary(long? eventSessionId);
}
