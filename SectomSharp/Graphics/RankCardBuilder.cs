using Discord;
using Discord.WebSocket;
using SectomSharp.Utils;
using SkiaSharp;

#pragma warning disable CS0618 // Type or member is obsolete

namespace SectomSharp.Graphics;

public sealed class RankCardBuilder
{
    private const int CardWidth = 930;
    private const int CardHeight = 280;
    private const int Scale = 4;
    private const int OverlayOpacity = 95;

    private static readonly Dictionary<UserStatus, SKColor> UserStatusColors = new()
    {
        { UserStatus.Online, new SKColor(67, 181, 129, 255) },
        { UserStatus.Idle, new SKColor(250, 166, 26, 255) },
        { UserStatus.DoNotDisturb, new SKColor(240, 71, 71, 255) },
        { UserStatus.Offline, new SKColor(116, 127, 141, 255) },
        { UserStatus.Invisible, new SKColor(116, 127, 141, 255) }
    };

    private static readonly SKColor BackgroundColor = new(44, 47, 51, 255);
    private static readonly SKColor OverlayColor = new(35, 39, 42, 255);
    private static readonly SKRoundRect RenderCardOverlayRect = new(new SKRect(20, 20, CardWidth - 20, CardHeight - 20), 12, 12);

    private static readonly SKPaint OverlayPaint = new()
    {
        Color = OverlayColor.WithAlpha(255 * OverlayOpacity / 100),
        IsAntialias = true
    };

    private static readonly SKPaint StatusBackgroundPaint = new()
    {
        Color = BackgroundColor,
        IsAntialias = true
    };

    private static readonly SKPaint TrackPaint = new()
    {
        Color = SKUtils.DarkGrey,
        IsAntialias = true
    };

    private static readonly SKPaint FillPaint = new()
    {
        Color = SKUtils.DiscordBlurple,
        IsAntialias = true
    };

    private static readonly SKPaint DisplayNamePaint = new()
    {
        Color = SKColors.White,
        IsAntialias = true,
        Typeface = SKUtils.GeistRegular,
        TextSize = 32
    };

    private static readonly SKPaint UsernamePaint = new()
    {
        Color = SKUtils.Grey,
        IsAntialias = true,
        Typeface = SKUtils.GeistRegular,
        TextSize = 18
    };

    private static readonly SKPaint LabelPaint = new()
    {
        Color = SKUtils.Grey,
        IsAntialias = true,
        Typeface = SKUtils.GeistRegular,
        TextSize = 16
    };

    private static readonly SKPaint ValuePaint = new()
    {
        Color = SKColors.White,
        IsAntialias = true,
        Typeface = SKUtils.GeistRegular,
        TextSize = 16
    };

    public required SocketGuildUser User { get; init; }
    public required uint Rank { get; init; }
    public required uint Level { get; init; }
    public required uint CurrentXp { get; init; }
    public required uint RequiredXp { get; init; }

    private void RenderCard(SKCanvas canvas, SKBitmap avatarBitmap)
    {
        canvas.Clear(BackgroundColor);
        canvas.DrawRoundRect(RenderCardOverlayRect, OverlayPaint);
        DrawAvatar(canvas, avatarBitmap);
        DrawUserInfo(canvas);
        DrawProgressBar(canvas);
        DrawStatistics(canvas);
    }

    private void DrawAvatar(SKCanvas canvas, SKBitmap avatarBitmap)
    {
        const float avatarSize = 120;
        const float avatarX = 50;
        const float avatarY = (CardHeight - avatarSize) / 2;

        using (var clipPath = new SKPath())
        {
            clipPath.AddCircle(avatarX + avatarSize / 2, avatarY + avatarSize / 2, avatarSize / 2);
            canvas.Save();
            canvas.ClipPath(clipPath);
        }

        using (var avatarPaint = new SKPaint())
        {
            avatarPaint.IsAntialias = true;
            avatarPaint.FilterQuality = SKFilterQuality.High;

            var avatarRect = new SKRect(avatarX, avatarY, avatarX + avatarSize, avatarY + avatarSize);
            canvas.DrawBitmap(avatarBitmap, avatarRect, avatarPaint);
        }

        canvas.Restore();

        DrawStatusIndicator(canvas, avatarX + avatarSize - 20, avatarY + avatarSize - 20);
    }

    private void DrawStatusIndicator(SKCanvas canvas, float x, float y)
    {
        const float statusSize = 24;
        SKColor statusColor = UserStatusColors[User.Status];
        canvas.DrawCircle(x, y, statusSize / 2 + 2, StatusBackgroundPaint);

        using var statusPaint = new SKPaint();
        statusPaint.Color = statusColor;
        statusPaint.IsAntialias = true;
        canvas.DrawCircle(x, y, statusSize / 2, statusPaint);
    }

    private void DrawUserInfo(SKCanvas canvas)
    {
        const float textX = 200;
        float textY = 90;
        canvas.DrawText(User.DisplayName, textX, textY, DisplayNamePaint);
        textY += 45;
        canvas.DrawText($"@{User.Username}", textX, textY, UsernamePaint);
    }

    private void DrawProgressBar(SKCanvas canvas)
    {
        const float progressX = 200;
        const float progressY = 160;
        const float progressWidth = 500;
        const float progressHeight = 20;

        canvas.DrawRoundRect(new SKRoundRect(new SKRect(progressX, progressY, progressX + progressWidth, progressY + progressHeight), 10, 10), TrackPaint);

        float progress = Math.Clamp((float)CurrentXp / RequiredXp * 100, 0, 100);
        float fillWidth = progressWidth * (progress / 100);

        if (fillWidth <= 0)
        {
            return;
        }

        canvas.DrawRoundRect(new SKRoundRect(new SKRect(progressX, progressY, progressX + fillWidth, progressY + progressHeight), 10, 10), FillPaint);
    }

    private void DrawStatistics(SKCanvas canvas)
    {
        float statX = 200;
        const float statY = 210;

        const string levelText = "LEVEL ";
        canvas.DrawText(levelText, statX, statY, LabelPaint);
        statX += LabelPaint.MeasureText(levelText);

        string levelValue = SKUtils.FormatNumber(Level);
        canvas.DrawText(levelValue, statX, statY, ValuePaint);
        statX += ValuePaint.MeasureText(levelValue) + 40;

        const string xpText = "XP ";
        canvas.DrawText(xpText, statX, statY, LabelPaint);
        statX += LabelPaint.MeasureText(xpText);

        string xpValue = $"{SKUtils.FormatNumber(CurrentXp)}/{SKUtils.FormatNumber(RequiredXp)}";
        canvas.DrawText(xpValue, statX, statY, ValuePaint);
        statX += ValuePaint.MeasureText(xpValue) + 40;

        const string rankText = "RANK ";
        canvas.DrawText(rankText, statX, statY, LabelPaint);
        statX += LabelPaint.MeasureText(rankText);

        string rankValue = $"#{SKUtils.FormatNumber(Rank)}";
        canvas.DrawText(rankValue, statX, statY, ValuePaint);
    }

    public async Task<byte[]> BuildAsync()
    {
        string avatarUrl = User.GetDisplayAvatarUrl(ImageFormat.Png, 4096);

        var info = new SKImageInfo(CardWidth * Scale, CardHeight * Scale);
        using var surface = SKSurface.Create(info);
        SKCanvas surfaceCanvas = surface.Canvas;
        surfaceCanvas.Scale(Scale);

        const int avatarTargetSize = 120 * Scale;
        using (SKBitmap avatarBitmap = await SKUtils.GetResizedSKBitmapFromUrlAsync(avatarUrl, avatarTargetSize, avatarTargetSize))
        {
            RenderCard(surfaceCanvas, avatarBitmap);
        }

        using SKImage image = surface.Snapshot();
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
