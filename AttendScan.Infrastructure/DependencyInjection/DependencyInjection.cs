using AttendScan.Application.Dtos.Events;
using AttendScan.Application.Dtos.People;
using AttendScan.Application.Dtos.Security;
using AttendScan.Application.Interfaces.Audit;
using AttendScan.Application.Interfaces.Events;
using AttendScan.Application.Interfaces.People;
using AttendScan.Application.Interfaces.Security;
using AttendScan.Application.Services;
using AttendScan.Application.Validators.Events;
using AttendScan.Application.Validators.People;
using AttendScan.Application.Validators.Security;
using AttendScan.core.Configuration;
using AttendScan.core.Interfaces;
using AttendScan.Infrastructure.Authorization;
using AttendScan.Infrastructure.Context;
using AttendScan.Infrastructure.Context.Core;
using AttendScan.Infrastructure.Interceptors;
using AttendScan.Infrastructure.Security;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AttendScan.Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Missing connection string 'DefaultConnection'. Set it with dotnet user-secrets or the ConnectionStrings__DefaultConnection environment variable.");

        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                   .AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>()));

        services.Configure<PaginationOptions>(configuration.GetSection("Pagination"));
        services.AddScoped(sp => sp.GetRequiredService<IOptions<PaginationOptions>>().Value);

        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.AddScoped(sp => sp.GetRequiredService<IOptions<JwtOptions>>().Value);

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Seguridad
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IValidator<RoleDto>, RoleValidator>();

        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IValidator<PermissionDto>, PermissionValidator>();

        services.AddScoped<IRolePermissionService, RolePermissionService>();

        services.AddScoped<IAppUserService, AppUserService>();
        services.AddScoped<IValidator<AppUserDto>, AppUserValidator>();

        services.AddScoped<IAppUserRoleService, AppUserRoleService>();

        services.AddScoped<IMenuService, MenuService>();
        services.AddScoped<IValidator<MenuDto>, MenuValidator>();
        services.AddScoped<IRoleMenuService, RoleMenuService>();

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();

        services.AddScoped<IAuditLogService, AuditLogService>();

        // Asistencia
        services.AddScoped<IPersonService, PersonService>();
        services.AddScoped<IValidator<PersonDto>, PersonValidator>();
        services.AddScoped<IPersonImportService, PersonImportService>();
        services.AddScoped<IPersonQrService, PersonQrService>();

        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IValidator<EventDto>, EventValidator>();

        services.AddScoped<IEventSessionService, EventSessionService>();
        services.AddScoped<IValidator<EventSessionDto>, EventSessionValidator>();

        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IValidator<ScanRequestDto>, ScanRequestValidator>();

        return services;
    }
}
