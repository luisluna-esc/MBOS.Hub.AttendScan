FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY MBOS.Hub.AttendScan.sln ./
COPY AttendScan.Web/AttendScan.Web.csproj AttendScan.Web/
COPY AttendScan.Application/AttendScan.Application.csproj AttendScan.Application/
COPY AttendScan.core/AttendScan.core.csproj AttendScan.core/
COPY AttendScan.Infrastructure/AttendScan.Infrastructure.csproj AttendScan.Infrastructure/
RUN dotnet restore AttendScan.Web/AttendScan.Web.csproj

COPY AttendScan.Web/ AttendScan.Web/
COPY AttendScan.Application/ AttendScan.Application/
COPY AttendScan.core/ AttendScan.core/
COPY AttendScan.Infrastructure/ AttendScan.Infrastructure/

RUN dotnet publish AttendScan.Web/AttendScan.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AttendScan.Web.dll"]
