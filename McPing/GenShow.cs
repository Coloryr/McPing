using McPing.PingTools;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace McPing;

static class GenShow
{
    private const string _picName = "PicTemp";
    private static string _picDir;

    private static Font _fontNormal;
    private static Font _fontBold;
    private static Font _fontItalic;

    private static RichTextOptions _fontNormalOpt;
    private static RichTextOptions _fontBoldOpt;
    private static RichTextOptions _fontItalicOpt;

    private static Color _backgroundColor;
    private static Color _goodPingColor;
    private static Color _badPingColor;
    private static Color _playerColor;
    private static Color _versionColor;

    private static readonly Random _random = new();

    private const int _size = 17;

    private static FontFamily _fontEmoji;

    public static bool Init()
    {
        _picDir = $"{Program.RunLocal}{_picName}/";
        if (!Directory.Exists(_picDir))
        {
            Directory.CreateDirectory(_picDir);
        }

        var temp = SystemFonts.Families.Where(a => a.Name == Program.Config.Show.FontNormal).FirstOrDefault();
        var temp1 = SystemFonts.Families.Where(a => a.Name == Program.Config.Show.FontBold).FirstOrDefault();
        var temp2 = SystemFonts.Families.Where(a => a.Name == Program.Config.Show.FontItalic).FirstOrDefault();

        _fontEmoji = SystemFonts.Families.Where(a => a.Name == Program.Config.Show.FontEmoji).FirstOrDefault();

        _fontNormal = temp.CreateFont(_size);
        _fontBold = temp1.CreateFont(_size, FontStyle.Bold);
        _fontItalic = temp2.CreateFont(_size, FontStyle.Italic);

        _fontNormalOpt = new RichTextOptions(_fontNormal)
        {
            FallbackFontFamilies = [_fontEmoji]
        };

        _fontBoldOpt = new RichTextOptions(_fontBold)
        {
            FallbackFontFamilies = [_fontEmoji]
        };

        _fontItalicOpt = new RichTextOptions(_fontItalic)
        {
            FallbackFontFamilies = [_fontEmoji]
        };

        _backgroundColor = Color.Parse(Program.Config.Show.BGColor);
        _goodPingColor = Color.Parse(Program.Config.Show.GoodPingColor);
        _badPingColor = Color.Parse(Program.Config.Show.BadPingColor);
        _playerColor = Color.Parse(Program.Config.Show.PlayerColor);
        _versionColor = Color.Parse(Program.Config.Show.VersionColor);

        return true;
    }

    public enum FontState
    {
        normal, bold, italic
    }

    public static void Draw(ref Image image, ref float x, ref float y, bool underline, bool strikethrough,
        FontState now, Color brush, string data)
    {
        FontRectangle res;
        float x1 = x, y1 = y;
        switch (now)
        {
            default:
            case FontState.normal:
                res = TextMeasurer.MeasureSize(data, _fontNormalOpt);
                image.Mutate(a => a.DrawText(new RichTextOptions(_fontNormalOpt)
                {
                    Origin = new PointF(x1, y1)
                }, data, brush));
                break;
            case FontState.bold:
                res = TextMeasurer.MeasureSize(data, _fontBoldOpt);
                image.Mutate(a => a.DrawText(new RichTextOptions(_fontBoldOpt)
                {
                    Origin = new PointF(x1, y1)
                }, data, brush));
                break;
            case FontState.italic:
                res = TextMeasurer.MeasureSize(data, _fontItalicOpt);
                image.Mutate(a => a.DrawText(new RichTextOptions(_fontItalicOpt)
                {
                    Origin = new PointF(x1, y1)
                }, data, brush));
                break;
        }
        if (underline)
        {
            image.Mutate(a => a.DrawLine(brush, 1,
                new PointF(x1, y1 + 21f), new PointF(x1 + res.Width, y1 + 21f)));
        }
        if (strikethrough)
        {
            image.Mutate(a => a.DrawLine(brush, 1,
                new PointF(x1, y1 + 12f), new PointF(x1 + res.Width, y1 + 12f)));
        }

        x += res.Width;
    }

    public static void DrawChat(ref Image image, ref float x, ref float y, Chat chat)
    {
        if (x > 580)
            return;
        if (chat.Text == "\n")
        {
            x = 80;
            y += 20;
            return;
        }

        if (!string.IsNullOrEmpty(chat.Text))
        {
            string text = chat.Obfuscated ? " " : chat.Text;
            Color color = chat.Color == null ? Color.White
                    : FixColor(chat.Color);

            FontState now = FontState.normal;
            if (chat.Bold)
            {
                now = FontState.bold;
            }
            if (chat.Italic)
            {
                now = FontState.italic;
            }

            if (chat.Obfuscated)
            {
                text = new string((char)_random.Next(33, 126), 1);
            }

            Draw(ref image, ref x, ref y, chat.Underlined, chat.Strikethrough, now, color, text);

        }

        if (chat.Extra != null)
        {
            foreach (var item in chat.Extra)
            {
                DrawChat(ref image, ref x, ref y, item);
            }
        }
    }

    public static string Gen(IServerInfo info)
    {
        try
        {
            Image img = new Image<Rgba32>(660, 84);
            img.Mutate((operation) =>
            {
                operation.Clear(_backgroundColor);
            });
            Image bitmap1;
            if (info.ServerMotd.FaviconByteArray == null)
            {
                bitmap1 = new Image<Rgba32>(64, 64);
            }
            else
            {
                try
                {
                    using var stream = new MemoryStream(info.ServerMotd.FaviconByteArray);
                    bitmap1 = Image.Load(stream);
                    bitmap1 = Tools.ZoomImage(bitmap1, 64, 64);
                }
                catch
                {
                    bitmap1 = new Image<Rgba32>(64, 64);
                }
            }
            img.Mutate((operation) =>
            {
                operation.DrawImage(bitmap1, new Point(10, 10), 1);
            });
            bitmap1.Dispose();

            float y = 10;
            float x = 80;

            DrawChat(ref img, ref x, ref y, info.ServerMotd.Description);
            y = 55;
            x = 80;
            string data = $"在线人数:{info.ServerMotd.Players.Online}/{info.ServerMotd.Players.Max}";
            var res = TextMeasurer.MeasureSize(data, _fontItalicOpt);
            img.Mutate(a => a.DrawText(new RichTextOptions(_fontNormalOpt)
            {
                Origin = new PointF(x, y)
            }, data, _playerColor));
            x += res.Width + 10f;
            data = $"服务器版本:";
            res = TextMeasurer.MeasureSize(data, _fontItalicOpt);
            img.Mutate(a => a.DrawText(new RichTextOptions(_fontNormalOpt)
            {
                Origin = new PointF(x, y)
            }, data, _versionColor));
            x += res.Width + 10f;

            var chat = ServerDescriptionJsonConverter.StringToChar(info.ServerMotd.Version.Name);

            DrawChat(ref img, ref x, ref y, chat);
            img.Mutate(a => a.DrawText(new RichTextOptions(_fontNormalOpt)
            {
                Origin = new PointF(600, 10)
            }, "Ping", _goodPingColor).DrawText(new RichTextOptions(_fontNormalOpt)
            {
                Origin = new PointF(600, 30)
            }, $"{info.ServerMotd.Ping}", info.ServerMotd.Ping > 100 ? _badPingColor : _goodPingColor));

            string local = $"{_picDir}{info.ServerMotd.ServerAddress}_{info.ServerMotd.ServerPort}.png";
            img.SaveAsPng(local);
            Program.LogOut("生成图片" + local);
            return local;
        }
        catch (Exception e)
        {
            Program.LogError(e);
        }
        return null;
    }

    private readonly static Dictionary<string, Color> ColorMap = new()
    {
        { "black", Color.Parse("#000000") },
        { "dark_blue", Color.Parse("#0000AA") },
        { "dark_green", Color.Parse("#00AA00") },
        { "dark_aqua", Color.Parse("#00AAAA") },
        { "dark_red", Color.Parse("#AA0000") },
        { "dark_purple", Color.Parse("#AA00AA") },
        { "gold", Color.Parse("#FFAA00") },
        { "gray", Color.Parse("#AAAAAA") },
        { "dark_gray", Color.Parse("#555555") },
        { "blue", Color.Parse("#5555FF") },
        { "green", Color.Parse("#55FF55") },
        { "aqua", Color.Parse("#55FFFF") },
        { "red", Color.Parse("#FF5555") },
        { "light_purple", Color.Parse("#FF55FF") },
        { "yellow", Color.Parse("#FFFF55") },
        { "white", Color.Parse("#FFFFFF") }
    };

    private static Color FixColor(string color)
    {
        if (color.StartsWith('#'))
            return Color.Parse(color);
        if (ColorMap.TryGetValue(color, out var color1))
        {
            return color1;
        }

        return Color.White;
    }
}
