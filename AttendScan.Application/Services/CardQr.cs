using QRCoder;

namespace AttendScan.Application.Services;

/// <summary>
/// Imagen QR del carnet. Lleva SOLO el CardCode ("AS1-XXXX-XXXX-C"), nada más: así el QR nunca
/// vence ni depende de datos que cambian. Mismo generador para la pantalla, el ZIP y el PDF del
/// carnet, para que lo que se ve sea exactamente lo que se imprime.
/// </summary>
public static class CardQr
{
    /// <summary>
    /// Nivel Q: el QR se sigue leyendo aunque se dañe ~25% (carnet rayado, doblado, sucio).
    /// No cambiar a L/M para "achicar": los carnets impresos no se pueden rehacer.
    /// </summary>
    public const QRCodeGenerator.ECCLevel ErrorCorrection = QRCodeGenerator.ECCLevel.Q;

    /// <summary>Píxeles por módulo: 20 → imagen de ~580 px, nítida hasta varios centímetros impresa.</summary>
    private const int PixelsPerModule = 20;

    /// <summary>PNG negro sobre blanco con su margen blanco (zona de silencio) de 4 módulos.</summary>
    public static byte[] Png(string cardCode)
    {
        var data = Matrix(cardCode);
        return new PngByteQRCode(data).GetGraphic(PixelsPerModule, drawQuietZones: true);
    }

    /// <summary>Datos del QR (módulos), para el PDF y para las pruebas de lectura.</summary>
    public static QRCodeData Matrix(string cardCode)
    {
        if (CardCodes.Normalize(cardCode) != cardCode)
        {
            throw new ArgumentException($"Not an official card code: {cardCode}", nameof(cardCode));
        }

        using var generator = new QRCodeGenerator();
        return generator.CreateQrCode(cardCode, ErrorCorrection);
    }
}
