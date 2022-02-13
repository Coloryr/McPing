using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace McPing
{
    class GenShow
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

            PicDir = Program.RunLocal + "PicTemp/";
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

        enum FontState
        {
            normal, bold, italic
        }
        private const string randomString = "0123456789abcdef";
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
                if (info.IconData == null)
                {
                    bitmap1 = new Image<Rgba32>(64, 64);
                }
                else
                {
                    using MemoryStream stream = new();
                    stream.Write(info.IconData);
                    stream.Seek(0, SeekOrigin.Begin);
                    bitmap1 = Image.Load(stream);
                    bitmap1 = Tools.ZoomImage(bitmap1, 64, 64);
                }
                img.Mutate((operation) =>
                {
                    operation.DrawImage(bitmap1, new Point(10, 10), 1);
                });
                bitmap1.Dispose();

                var temp = info.MOTD.Split('\n');
                float y = 10;
                float x = 80;
                FontRectangle res;
                Color brush;
                FontState now;
                string[] temp1;
                bool isStart = false;
                bool underline = false;
                bool strikethrough = false;
                foreach (var item in temp)
                {
                    strikethrough = false;
                    underline = false;
                    x = 80;
                    brush = Color.White;
                    now = FontState.normal;
                    temp1 = item.Split("§");
                    if (item.StartsWith("§"))
                        isStart = true;
                    foreach (var item2 in temp1)
                    {
                        if (item2.Length == 0)
                            continue;
                        string draw = "";
                        char color = item2.ToLower()[0];

                        if (!isStart)
                        {
                            draw = item2;
                            isStart = true;
                        }
                        else if (color == '#')
                        {
                            string color1 = item2[..7];
                            brush = Color.Parse(color1);
                            draw = item2[7..];
                        }
                        else if (color == 'k')
                        {
                            GetBrush(randomString[new Random().Next(randomString.Length - 1)], out brush);
                        }
                        else if (color == 'l')
                        {
                            now = FontState.bold;
                            draw = item2[1..];
                        }
                        else if (color == 'm')
                        {
                            strikethrough = true;
                            draw = item2[1..];
                        }
                        else if (color == 'n')
                        {
                            underline = true;
                            draw = item2[1..];
                        }
                        else if (color == 'o')
                        {
                            now = FontState.italic;
                            draw = item2[1..];
                        }
                        else if (color == 'r')
                        {
                            strikethrough = false;
                            underline = false;
                            now = FontState.normal;
                            brush = Color.White;
                            draw = item2[1..];
                        }
                        else
                        {
                            if (!GetBrush(color, out var temp2))
                            {
                                draw = item2;
                            }
                            else
                            {
                                brush = temp2;
                                draw = item2[1..];
                            }
                        }

                        isStart = true;

                        if (draw.Length == 0)
                        {
                            continue;
                        }

                        switch (now)
                        {
                            default:
                            case FontState.normal:
                                res = TextMeasurer.Measure(draw, FontNormalOpt);
                                img.Mutate(a => a.DrawText(new TextOptions(FontNormalOpt)
                                {
                                    Origin = new PointF(x, y)
                                }, draw, brush));
                                break;
                            case FontState.bold:
                                res = TextMeasurer.Measure(draw, FontBoldOpt);
                                img.Mutate(a => a.DrawText(new TextOptions(FontBoldOpt)
                                {
                                    Origin = new PointF(x, y)
                                }, draw, brush));
                                break;
                            case FontState.italic:
                                res = TextMeasurer.Measure(draw, FontItalicOpt);
                                img.Mutate(a => a.DrawText(new TextOptions(FontItalicOpt)
                                {
                                    Origin = new PointF(x, y)
                                }, draw, brush));
                                break;
                        }

                        if (underline)
                        {
                            img.Mutate(a => a.DrawLines(brush, 1, new PointF(x, y + 21f), new PointF(x + res.Width, y + 21f)));
                        }
                        if (strikethrough)
                        {
                            img.Mutate(a => a.DrawLines(brush, 1, new PointF(x, y + 12f), new PointF(x + res.Width, y + 12f)));
                        }

                        x += res.Width;
                        if (x > 580)
                            break;
                    }
                    y += 20;
                }
                y = 50;
                x = 80;
                string data = $"在线人数:{info.CurrentPlayerCount}/{info.MaxPlayerCount}";
                res = TextMeasurer.Measure(data, FontItalicOpt);
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
                brush = VersionColor;
                now = FontState.normal;
                temp1 = ("r" + info.GameVersion).Split("§");
                if (info.GameVersion.StartsWith("§"))
                    isStart = true;
                foreach (var item2 in temp1)
                {
                    if (item2.Length == 0)
                        continue;
                    char color = item2.ToLower()[0];
                    string draw = "";

                    if (!isStart)
                    {
                        draw = item2;
                        isStart = true;
                    }
                    else if (color == 'k')
                    {
                        GetBrush(randomString[new Random().Next(randomString.Length - 1)], out brush);
                    }
                    else if (color == 'l')
                    {
                        now = FontState.bold;
                        draw = item2[1..];
                    }
                    else if (color == 'm')
                    {
                        strikethrough = true;
                        draw = item2[1..];
                    }
                    else if (color == 'n')
                    {
                        underline = true;
                        draw = item2[1..];
                    }
                    else if (color == 'o')
                    {
                        now = FontState.italic;
                        draw = item2[1..];
                    }
                    else if (color == 'r')
                    {
                        strikethrough = false;
                        underline = false;
                        now = FontState.normal;
                        brush = Color.White;
                        draw = item2[1..];
                    }
                    else
                    {
                        if (!GetBrush(color, out var temp2))
                        {
                            draw = item2;
                        }
                        else
                        {
                            brush = temp2;
                            draw = item2[1..];
                        }
                    }
                    isStart = true;

                    if (draw.Length == 0)
                    {
                        continue;
                    }

                    switch (now)
                    {
                        default:
                        case FontState.normal:
                            res = TextMeasurer.Measure(draw, FontNormalOpt);
                            img.Mutate(a => a.DrawText(new TextOptions(FontNormalOpt)
                            {
                                Origin = new PointF(x, y)
                            }, draw, brush));
                            break;
                        case FontState.bold:
                            res = TextMeasurer.Measure(draw, FontBoldOpt);
                            img.Mutate(a => a.DrawText(new TextOptions(FontBoldOpt)
                            {
                                Origin = new PointF(x, y)
                            }, draw, brush));
                            break;
                        case FontState.italic:
                            res = TextMeasurer.Measure(draw, FontItalicOpt);
                            img.Mutate(a => a.DrawText(new TextOptions(FontItalicOpt)
                            {
                                Origin = new PointF(x, y)
                            }, draw, brush));
                            break;
                    }

                    if (underline)
                    {
                        img.Mutate(a => a.DrawLines(brush, 1, new PointF(x, y + 21f), new PointF(x + res.Width, y + 21f)));
                    }
                    if (strikethrough)
                    {
                        img.Mutate(a => a.DrawLines(brush, 1, new PointF(x, y + 12f), new PointF(x + res.Width, y + 12f)));
                    }

                    x += res.Width;
                    if (x > 580)
                    {
                        img.Mutate(a => a.DrawText(new TextOptions(FontNormalOpt)
                        {
                            Origin = new PointF(x, y)
                        }, "...", VersionColor));
                    }
                }

                img.Mutate(a => a.DrawText(new TextOptions(FontNormalOpt)
                {
                    Origin = new PointF(600, 10)
                }, "Ping", GoodPingColor).DrawText(new TextOptions(FontNormalOpt)
                {
                    Origin = new PointF(600, 30)
                }, $"{info.Ping}", info.Ping > 100 ? BadPingColor : GoodPingColor));

                string local = PicDir + info.IP + ".png";
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

        private static Color C0 = Color.Parse("#000000");
        private static Color C1 = Color.Parse("#0000AA");
        private static Color C2 = Color.Parse("#00AA00");
        private static Color C3 = Color.Parse("#00AAAA");
        private static Color C4 = Color.Parse("#AA0000");
        private static Color C5 = Color.Parse("#AA00AA");
        private static Color C6 = Color.Parse("#FFAA00");
        private static Color C7 = Color.Parse("#AAAAAA");
        private static Color C8 = Color.Parse("#555555");
        private static Color C9 = Color.Parse("#5555FF");
        private static Color Ca = Color.Parse("#55FF55");
        private static Color Cb = Color.Parse("#55FFFF");
        private static Color Cc = Color.Parse("#FF5555");
        private static Color Cd = Color.Parse("#FF55FF");
        private static Color Ce = Color.Parse("#FFFF55");
        private static Color Cf = Color.Parse("#FFFFFF");

        private static bool GetBrush(char color, out Color color1)
        {
            switch (color)
            {
                case '0':
                    color1 = C0;
                    return true;
                case '1':
                    color1 = C1;
                    return true;
                case '2':
                    color1 = C2;
                    return true;
                case '3':
                    color1 = C3;
                    return true;
                case '4':
                    color1 = C4;
                    return true;
                case '5':
                    color1 = C5;
                    return true;
                case '6':
                    color1 = C6;
                    return true;
                case '7':
                    color1 = C7;
                    return true;
                case '8':
                    color1 = C8;
                    return true;
                case '9':
                    color1 = C9;
                    return true;
                case 'a':
                    color1 = Ca;
                    return true;
                case 'b':
                    color1 = Cb;
                    return true;
                case 'c':
                    color1 = Cc;
                    return true;
                case 'd':
                    color1 = Cd;
                    return true;
                case 'e':
                    color1 = Ce;
                    return true;
                case 'f':
                    color1 = Cf;
                    return true;
                default:
                    color1 = Cf;
                    return false;
            }
        }
    }
}
