using JetBrains.Annotations;
using SkiaSharp;
using Svg.Skia;

#pragma warning disable CS0618 // Type or member is obsolete

namespace SectomSharp.Utils;

public static class SKUtils
{
    public const int Scale = 4;

    private static readonly HttpClient HttpClient = new();

    public static readonly SKColor Grey = new(128, 131, 134, 255);
    public static readonly SKColor DarkGrey = new(72, 75, 78, 255);
    public static readonly SKColor DiscordBlurple = new(88, 101, 242, 255);

    public static readonly SKTypeface GeistRegular = SKTypeface.FromFile(GetFullPathFromFontsFolder("Geist-Regular.ttf"));

    public static readonly SKTypeface RobotoRegular = SKTypeface.FromFile(GetFullPathFromFontsFolder("Roboto-Regular.ttf"));
    public static readonly SKTypeface RobotoBold = SKTypeface.FromFile(GetFullPathFromFontsFolder("Roboto-Bold.ttf"));

    private static void ThrowIfNotRelativePath(string relativePath)
    {
        if (relativePath.StartsWith("~/", StringComparison.Ordinal))
        {
            throw new ArgumentException("Path must be relative.", nameof(relativePath));
        }
    }

    private static string GetFullPathFromFontsFolder([PathReference("~/Fonts/")] string relativePath)
    {
        ThrowIfNotRelativePath(relativePath);
        return Path.Combine(AppContext.BaseDirectory, "Fonts", relativePath);
    }

    private static string GetFullPathFromAssetsFolder([PathReference("~/Assets/")] string relativePath)
    {
        ThrowIfNotRelativePath(relativePath);
        return Path.Combine(AppContext.BaseDirectory, "Assets", relativePath);
    }

    public static string FormatNumber(uint number)
        => number switch
        {
            >= 1_000_000 => $"{number / 1_000_000.0:F1}M",
            >= 1_000 => $"{number / 1_000.0:F1}K",
            _ => number.ToString()
        };

    [MustDisposeResource]
    public static async Task<SKBitmap> GetResizedSKBitmapFromUrlAsync(string url, int targetWidth, int targetHeight)
    {
        const int maxRetries = 3;
        const int delayMilliseconds = 500;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                byte[] imageData = await HttpClient.GetByteArrayAsync(url);
                using SKBitmap originalBitmap = SKBitmap.Decode(imageData);

                var resizedBitmap = new SKBitmap(targetWidth, targetHeight);
                using var canvas = new SKCanvas(resizedBitmap);
                using var paint = new SKPaint();
                paint.IsAntialias = true;
                paint.FilterQuality = SKFilterQuality.High;

                canvas.DrawBitmap(originalBitmap, new SKRect(0, 0, targetWidth, targetHeight), paint);
                return resizedBitmap;
            }
            catch (HttpRequestException) when (attempt < maxRetries - 1)
            {
                await Task.Delay(delayMilliseconds * (attempt + 1));
            }
        }

        byte[] finalImageData = await HttpClient.GetByteArrayAsync(url);
        using SKBitmap finalOriginalBitmap = SKBitmap.Decode(finalImageData);

        var finalResizedBitmap = new SKBitmap(targetWidth, targetHeight);
        using var finalCanvas = new SKCanvas(finalResizedBitmap);
        using var finalPaint = new SKPaint();
        finalPaint.IsAntialias = true;
        finalPaint.FilterQuality = SKFilterQuality.High;
        finalCanvas.DrawBitmap(finalOriginalBitmap, new SKRect(0, 0, targetWidth, targetHeight), finalPaint);
        return finalResizedBitmap;
    }

    public static SKSvg GetSvgFromAssets([PathReference("~/Assets/")] string relativePath)
    {
        string fullPath = GetFullPathFromAssetsFolder(relativePath);
        var svg = new SKSvg();
        SKPicture? svgPicture = svg.Load(fullPath);
        ArgumentNullException.ThrowIfNull(svgPicture);
        return svg;
    }

    public static void DrawSvg(SKCanvas canvas, SKSvg svg, SKRect targetRect, float opacity = 1)
    {
        SKPicture? picture = svg.Picture;
        ArgumentNullException.ThrowIfNull(picture);

        float scaleX = targetRect.Width / picture.CullRect.Width;
        float scaleY = targetRect.Height / picture.CullRect.Height;

        canvas.Save();
        canvas.Translate(targetRect.Left, targetRect.Top);
        canvas.Scale(scaleX, scaleY);

        if (opacity >= 0.999f)
        {
            canvas.DrawPicture(svg.Picture);
        }
        else
        {
            using var paint = new SKPaint();
            paint.Color = SKColors.White.WithAlpha((byte)(Byte.MaxValue * opacity));
            paint.IsAntialias = true;
            canvas.DrawPicture(svg.Picture, paint);
        }

        canvas.Restore();
    }
}
