using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McPing
{
    class GenShow
    {
        private const string PicName = "PicTemp";
        private static string PicDir;

        private static StringFormat sf;

        private static Font font_normal;
        private static Font font_bold;
        private static Font font_italic;
        private static Font font_underline;
        private static Font font_strikethrough;
        private static Color bg_color;
        private static Brush good_ping_color;
        private static Brush bad_ping_color;
        private static Brush player_color;
        private static Brush version_color;

        public static void Init()
        {
            sf = StringFormat.GenericTypographic;
            sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;

            PicDir = Program.RunLocal + "PicTemp/";
            if (!Directory.Exists(PicDir))
            {
                Directory.CreateDirectory(PicDir);
            }
            font_normal?.Dispose();
            font_bold?.Dispose();
            font_italic?.Dispose();
            font_underline?.Dispose();
            font_strikethrough?.Dispose();
            good_ping_color?.Dispose();
            bad_ping_color?.Dispose();
            player_color?.Dispose();
            version_color?.Dispose();

            font_normal = new Font(Program.Config.Show.Font, 13);
            font_bold = new Font(Program.Config.Show.Font, 13, FontStyle.Bold);
            font_italic = new Font(Program.Config.Show.Font, 13, FontStyle.Italic);
            font_underline = new Font(Program.Config.Show.Font, 13, FontStyle.Underline);
            font_strikethrough = new Font(Program.Config.Show.Font, 13, FontStyle.Strikeout);

            bg_color = ColorTranslator.FromHtml(Program.Config.Show.BGColor);
            good_ping_color = new SolidBrush(ColorTranslator.FromHtml(Program.Config.Show.GoodPingColor));
            bad_ping_color = new SolidBrush(ColorTranslator.FromHtml(Program.Config.Show.BadPingColor));
            player_color = new SolidBrush(ColorTranslator.FromHtml(Program.Config.Show.PlayerColor));
            version_color = new SolidBrush(ColorTranslator.FromHtml(Program.Config.Show.VersionColor));
        }

        enum FontState
        {
            normal, bold, strikethrough, underline, italic
        }
        private const string randomString = "0123456789abcdef";
        public static string Gen(ServerInfo info)
        {
            using Bitmap bitmap = new Bitmap(660, 86);
            using Graphics graphics = Graphics.FromImage(bitmap);
            graphics.InterpolationMode = InterpolationMode.High;
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            graphics.Clear(bg_color);
            Bitmap bitmap1;
            if (info.IconData == null)
            {
                bitmap1 = new(64, 64);
            }
            else
            {

                using MemoryStream stream = new MemoryStream();
                stream.Write(info.IconData);
                stream.Seek(0, SeekOrigin.Begin);
                bitmap1 = Image.FromStream(stream) as Bitmap;
                bitmap1 = Tools.ZoomImage(bitmap1, 64, 64);
            }
            graphics.DrawImage(bitmap1, 10, 10);
            bitmap1.Dispose();

            var temp = info.MOTD.Split('\n');
            float y = 10;
            float x = 80;
            SizeF res;
            Brush brush;
            FontState now;
            string[] temp1;
            foreach (var item in temp)
            {
                x = 80;
                brush = Brushes.White;
                now = FontState.normal;
                temp1 = item.Split("§");
                foreach (var item2 in temp1)
                {
                    if (item2.Length == 0)
                        continue;
                    char color = item2.ToLower()[0];
                    string draw = "";

                    if (color == 'k')
                    {
                        brush = GetBrush(randomString[new Random().Next(randomString.Length - 1)]);
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
                        brush = Brushes.White;
                        draw = item2[1..];
                    }
                    else
                    {
                        var temp2 = GetBrush(color);
                        if (temp2 == null)
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
                            res = graphics.MeasureString(draw, font_normal, PointF.Empty, sf);
                            graphics.DrawString(draw, font_normal, brush, x, y);
                            break;
                        case FontState.bold:
                            res = graphics.MeasureString(draw, font_bold, PointF.Empty, sf);
                            graphics.DrawString(draw, font_bold, brush, x, y);
                            break;
                        case FontState.strikethrough:
                            res = graphics.MeasureString(draw, font_strikethrough, PointF.Empty, sf);
                            graphics.DrawString(draw, font_strikethrough, brush, x, y);
                            break;
                        case FontState.underline:
                            res = graphics.MeasureString(draw, font_underline, PointF.Empty, sf);
                            graphics.DrawString(draw, font_underline, brush, x, y);
                            break;
                        case FontState.italic:
                            res = graphics.MeasureString(draw, font_italic, PointF.Empty, sf);
                            graphics.DrawString(draw, font_italic, brush, x, y);
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
            res = graphics.MeasureString(data, font_italic);
            graphics.DrawString(data, font_normal, player_color, x, y);
            x += res.Width;
            data = $"服务器版本:";
            res = graphics.MeasureString(data, font_italic);
            graphics.DrawString(data, font_normal, version_color, x, y);
            x += res.Width;
            brush = version_color;
            now = FontState.normal;
            temp1 = info.GameVersion.Split("§");
            foreach (var item2 in temp1)
            {
                if (item2.Length == 0)
                    continue;
                char color = item2.ToLower()[0];
                string draw = "";

                if (color == 'k')
                {
                    brush = GetBrush(randomString[new Random().Next(randomString.Length - 1)]);
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
                    brush = Brushes.White;
                    draw = item2[1..];
                }
                else
                {
                    var temp2 = GetBrush(color);
                    if (temp2 == null)
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
                        res = graphics.MeasureString(draw, font_normal, PointF.Empty, sf);
                        graphics.DrawString(draw, font_normal, brush, x, y);
                        break;
                    case FontState.bold:
                        res = graphics.MeasureString(draw, font_bold, PointF.Empty, sf);
                        graphics.DrawString(draw, font_bold, brush, x, y);
                        break;
                    case FontState.strikethrough:
                        res = graphics.MeasureString(draw, font_strikethrough, PointF.Empty, sf);
                        graphics.DrawString(draw, font_strikethrough, brush, x, y);
                        break;
                    case FontState.underline:
                        res = graphics.MeasureString(draw, font_underline, PointF.Empty, sf);
                        graphics.DrawString(draw, font_underline, brush, x, y);
                        break;
                    case FontState.italic:
                        res = graphics.MeasureString(draw, font_italic, PointF.Empty, sf);
                        graphics.DrawString(draw, font_italic, brush, x, y);
                        break;
                }
                x += res.Width;
                if (x > 580)
                {
                    graphics.DrawString("...", font_normal, version_color, x, y);
                }
            }

            graphics.DrawString($"Ping", font_normal, good_ping_color, 600, 10);
            graphics.DrawString($"{info.Ping}", font_normal, info.Ping > 100 ? bad_ping_color : good_ping_color, 600, 30);

            string local = PicDir + info.IP + ".jpg";
            bitmap.Save(local);
            Program.LogOut("生成图片" + local);
            return local;
        }

        private static Brush GetBrush(char color)
        {
            switch (color)
            {
                case '0':
                    return Brushes.Black;
                case '1':
                    return Brushes.MediumBlue;
                case '2':
                    return Brushes.LimeGreen;
                case '3':
                    return Brushes.DarkTurquoise;
                case '4':
                    return Brushes.Firebrick;
                case '5':
                    return Brushes.DarkOrange;
                case '6':
                    return Brushes.Goldenrod;
                case '7':
                    return Brushes.Gainsboro;
                case '8':
                    return Brushes.DimGray;
                case '9':
                    return Brushes.RoyalBlue;
                case 'a':
                    return Brushes.Lime;
                case 'b':
                    return Brushes.Aqua;
                case 'c':
                    return Brushes.Tomato;
                case 'd':
                    return Brushes.Magenta;
                case 'e':
                    return Brushes.Yellow;
                case 'f':
                    return Brushes.White;
                default:
                    return null;
            }
        }
    }
}
