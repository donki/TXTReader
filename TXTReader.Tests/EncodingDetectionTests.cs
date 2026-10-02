using System.Text;
using TXTReader.Services;

// Preferences, SecureStorage y la cultura del hilo son estado global: las pruebas van en serie.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace TXTReader.Tests;

public sealed class EncodingDetectionTests : IDisposable
{
    private readonly TempFolder _tmp = new();

    public void Dispose() => _tmp.Dispose();

    private static readonly Encoding Utf32Be = new UTF32Encoding(bigEndian: true, byteOrderMark: true);

    private string Write(byte[] bytes)
    {
        var path = _tmp.Combine(Guid.NewGuid().ToString("N") + ".txt");
        File.WriteAllBytes(path, bytes);
        return path;
    }

    private static byte[] WithBom(Encoding encoding, string text) =>
        [.. encoding.GetPreamble(), .. encoding.GetBytes(text)];

    private const string Sample = "Canción de España: ñandú, 5 €\nSecond line";

    public static TheoryData<string, string> BomEncodings => new()
    {
        { "utf-8", "Unicode (UTF-8)" },
        { "utf-16", "Unicode" },
        { "utf-16BE", "Unicode (Big-Endian)" },
        { "utf-32", "Unicode (UTF-32)" },
        { "utf-32BE", "Unicode (UTF-32 Big-Endian)" },
    };

    [Theory]
    [MemberData(nameof(BomEncodings))]
    public async Task WithByteOrderMark_IsDecodedAndTheMarkRemoved(string name, string expectedName)
    {
        var encoding = name == "utf-32BE" ? Utf32Be : Encoding.GetEncoding(name);

        var (content, detected) = await EncodingDetectionService.ReadFileWithEncodingDetectionAsync(Write(WithBom(encoding, Sample)));

        Assert.Equal(Sample, content);
        Assert.Equal(expectedName, detected);
    }

    [Fact]
    public async Task Utf8WithoutBom_IsDetected()
    {
        var (content, detected) = await EncodingDetectionService.ReadFileWithEncodingDetectionAsync(Write(Encoding.UTF8.GetBytes(Sample)));

        Assert.Equal(Sample, content);
        Assert.Equal(Encoding.UTF8.EncodingName, detected);
    }

    [Fact]
    public async Task PlainAscii_IsReadAsUtf8()
    {
        var (content, _) = await EncodingDetectionService.ReadFileWithEncodingDetectionAsync(Write(Encoding.ASCII.GetBytes("hello\r\nworld")));
        Assert.Equal("hello\r\nworld", content);
    }

    [Fact]
    public async Task Windows1252WithEuroSign_IsDecoded()
    {
        // 0x80 (€) solo existe en Windows-1252: antes pedir esa codificacion lanzaba una excepcion.
        // Las pruebas no registran el proveedor de paginas de codigos: lo tiene que hacer la app.
        byte[] bytes = [.. Encoding.Latin1.GetBytes("Precio: 5 "), 0x80, .. Encoding.Latin1.GetBytes(" y ñ")];

        var (content, detected) = await EncodingDetectionService.ReadFileWithEncodingDetectionAsync(Write(bytes));

        Assert.Equal("Precio: 5 € y ñ", content);
        Assert.Equal("Western European (Windows)", detected);
    }

    [Fact]
    public async Task Latin1Accents_WithoutBytesIn80To9F_AreDecoded()
    {
        // «canción» en Latin-1/ANSI (0xF3): no es UTF-8 valido. Antes salia «canci�n».
        const string text = "canción, pingüino, año";
        var bytes = Encoding.Latin1.GetBytes(text);

        var (content, _) = await EncodingDetectionService.ReadFileWithEncodingDetectionAsync(Write(bytes));

        Assert.Equal(text, content);
    }

    [Fact]
    public async Task SingleAnsiByte_IsDecoded()
    {
        var (content, _) = await EncodingDetectionService.ReadFileWithEncodingDetectionAsync(Write([0xE9]));
        Assert.Equal("é", content);
    }

    [Fact]
    public async Task EmptyFile_IsEmptyText()
    {
        var (content, detected) = await EncodingDetectionService.ReadFileWithEncodingDetectionAsync(Write([]));
        Assert.Equal(string.Empty, content);
        Assert.Equal(Encoding.UTF8.EncodingName, detected);
    }

    [Fact]
    public async Task OnlyAByteOrderMark_IsEmptyText()
    {
        var (content, _) = await EncodingDetectionService.ReadFileWithEncodingDetectionAsync(Write([0xEF, 0xBB, 0xBF]));
        Assert.Equal(string.Empty, content);
    }

    [Fact]
    public async Task TruncatedUtf8Sequence_FallsBackToAnsiInsteadOfFailing()
    {
        // Un fichero cortado a mitad de un caracter de varios bytes no es UTF-8 valido.
        byte[] bytes = [.. Encoding.UTF8.GetBytes("abc"), 0xC3];

        var (content, _) = await EncodingDetectionService.ReadFileWithEncodingDetectionAsync(Write(bytes));

        Assert.Equal("abcÃ", content);
    }

    [Fact]
    public async Task BigFile_IsReadWhole()
    {
        var text = string.Concat(Enumerable.Repeat("línea de texto con acentos áéíóú\n", 50_000));

        var (content, _) = await EncodingDetectionService.ReadFileWithEncodingDetectionAsync(Write(Encoding.UTF8.GetBytes(text)));

        Assert.Equal(text.Length, content.Length);
    }

    [Fact]
    public async Task MissingFile_Throws_AndIsLogged()
    {
        TestLog.Reset();
        var missing = _tmp.Combine("missing.txt");

        await Assert.ThrowsAsync<FileNotFoundException>(() => EncodingDetectionService.ReadFileWithEncodingDetectionAsync(missing));

        Assert.Contains("Fallback also failed", await TestLog.WaitForAsync("Fallback also failed"));
    }

    [Fact]
    public async Task ContentUri_OutsideAndroid_IsNotSupported() =>
        await Assert.ThrowsAsync<PlatformNotSupportedException>(() =>
            EncodingDetectionService.ReadFileWithEncodingDetectionAsync("content://com.example/doc/1"));
}
