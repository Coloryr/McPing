using McPing.PingTools;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace McPing;

static class GenShow
{
    private const string PicName = "PicTemp";
    private static string PicDir;

    private static Font FontNormal;
    private static Font FontBold;
    private static Font FontItalic;

    private static TextOptions FontNormalOpt;
    private static TextOptions FontBoldOpt;
    private static TextOptions FontItalicOpt;

    private static Color BackgroundColor;
    private static Color GoodPingColor;
    private static Color BadPingColor;
    private static Color PlayerColor;
    private static Color VersionColor;

    private const int Size = 17;

    private static FontFamily FontEmoji;

    public static bool Init()
    {

        PicDir = $"{Program.RunLocal}{PicName}/";
        if (!Directory.Exists(PicDir))
        {
            Directory.CreateDirectory(PicDir);
        }

        var temp = SystemFonts.Families.Where(a => a.Name == Program.Config.Show.FontNormal).FirstOrDefault();

        var temp1 = SystemFonts.Families.Where(a => a.Name == Program.Config.Show.FontBold).FirstOrDefault();

        var temp2 = SystemFonts.Families.Where(a => a.Name == Program.Config.Show.FontItalic).FirstOrDefault();

        FontEmoji = SystemFonts.Families.Where(a => a.Name == Program.Config.Show.FontEmoji).FirstOrDefault();

        FontNormal = temp.CreateFont(Size);
        FontBold = temp1.CreateFont(Size, FontStyle.Bold);
        FontItalic = temp2.CreateFont(Size, FontStyle.Italic);

        FontNormalOpt = new TextOptions(FontNormal)
        {
            FallbackFontFamilies = new List<FontFamily>() { FontEmoji }
        };

        FontBoldOpt = new TextOptions(FontBold)
        {
            FallbackFontFamilies = new List<FontFamily>() { FontEmoji }
        };

        FontItalicOpt = new TextOptions(FontItalic)
        {
            FallbackFontFamilies = new List<FontFamily>() { FontEmoji }
        };

        BackgroundColor = Color.Parse(Program.Config.Show.BGColor);
        GoodPingColor = Color.Parse(Program.Config.Show.GoodPingColor);
        BadPingColor = Color.Parse(Program.Config.Show.BadPingColor);
        PlayerColor = Color.Parse(Program.Config.Show.PlayerColor);
        VersionColor = Color.Parse(Program.Config.Show.VersionColor);

        return true;
    }

    public enum FontState
    {
        normal, bold, italic
    }

    private static readonly Random _random = new();

    public static void Draw(ref Image image, ref float x, ref float y, bool underline, bool strikethrough,
        FontState now, Color brush, string data)
    {
        FontRectangle res;
        float x1 = x, y1 = y;
        switch (now)
        {
            default:
            case FontState.normal:
                res = TextMeasurer.Measure(data, FontNormalOpt);
                image.Mutate(a => a.DrawText(new TextOptions(FontNormalOpt)
                {
                    Origin = new PointF(x1, y1)
                }, data, brush));
                break;
            case FontState.bold:
                res = TextMeasurer.Measure(data, FontBoldOpt);
                image.Mutate(a => a.DrawText(new TextOptions(FontBoldOpt)
                {
                    Origin = new PointF(x1, y1)
                }, data, brush));
                break;
            case FontState.italic:
                res = TextMeasurer.Measure(data, FontItalicOpt);
                image.Mutate(a => a.DrawText(new TextOptions(FontItalicOpt)
                {
                    Origin = new PointF(x1, y1)
                }, data, brush));
                break;
        }
        if (underline)
        {
            image.Mutate(a => a.DrawLines(brush, 1,
                new PointF(x1, y1 + 21f), new PointF(x1 + res.Width, y1 + 21f)));
        }
        if (strikethrough)
        {
            image.Mutate(a => a.DrawLines(brush, 1,
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
                operation.Clear(BackgroundColor);
            });
            Image bitmap1;
            if (info.MOTD.FaviconByteArray == null)
            {
                bitmap1 = new Image<Rgba32>(64, 64);
            }
            else
            {
                using MemoryStream stream = new();
                stream.Write(info.MOTD.FaviconByteArray);
                stream.Seek(0, SeekOrigin.Begin);
                bitmap1 = Image.Load(stream);
                bitmap1 = Tools.ZoomImage(bitmap1, 64, 64);
            }
            img.Mutate((operation) =>
            {
                operation.DrawImage(bitmap1, new Point(10, 10), 1);
            });
            bitmap1.Dispose();

            float y = 10;
            float x = 80;
            
            DrawChat(ref img, ref x, ref y, info.MOTD.Description);
            y = 50;
            x = 80;
            string data = $"在线人数:{info.MOTD.Players.Online}/{info.MOTD.Players.Max}";
            var res = TextMeasurer.Measure(data, FontItalicOpt);
            img.Mutate(a => a.DrawText(new TextOptions(FontNormalOpt)
            {
                Origin = new PointF(x, y)
            }, data, PlayerColor));
            x += res.Width + 10f;
            data = $"服务器版本:";
            res = TextMeasurer.Measure(data, FontItalicOpt);
            img.Mutate(a => a.DrawText(new TextOptions(FontNormalOpt)
            {
                Origin = new PointF(x, y)
            }, data, VersionColor));
            x += res.Width + 10f;

            var chat = ServerDescriptionJsonConverter.StringToChar(info.MOTD.Version.Name);

            DrawChat(ref img, ref x, ref y, chat);
            img.Mutate(a => a.DrawText(new TextOptions(FontNormalOpt)
            {
                Origin = new PointF(600, 10)
            }, "Ping", GoodPingColor).DrawText(new TextOptions(FontNormalOpt)
            {
                Origin = new PointF(600, 30)
            }, $"{info.MOTD.Ping}", info.MOTD.Ping > 100 ? BadPingColor : GoodPingColor));

            string local = $"{PicDir}{info.MOTD.ServerAddress}_{info.MOTD.ServerPort}.png";
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
