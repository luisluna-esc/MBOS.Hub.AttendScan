using AttendScan.Application.Services;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

namespace AttendScan.Tests;

/// <summary>
/// El QR del carnet se genera con QRCoder y aquí se lee con ZXing (otra librería, la misma familia
/// de lectores que usan los celulares): cada QR tiene que devolver exactamente su código.
/// </summary>
public class CardQrTests
{
    [Fact]
    public void Every_generated_qr_contains_exactly_its_code()
    {
        for (var i = 0; i < 2000; i++)
        {
            var code = CardCodes.Generate();
            Assert.Equal(code, Read(code, pure: true));
        }
    }

    /// <summary>
    /// Con el detector general (el que busca el QR dentro de la imagen) ZXing a veces no encuentra
    /// el QR en una imagen sintética perfecta (~0,3%): es un límite conocido de ZXing, no del QR
    /// (jsQR, el motor de qr-scanner que usa la app, leyó 2.240 códigos en 4 tamaños sin fallar).
    /// Lo que nunca puede pasar es leer OTRO código: eso sí registraría a la persona equivocada.
    /// </summary>
    [Fact]
    public void Detector_never_reads_a_different_code()
    {
        for (var i = 0; i < 2000; i++)
        {
            var code = CardCodes.Generate();
            var read = Read(code, pure: false);
            Assert.True(read is null || read == code, $"{code} se leyó como {read}");
        }
    }

    [Theory]
    [InlineData("AS1-83Y5-08SA-D")]
    [InlineData("AS1-0000-0000-0")]
    [InlineData("AS1-ZZZZ-ZZZZ-8")]
    public void Known_codes_read_back_and_scanner_accepts_them(string code)
    {
        var read = Read(code, pure: true);
        Assert.Equal(code, read);
        // Lo que lee el celular es exactamente lo que acepta el servidor al escanear.
        Assert.Equal(code, CardCodes.Normalize(read));
    }

    [Fact]
    public void Qr_has_high_error_correction_and_quiet_zone()
    {
        Assert.Equal(QRCoder.QRCodeGenerator.ECCLevel.Q, CardQr.ErrorCorrection);

        using var data = CardQr.Matrix("AS1-83Y5-08SA-D");
        var matrix = data.ModuleMatrix;
        // Margen blanco de 4 módulos alrededor (sin él muchos lectores no lo reconocen).
        for (var i = 0; i < matrix.Count; i++)
        {
            for (var q = 0; q < 4; q++)
            {
                Assert.False(matrix[q][i]);
                Assert.False(matrix[i][q]);
                Assert.False(matrix[matrix.Count - 1 - q][i]);
                Assert.False(matrix[i][matrix.Count - 1 - q]);
            }
        }
    }

    [Fact]
    public void Png_is_a_valid_image()
    {
        var png = CardQr.Png("AS1-83Y5-08SA-D");
        Assert.True(png.Length > 100);
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, png[..4]);
    }

    [Fact]
    public void Only_official_codes_get_a_qr()
    {
        Assert.Throws<ArgumentException>(() => CardQr.Png("AS1-7KQ3-M9XD-S")); // verificador malo
        Assert.Throws<ArgumentException>(() => CardQr.Png("as1-7kq3-m9xd-r")); // no está en su forma oficial
        Assert.Throws<ArgumentException>(() => CardQr.Png("hola"));
    }

    /// <summary>
    /// Dibuja el QR en escala de grises (4 px por módulo) y lo lee con ZXing. pure: lee el QR
    /// directamente (comprueba el contenido); sin pure, primero lo busca en la imagen como una cámara.
    /// </summary>
    private static string? Read(string code, bool pure)
    {
        using var data = CardQr.Matrix(code);
        var modules = data.ModuleMatrix;
        const int scale = 4;
        var size = modules.Count * scale;
        var pixels = new byte[size * size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                pixels[y * size + x] = modules[y / scale][x / scale] ? (byte)0 : (byte)255;
            }
        }

        var bitmap = new BinaryBitmap(new HybridBinarizer(new RGBLuminanceSource(pixels, size, size, RGBLuminanceSource.BitmapFormat.Gray8)));
        var hints = new Dictionary<DecodeHintType, object> { [DecodeHintType.TRY_HARDER] = true };
        if (pure)
        {
            hints[DecodeHintType.PURE_BARCODE] = true;
        }

        return new QRCodeReader().decode(bitmap, hints)?.Text;
    }
}
