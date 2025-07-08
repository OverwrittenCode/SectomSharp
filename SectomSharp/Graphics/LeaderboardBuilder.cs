using Discord.WebSocket;
using SectomSharp.Utils;
using SkiaSharp;
using Svg.Skia;

#pragma warning disable CS0618 // Type or member is obsolete

namespace SectomSharp.Graphics;

public sealed class LeaderboardBuilder
{
    private const int CardWidth = 500;
    private const int CardHeight = 610;
    private const int Padding = 30;

    private const int HeaderImageSize = 64;
    private const int HeaderSpacing = 24;
    private const int HeaderSubtitleSize = 14;
    private const int HeaderSubtitleSpacing = 22;

    private const int PodiumHeight = 120;
    private const int WinnerHeightBonus = 16;

    private const int ListItemHeight = 70;

    private static readonly SKColor[] RankColors =
    [
        new(255, 170, 0, 255),
        new(0, 155, 214, 255),
        new(0, 217, 95, 255)
    ];

    private static readonly SKSvg LeaderboardBackground = SKUtils.GetSvgFromAssets("leaderboardBackground.svg");
    private static readonly SKSvg Crown = SKUtils.GetSvgFromAssets("crown.svg");
    private static readonly SKSvg UnknownPlayer = SKUtils.GetSvgFromAssets("unknownPlayer.svg");

    private static readonly PodiumPosition[] PodiumPositions =
    [
        new(1, [new SKPoint(8, 8), new SKPoint(8, 8), new SKPoint(0, 0), new SKPoint(8, 8)]),
        new(0, [new SKPoint(8, 8), new SKPoint(8, 8), new SKPoint(0, 0), new SKPoint(0, 0)]),
        new(2, [new SKPoint(8, 8), new SKPoint(8, 8), new SKPoint(8, 8), new SKPoint(0, 0)])
    ];

    private static readonly SKPaint RankNumberPaint = new()
    {
        Color = SKColors.White,
        IsAntialias = true,
        Typeface = SKUtils.RobotoRegular,
        TextSize = 20,
        TextAlign = SKTextAlign.Center
    };

    private static readonly SKPaint RankLabelPaint = new()
    {
        Color = SKColors.White,
        IsAntialias = true,
        Typeface = SKUtils.RobotoRegular,
        TextSize = 14,
        TextAlign = SKTextAlign.Center
    };

    private static readonly SKPaint ListPlayerNamePaint = new()
    {
        Color = SKColors.White,
        IsAntialias = true,
        Typeface = SKUtils.RobotoRegular,
        TextSize = 14
    };

    private static readonly SKPaint ListPlayerUsernamePaint = new()
    {
        Color = SKColors.White,
        IsAntialias = true,
        Typeface = SKUtils.RobotoRegular,
        TextSize = 14
    };

    private static readonly SKPaint ListPlayerStatsPaint = new()
    {
        Color = SKColors.White,
        IsAntialias = true,
        Typeface = SKUtils.RobotoRegular,
        TextSize = 14
    };

    private static readonly SKPaint RankTextPaint = new()
    {
        Color = SKColors.White,
        IsAntialias = true,
        Typeface = SKUtils.RobotoBold,
        TextSize = 12,
        TextAlign = SKTextAlign.Center
    };

    private static readonly SKPaint PodiumPlayerInfoNamePaint = new()
    {
        Color = SKColors.White,
        IsAntialias = true,
        Typeface = SKUtils.RobotoRegular,
        TextSize = 16,
        TextAlign = SKTextAlign.Center
    };

    private static readonly SKPaint PodiumPlayerInfoUsernamePaint = new()
    {
        Color = SKColors.White,
        IsAntialias = true,
        Typeface = SKUtils.RobotoRegular,
        TextSize = 12,
        TextAlign = SKTextAlign.Center
    };

    private static readonly SKPaint HeaderTitlePaint = new()
    {
        Color = SKColors.White,
        IsAntialias = true,
        Typeface = SKUtils.RobotoRegular,
        TextSize = 20,
        TextAlign = SKTextAlign.Center
    };

    private static readonly SKPaint HeaderSubtitlePaint = new()
    {
        Color = SKColors.White,
        IsAntialias = true,
        Typeface = SKUtils.RobotoRegular,
        TextSize = HeaderSubtitleSize,
        TextAlign = SKTextAlign.Center
    };

    private static readonly SKPaint AvatarPaint = new()
    {
        IsAntialias = true,
        FilterQuality = SKFilterQuality.High
    };

    private static readonly SKPaint ListItemBackgroundPaint = new()
    {
        Color = new SKColor(30, 34, 55, 180),
        IsAntialias = true
    };

    private static readonly SKPaint PodiumPaintWinner = new()
    {
        Color = new SKColor(46, 52, 71, 180),
        IsAntialias = true
    };

    private static readonly SKPaint PodiumPaintNormal = new()
    {
        Color = new SKColor(28, 32, 56, 180),
        IsAntialias = true
    };

    private static void DrawUnknownPlayerSvg(SKCanvas canvas, float centerX, float centerY, float avatarSize)
    {
        using var clipPath = new SKPath();
        clipPath.AddCircle(centerX, centerY, avatarSize / 2f);
        canvas.Save();
        canvas.ClipPath(clipPath);

        var avatarRect = new SKRect(centerX - avatarSize / 2f, centerY - avatarSize / 2f, centerX + avatarSize / 2f, centerY + avatarSize / 2f);

        SKUtils.DrawSvg(canvas, UnknownPlayer, avatarRect);
        canvas.Restore();
    }

    private static async Task DrawListItem(SKCanvas canvas, LeaderboardPlayer player, int rank, float x, float y, float width)
    {
        const int avatarSize = 56;
        const int avatarXOffset = 70;

        DrawListItemBackground(canvas, x, y, width);
        DrawListRank(canvas, rank, x, y);

        if (player == LeaderboardPlayer.Unknown)
        {
            float avatarX = x + avatarXOffset;
            float avatarY = y + (ListItemHeight - avatarSize) / 2f;
            float centerX = avatarX + avatarSize / 2f;
            float centerY = avatarY + avatarSize / 2f;

            DrawUnknownPlayerSvg(canvas, centerX, centerY, avatarSize);
        }
        else
        {
            using SKBitmap avatarBitmap = await SKUtils.GetResizedSKBitmapFromUrlAsync(player.AvatarUrl, avatarSize * SKUtils.Scale, avatarSize * SKUtils.Scale);
            DrawListAvatar(canvas, avatarBitmap, x, y);
        }

        DrawListPlayerInfo(canvas, player, x, y, width);
        return;

        static void DrawListItemBackground(SKCanvas canvas, float x, float y, float width)
        {
            var itemRect = new SKRect(x, y, x + width, y + ListItemHeight);
            var roundRect = new SKRoundRect(itemRect, 8, 8);
            canvas.DrawRoundRect(roundRect, ListItemBackgroundPaint);
        }

        static void DrawListRank(SKCanvas canvas, int rank, float x, float y)
        {
            float rankCenterX = x + 30;
            canvas.DrawText(rank.ToString(), rankCenterX, y + 30, RankNumberPaint);
            canvas.DrawText("Rank", rankCenterX, y + 50, RankLabelPaint);
        }

        static void DrawListAvatar(SKCanvas canvas, SKBitmap avatarBitmap, float itemX, float itemY)
        {
            const int avatarSize = 56;
            const int avatarXOffset = 70;

            float avatarX = itemX + avatarXOffset;
            float avatarY = itemY + (ListItemHeight - avatarSize) / 2f;
            float centerX = avatarX + avatarSize / 2f;
            float centerY = avatarY + avatarSize / 2f;

            using var clipPath = new SKPath();
            clipPath.AddCircle(centerX, centerY, avatarSize / 2f);
            canvas.Save();
            canvas.ClipPath(clipPath);

            canvas.DrawBitmap(avatarBitmap, new SKRect(avatarX, avatarY, avatarX + avatarSize, avatarY + avatarSize), AvatarPaint);
            canvas.Restore();
        }

        static void DrawListPlayerInfo(SKCanvas canvas, LeaderboardPlayer player, float itemX, float itemY, float itemWidth)
        {
            const int avatarXOffset = 70;
            const int avatarSize = 56;

            float textX = itemX + avatarXOffset + avatarSize + 16;

            canvas.DrawText(player.DisplayName, textX, itemY + 25, ListPlayerNamePaint);
            canvas.DrawText($"@{player.Username}", textX, itemY + 50, ListPlayerUsernamePaint);

            string levelText = $"Level {player.Level}";
            string xpText = $"{SKUtils.FormatNumber(player.Xp)} XP";

            float statsX = itemX + itemWidth - 80 - 24;

            canvas.DrawText(levelText, statsX, itemY + 25, ListPlayerStatsPaint);
            canvas.DrawText(xpText, statsX, itemY + 45, ListPlayerStatsPaint);
        }
    }

    public required SocketGuild Guild { get; init; }

    public required LeaderboardPlayers Players { get; init; }

    private void DrawHeader(SKCanvas canvas, SKBitmap headerImage)
    {
        float currentY = Padding;

        DrawHeaderImage(canvas, headerImage, currentY);
        currentY += HeaderImageSize + HeaderSpacing;

        DrawHeaderTitle(canvas, currentY, Guild);
        currentY += HeaderSubtitleSpacing;

        DrawHeaderSubtitle(canvas, currentY, Guild);
        return;

        static void DrawHeaderImage(SKCanvas canvas, SKBitmap headerImage, float y)
        {
            const float imageX = (CardWidth - HeaderImageSize) / 2f;
            const float imageRadius = HeaderImageSize / 2f;
            const float centerX = imageX + imageRadius;
            float centerY = y + imageRadius;

            using var clipPath = new SKPath();
            clipPath.AddCircle(centerX, centerY, imageRadius);
            canvas.Save();
            canvas.ClipPath(clipPath);

            var imageRect = new SKRect(imageX, y, imageX + HeaderImageSize, y + HeaderImageSize);
            canvas.DrawBitmap(headerImage, imageRect, AvatarPaint);
            canvas.Restore();
        }

        static void DrawHeaderTitle(SKCanvas canvas, float y, SocketGuild guild) => canvas.DrawText(guild.Name, CardWidth / 2f, y, HeaderTitlePaint);

        static void DrawHeaderSubtitle(SKCanvas canvas, float y, SocketGuild guild) => canvas.DrawText($"{guild.MemberCount} members", CardWidth / 2f, y, HeaderSubtitlePaint);
    }

    private async Task DrawPodium(SKCanvas canvas, float startY)
    {
        const float totalPodiumWidth = CardWidth * 0.9f;
        const float podiumWidth = totalPodiumWidth / 3f;
        const float startX = CardWidth * 0.05f;

        const int podiumAvatarSize = 72;

        var avatarTasks = new Task<SKBitmap?>[PodiumPositions.Length];
        for (int i = 0; i < PodiumPositions.Length; i++)
        {
            LeaderboardPlayer player = Players[(int)PodiumPositions[i].PlayerIndex];
            if (player == LeaderboardPlayer.Unknown)
            {
                avatarTasks[i] = Task.FromResult<SKBitmap?>(null);
            }
            else
            {
                // ReSharper disable once NotDisposedResource
                avatarTasks[i] = SKUtils.GetResizedSKBitmapFromUrlAsync(player.AvatarUrl, podiumAvatarSize * SKUtils.Scale, podiumAvatarSize * SKUtils.Scale)!;
            }
        }

        SKBitmap?[] avatarBitmaps = await Task.WhenAll(avatarTasks);

        const int podiumAvatarOffset = 15;

        try
        {
            for (int i = 0; i < PodiumPositions.Length; i++)
            {
                PodiumPosition position = PodiumPositions[i];
                LeaderboardPlayer player = Players[(int)position.PlayerIndex];
                float podiumX = startX + i * podiumWidth;
                uint rank = position.PlayerIndex + 1;

                const int podiumInfoStartOffset = 50;
                const int crownToPodiumSpacing = 10;

                float podiumY = startY + (position.IsFirst ? -WinnerHeightBonus : 0);
                float podiumHeight = PodiumHeight + (position.IsFirst ? WinnerHeightBonus : 0);

                DrawPodiumBackground(canvas, podiumX, podiumY, podiumWidth, podiumHeight, position);

                float centerX = podiumX + podiumWidth / 2f;
                float centerY = podiumY - podiumAvatarOffset;

                if (position.IsFirst)
                {
                    DrawCrown(canvas, centerX, centerY - podiumAvatarSize / 2f - crownToPodiumSpacing);

                    static void DrawCrown(SKCanvas canvas, float centerX, float centerY)
                    {
                        const int crownSize = 20;
                        const int crownVerticalOffset = 6;

                        var targetRect = new SKRect(
                            centerX - crownSize / 2f,
                            centerY - crownSize / 2f - crownVerticalOffset,
                            centerX + crownSize / 2f,
                            centerY + crownSize / 2f - crownVerticalOffset
                        );

                        SKUtils.DrawSvg(canvas, Crown, targetRect);
                    }
                }

                SKColor rankColor = RankColors[rank - 1];

                using var podiumBorderPaint = new SKPaint();
                podiumBorderPaint.IsAntialias = true;
                podiumBorderPaint.Style = SKPaintStyle.Stroke;
                podiumBorderPaint.StrokeWidth = 3;
                podiumBorderPaint.Color = rankColor;
                canvas.DrawCircle(centerX, centerY, podiumAvatarSize / 2f + 1.5f, podiumBorderPaint);

                if (player == LeaderboardPlayer.Unknown)
                {
                    DrawPodiumUnknownAvatar(canvas, rank, centerX, centerY, rankColor);
                }
                else
                {
                    using SKBitmap? avatarBitmap = avatarBitmaps[i];
                    if (avatarBitmap != null)
                    {
                        DrawPodiumAvatar(canvas, avatarBitmap, rankColor, centerX, centerY, rank);
                    }
                }

                DrawPodiumPlayerInfo(canvas, player, rankColor, podiumX, podiumY + podiumInfoStartOffset, podiumWidth);
            }
        }
        finally
        {
            foreach (SKBitmap? bitmap in avatarBitmaps)
            {
                bitmap?.Dispose();
            }
        }

        return;

        static void DrawPodiumBackground(SKCanvas canvas, float x, float y, float width, float height, PodiumPosition position)
        {
            var podiumRect = new SKRect(x, y, x + width, y + height);

            using var path = new SKPath();
            var roundRect = new SKRoundRect();
            roundRect.SetRectRadii(podiumRect, position.CornerRadii);
            path.AddRoundRect(roundRect);
            canvas.DrawPath(path, position.IsFirst ? PodiumPaintWinner : PodiumPaintNormal);
        }

        static void DrawPodiumUnknownAvatar(SKCanvas canvas, uint rank, float centerX, float centerY, SKColor rankColor)
        {
            DrawUnknownPlayerSvg(canvas, centerX, centerY, podiumAvatarSize);
            DrawRankBadge(canvas, centerX, centerY, podiumAvatarSize, rankColor, rank);
        }

        static void DrawPodiumAvatar(SKCanvas canvas, SKBitmap avatarBitmap, SKColor rankColor, float centerX, float centerY, uint rank)
        {
            float avatarX = centerX - podiumAvatarSize / 2f;
            float avatarY = centerY - podiumAvatarSize / 2f;

            using (var clipPath = new SKPath())
            {
                clipPath.AddCircle(centerX, centerY, podiumAvatarSize / 2f);
                canvas.Save();
                canvas.ClipPath(clipPath);
            }

            canvas.DrawBitmap(avatarBitmap, new SKRect(avatarX, avatarY, avatarX + podiumAvatarSize, avatarY + podiumAvatarSize), AvatarPaint);
            canvas.Restore();

            DrawRankBadge(canvas, centerX, centerY, podiumAvatarSize, rankColor, rank);
        }

        static void DrawRankBadge(SKCanvas canvas, float centerX, float centerY, float avatarSize, SKColor rankColor, uint rank)
        {
            const int podiumRankBadgeRadius = 12;
            const int podiumRankBadgeOffset = 8;
            const int podiumRankTextOffset = 4;

            float badgeX = centerX + avatarSize / 2f - podiumRankBadgeOffset;
            float badgeY = centerY + avatarSize / 2f - podiumRankBadgeOffset;

            using var rankBadgePaint = new SKPaint();
            rankBadgePaint.IsAntialias = true;
            rankBadgePaint.Color = rankColor;
            canvas.DrawCircle(badgeX, badgeY, podiumRankBadgeRadius, rankBadgePaint);

            canvas.DrawText(rank.ToString(), badgeX, badgeY + podiumRankTextOffset, RankTextPaint);
        }

        static void DrawPodiumPlayerInfo(SKCanvas canvas, LeaderboardPlayer player, SKColor rankColor, float x, float y, float width)
        {
            const int podiumTextSpacing = 20;
            const int podiumLevelXpSpacing = 16;

            float centerX = x + width / 2f;
            float currentY = y;

            canvas.DrawText(player.DisplayName, centerX, currentY, PodiumPlayerInfoNamePaint);

            currentY += podiumTextSpacing;
            canvas.DrawText($"@{player.Username}", centerX, currentY, PodiumPlayerInfoUsernamePaint);

            currentY += podiumTextSpacing;
            using var podiumPlayerInfoStatsPaint = new SKPaint();
            podiumPlayerInfoStatsPaint.IsAntialias = true;
            podiumPlayerInfoStatsPaint.Typeface = SKUtils.RobotoRegular;
            podiumPlayerInfoStatsPaint.TextSize = 14;
            podiumPlayerInfoStatsPaint.TextAlign = SKTextAlign.Center;
            podiumPlayerInfoStatsPaint.Color = rankColor;

            canvas.DrawText($"Level {player.Level}", centerX, currentY, podiumPlayerInfoStatsPaint);

            currentY += podiumLevelXpSpacing;
            canvas.DrawText($"{SKUtils.FormatNumber(player.Xp)} XP", centerX, currentY, podiumPlayerInfoStatsPaint);
        }
    }

    private async Task DrawPlayerList(SKCanvas canvas, float startY)
    {
        const float itemWidth = CardWidth * 0.95f;
        const float itemX = CardWidth * 0.025f;

        float currentY = startY;

        for (int rank = 3; rank < LeaderboardPlayers.Length; rank++)
        {
            LeaderboardPlayer player = Players[rank];

            await DrawListItem(canvas, player, rank + 1, itemX, currentY, itemWidth);
            currentY += ListItemHeight + 8;
        }
    }

    public async Task<byte[]> BuildAsync()
    {
        var info = new SKImageInfo(CardWidth * SKUtils.Scale, CardHeight * SKUtils.Scale);
        using var surface = SKSurface.Create(info);
        SKCanvas canvas = surface.Canvas;
        canvas.Scale(SKUtils.Scale);

        const float opacity = 0.7f;
        SKUtils.DrawSvg(canvas, LeaderboardBackground, new SKRect(0, 0, CardWidth, CardHeight), opacity);

        float currentY = Padding;

        using (SKBitmap headerImage = await SKUtils.GetResizedSKBitmapFromUrlAsync(Guild.IconUrl, HeaderImageSize * SKUtils.Scale, HeaderImageSize * SKUtils.Scale))
        {
            DrawHeader(canvas, headerImage);
        }

        const int headerToPodiumSpacing = 100;
        const int podiumToListSpacing = 20;

        currentY += HeaderImageSize + HeaderSpacing + HeaderSubtitleSpacing + HeaderSubtitleSize;
        currentY += headerToPodiumSpacing;

        await DrawPodium(canvas, currentY);
        currentY += PodiumHeight + WinnerHeightBonus + podiumToListSpacing;

        await DrawPlayerList(canvas, currentY);

        using SKImage image = surface.Snapshot();
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private sealed record PodiumPosition(uint PlayerIndex, SKPoint[] CornerRadii)
    {
        public bool IsFirst { get; } = PlayerIndex == 0;
    }
}
