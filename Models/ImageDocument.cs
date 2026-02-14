using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace ImageConverter.Models;

public sealed class ImageDocument(string fileName, IImageFormat targetFormat) : IDisposable
{
    private Image? _image;

    public string FileName { get; } = fileName;
    public int Width => _image?.Width ?? 0;
    public int Height => _image?.Height ?? 0;
    public bool IsLoaded => _image is not null;
    public IImageFormat TargetFormat { get; set; } = targetFormat;

    public async Task LoadAsync(Stream stream)
    {
        _image?.Dispose();
        _image = await Image.LoadAsync(stream);
    }

    public async Task<ConversionResult> ConvertAsync(IImageEncoder? encoder)
    {
        if (_image is null)
            throw new InvalidOperationException("Image not loaded.");

        encoder ??= Configuration.Default.ImageFormatsManager.GetEncoder(TargetFormat);

        var outputStream = new MemoryStream();
        await _image.SaveAsync(outputStream, encoder);

        outputStream.Position = 0;
        var outputFileName = FormatInfo.GetOutputFileName(FileName, TargetFormat);
        return new ConversionResult(outputStream, outputFileName, outputStream.Length);
    }

    public string ToThumbnailDataUrl(int maxSize = 320)
    {
        if (_image is null)
            throw new InvalidOperationException("Image not loaded.");

        using var thumbnail = _image.Clone(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(maxSize, maxSize),
            Mode = ResizeMode.Max
        }));
        return thumbnail.ToBase64String(JpegFormat.Instance);
    }

    public void Dispose()
    {
        _image?.Dispose();
        _image = null;
    }
}

public record ConversionResult(MemoryStream Stream, string OutputFileName, long Size);
