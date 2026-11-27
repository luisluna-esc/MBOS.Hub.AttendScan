namespace AttendScan.Application.Interfaces.People;

public record QrFile(byte[] Content, string FileName, string ContentType);

public interface IPersonQrService
{
    /// <summary>PNG del QR de una persona, o null si no existe.</summary>
    Task<QrFile?> GetQr(long personId);

    /// <summary>ZIP con el PNG del QR de cada persona ("001 Paterno Materno Nombres AS1-XXXX-XXXX-C.png").</summary>
    Task<QrFile> GetAllQrZip();
}
