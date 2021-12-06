using System;
using System.IO;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Linq;

namespace McPing
{
    class GenShow
    {
        private const string PicName = "PicTemp";
        private static string PicDir;

        private static Font font_normal;
        private static Font font_bold;
        private static Font font_italic;
        private static Font font_underline;
        private static Font font_strikethrough;
        private static Color bg_color;
        private static Color good_ping_color;
        private static Color bad_ping_color;
        private static Color player_color;
        private static Color version_color;

        private const int size = 17;

        public static bool Init()
        {

            PicDir = Program.RunLocal + "PicTemp/";
            if (!Directory.Exists(PicDir))
            {
                Directory.CreateDirectory(PicDir);
            }

            FontFamily fontFamily = SystemFonts.Families.Where(a => a.Name == Program.Config.Show.Font).FirstOrDefault();
            if (fontFamily == null)
            {
                Program.LogError($"找不到字体{Program.Config.Show.Font}");
                return false;
            }

            font_normal = fontFamily.CreateFont(size);
            font_bold = fontFamily.CreateFont(size, FontStyle.Bold);
            font_italic = fontFamily.CreateFont(size, FontStyle.Italic);
            font_underline = fontFamily.CreateFont(size);
            font_strikethrough = fontFamily.CreateFont(size);

            bg_color = Color.Parse(Program.Config.Show.BGColor);
            good_ping_color = Color.Parse(Program.Config.Show.GoodPingColor);
            bad_ping_color = Color.Parse(Program.Config.Show.BadPingColor);
            player_color = Color.Parse(Program.Config.Show.PlayerColor);
            version_color = Color.Parse(Program.Config.Show.VersionColor);

            return true;
        }

        enum FontState
        {
            normal, bold, strikethrough, underline, italic
        }
        private const string randomString = "0123456789abcdef";
        public static string Gen(ServerInfo info)
        {
            var textOptions = new TextOptions()
            {
                ApplyKerning = true,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var graphicsOptions = new GraphicsOptions()
            {
                Antialias = true,
                AntialiasSubpixelDepth = 256
            };

            Image img = new Image<Rgba32>(660, 84);
            img.Mutate((operation)=> 
            {
                operation.SetGraphicsOptions(graphicsOptions);
                operation.Clear(bg_color);
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
            img.Mutate((operation) => {
                operation.SetGraphicsOptions(graphicsOptions);
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
            foreach (var item in temp)
            {
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
                    if (color == '#')
                    {
                        string color1 = item2[..7];
                        brush = Color.Parse(color1);
                        draw = item2[7..];
                    }
                    else if (isStart && color is 'k' or 'l' or 'm' or 'n' or 'o' or 'r')
                    {
                        draw = item2;
                        isStart = false;
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
                        now = FontState.strikethrough;
                        draw = item2[1..];
                    }
                    else if (color == 'n')
                    {
                        now = FontState.underline;
                        draw = item2[1..];
                    }
                    else if (color == 'o')
                    {
                        now = FontState.italic;
                        draw = item2[1..];
                    }
                    else if (color == 'r')
                    {
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
                    if (draw.Length == 0)
                        continue;
                    switch (now)
                    {
                        default:
                        case FontState.normal:
                            res = TextMeasurer.Measure(draw, new RendererOptions(font_normal));
                            img.Mutate((a) =>
                            {
                                a.SetGraphicsOptions(graphicsOptions);
                                a.DrawText(draw, font_normal, brush, new PointF(x, y));
                            });
                            break;
                        case FontState.bold:
                            res = TextMeasurer.Measure(draw, new RendererOptions(font_bold));
                            img.Mutate((a) =>
                            {
                                a.SetGraphicsOptions(graphicsOptions);
                                a.DrawText(draw, font_bold, brush, new PointF(x, y));
                            });
                            break;
                        case FontState.strikethrough:
                            res = TextMeasurer.Measure(draw, new RendererOptions(font_strikethrough));
                            img.Mutate((a) =>
                            {
                                a.SetGraphicsOptions(graphicsOptions);
                                a.DrawText(draw, font_strikethrough, brush, new PointF(x, y));
                                a.DrawLines(brush, 1, new PointF(x, y + 6.5f), new PointF(x + res.Width, y + 6.5f));
                            });
                            break;
                        case FontState.underline:
                            res = TextMeasurer.Measure(draw, new RendererOptions(font_underline));
                            img.Mutate((a) =>
                            {
                                a.SetGraphicsOptions(graphicsOptions);
                                a.DrawText(draw, font_underline, brush, new PointF(x, y));
                                a.DrawLines(brush, 1, new PointF(x, y + 13f), new PointF(x + res.Width, y + 13f));
                            });
                            break;
                        case FontState.italic:
                            res = TextMeasurer.Measure(draw, new RendererOptions(font_italic));
                            img.Mutate((a) =>
                            {
                                a.SetGraphicsOptions(graphicsOptions);
                                a.DrawText(draw, font_italic, brush, new PointF(x, y));
                            });
                            break;
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
            res = TextMeasurer.Measure(data, new RendererOptions(font_italic));
            img.Mutate((a) =>
            {
                a.SetGraphicsOptions(graphicsOptions);
                a.DrawText(data, font_normal, player_color, new PointF(x, y));
            });
            x += res.Width + 10f;
            data = $"服务器版本:";
            res = TextMeasurer.Measure(data, new RendererOptions(font_italic));
            img.Mutate((a) =>
            {
                a.SetGraphicsOptions(graphicsOptions);
                a.DrawText(data, font_normal, version_color, new PointF(x, y));
            });
            x += res.Width + 10f;
            brush = version_color;
            now = FontState.normal;
            temp1 = ("r" + info.GameVersion).Split("§");
            foreach (var item2 in temp1)
            {
                if (item2.Length == 0)
                    continue;
                char color = item2.ToLower()[0];
                string draw = "";

                if (color == 'k')
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
                    now = FontState.strikethrough;
                    draw = item2[1..];
                }
                else if (color == 'n')
                {
                    now = FontState.underline;
                    draw = item2[1..];
                }
                else if (color == 'o')
                {
                    now = FontState.italic;
                    draw = item2[1..];
                }
                else if (color == 'r')
                {
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
                if (draw.Length == 0)
                    continue;
                switch (now)
                {
                    default:
                    case FontState.normal:
                        res = TextMeasurer.Measure(draw, new RendererOptions(font_normal));
                        img.Mutate((a) =>
                        {
                            a.SetGraphicsOptions(graphicsOptions);
                            a.DrawText(draw, font_normal, brush, new PointF(x, y));
                        });
                        break;
                    case FontState.bold:
                        res = TextMeasurer.Measure(draw, new RendererOptions(font_bold));
                        img.Mutate((a) =>
                        {
                            a.SetGraphicsOptions(graphicsOptions);
                            a.DrawText(draw, font_bold, brush, new PointF(x, y));
                        });
                        break;
                        //删除线
                    case FontState.strikethrough:
                        res = TextMeasurer.Measure(draw, new RendererOptions(font_strikethrough));
                        img.Mutate((a) =>
                        {
                            a.SetGraphicsOptions(graphicsOptions);
                            a.DrawText(draw, font_strikethrough, brush, new PointF(x, y));
                            a.DrawLines(brush, 1, new PointF(x, y + 6.5f), new PointF(x + res.Width, y + 6.5f));
                        });
                        break;
                        //下划线
                    case FontState.underline:
                        res = TextMeasurer.Measure(draw, new RendererOptions(font_underline));
                        img.Mutate((a) =>
                        {
                            a.SetGraphicsOptions(graphicsOptions);
                            a.DrawText(draw, font_underline, brush, new PointF(x, y));
                            a.DrawLines(brush, 1, new PointF(x, y + 13f), new PointF(x + res.Width, y + 13f));
                        });
                        break;
                    case FontState.italic:
                        res = TextMeasurer.Measure(draw, new RendererOptions(font_italic));
                        img.Mutate((a) =>
                        {
                            a.SetGraphicsOptions(graphicsOptions);
                            a.DrawText(draw, font_italic, brush, new PointF(x, y));
                        });
                        break;
                }
                x += res.Width;
                if (x > 580)
                {
                    img.Mutate((a) =>
                    {
                        a.SetGraphicsOptions(graphicsOptions);
                        a.DrawText("...", font_normal, version_color, new PointF(x, y));
                    });
                }
            }

            img.Mutate((a) =>
            {
                a.SetGraphicsOptions(graphicsOptions);
                a.DrawText("Ping", font_normal, good_ping_color, new PointF(600, 10));
                a.DrawText($"{info.Ping}", font_normal, info.Ping > 100 ? bad_ping_color : good_ping_color, new PointF(600, 30));
            });

            string local = PicDir + info.IP + ".png";
            img.SaveAsPng(local);
            Program.LogOut("生成图片" + local);
            return local;
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
