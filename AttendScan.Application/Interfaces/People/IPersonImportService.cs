using AttendScan.core.Entities.ResponseApi.DisplayFormat;

namespace AttendScan.Application.Interfaces.People;

public interface IPersonImportService
{
    /// <summary>
    /// Lee el Excel de la lista de personas. Sin commit solo muestra qué pasaría (vista previa);
    /// con commit crea las personas nuevas y les genera su código de carnet, todo o nada.
    /// </summary>
    Task<ResponseGetObject> Import(Stream excel, bool commit);
}
