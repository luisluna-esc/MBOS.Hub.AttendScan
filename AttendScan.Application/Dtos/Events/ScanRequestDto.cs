namespace AttendScan.Application.Dtos.Events;

public class ScanRequestDto
{
    public long EventSessionId { get; set; }
    /// <summary>Texto leído del QR (o tipeado a mano).</summary>
    public string Code { get; set; } = string.Empty;
}
