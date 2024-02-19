using McPing.PingTools;
using McPing.Robot;
using SixLabors.Fonts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace McPing;

static class Program
{
    public const string Version = "1.9.0";
    public static string RunLocal { get; private set; }
    public static ConfigObj Config { get; private set; }

    private static Logs logs;
    private static bool have;

    private static readonly ConcurrentDictionary<long, int> DelaySave = new();
    private static Timer timer = new(Tick, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

    private static void Tick(object sender)
    {
        List<long> remove = new();
        foreach (var item in DelaySave)
        {
            DelaySave.TryUpdate(item.Key, item.Value - 1, item.Value);
            if (item.Value <= 1)
            {
                remove.Add(item.Key);
            }
        }

        foreach (var item in remove)
        {
            DelaySave.Remove(item, out var temp);
        }
    }

    public static void Message(GroupMessagePack pack)
    {
        if (Config.Group.Contains(pack.group_id))
        {
            if (pack.message[0].type != "text")
            {
                return;
            }
            var message = pack.message[0].data.text.Split(' ');
            if (message[0] == Config.Head)
            {
                if (have)
                {
                    Task.Run(async () =>
                    {
                        SendMessageGroup(pack.group_id, $"正在获取[{Config.DefaultIP}]");
                        string local = await PingUtils.Get(Config.DefaultIP);
                        if (local == null)
                        {
                            SendMessageGroup(pack.group_id, $"获取[{Config.DefaultIP}]错误");
                        }
                        else
                        {
                            SendMessageGroupImg(pack.group_id, local);
                        }
                    });
                    return;
                }
                if (message.Length == 1)
                {
                    SendMessageGroup(pack.group_id, $"输入{Config.Head} [IP] [端口](可选) 来生成服务器Motd图片，支持JAVA版和BE版");
                    return;
                }
                if (DelaySave.ContainsKey(pack.user_id))
                {
                    SendMessageGroup(pack.group_id, $"查询过于频繁");
                    return;
                }
                var ip = message[1];
                if (message.Length == 3)
                {
                    var port = message[2];
                    if (string.IsNullOrWhiteSpace(ip))
                    {
                        Get(pack.group_id, port);
                    }
                    else
                    {
                        Task.Run(async () =>
                        {
                            SendMessageGroup(pack.group_id, $"正在获取[{ip}:{port}]");
                            string local = await PingUtils.Get(ip, port);
                            if (local == null)
                            {
                                SendMessageGroup(pack.group_id, $"获取[{ip}:{port}]错误");
                            }
                            else
                            {
                                SendMessageGroupImg(pack.group_id, local);
                            }
                        });
                    }
                }
                else
                {
                    Get(pack.group_id, ip);
                }
                DelaySave.TryAdd(pack.user_id, Config.Delay);
            }
        }
    }

    private static void Get(long group, string ip)
    {
        Task.Run(async () =>
        {
            SendMessageGroup(group, $"正在获取[{ip}]");
            string local = await PingUtils.Get(ip);
            if (local == null)
            {
                SendMessageGroup(group, $"获取[{ip}]错误");
            }
            else
            {
                SendMessageGroupImg(group, local);
            }
        });
    }

    public record Pairing
    {
        public string Name { get; }
        public string Path { get; }
        public Pairing(string name, string path)
        {
            Name = name;
            Path = path;
        }
    }

    static async Task Main()
    {
        Console.WriteLine($"[Main]正在启动McPing {Version}");
        RunLocal = AppContext.BaseDirectory;
        logs = new Logs(RunLocal);
        Config = ConfigUtils.Config(new ConfigObj()
        {
            Robot = new RobotObj()
            {
                Url = "ws://127.0.0.1:8081/"
            },
            Group = [],
            Show = new ShowObj()
            {
                FontNormal = "Microsoft YaHei",
                FontBold = "Microsoft YaHei",
                FontItalic = "Microsoft YaHei",
                FontEmoji = "Segoe UI Emoji",
                BGColor = "#1C1C1C",
                GoodPingColor = "#7CFC00",
                BadPingColor = "#FF4500",
                PlayerColor = "#A020F0",
                VersionColor = "#8B795E"
            },
            Head = "#mc",
            DefaultIP = "",
            Delay = 10
        }, RunLocal + "config.json");

        have = !string.IsNullOrWhiteSpace(Config.DefaultIP);

        if (!GenShow.Init())
        {
            LogError("初始化错误");
            return;
        }

        LogOut("正在连接ColorMirai");

        RobotCore.Start();

        while (true)
        {
            if (Config.NoInput)
                return;
            string comm = Console.ReadLine();
            var arg = comm.Split(' ');
            if (arg[0] == "stop")
            {
                timer.Dispose();
                LogOut("正在退出");
                RobotCore.Stop();
                return;
            }
            else if (arg[0] == "font")
            {
                IOrderedEnumerable<FontFamily> ordered = SystemFonts.Families.OrderBy(x => x.Name);
                foreach (FontFamily family in ordered)
                {
                    var pairings = new List<Pairing>();
                    IOrderedEnumerable<FontStyle> styles = family.GetAvailableStyles().OrderBy(x => x);
                    foreach (FontStyle style in styles)
                    {
                        Font font = family.CreateFont(0F, style);
                        font.TryGetPath(out string path);
                        pairings.Add(new Pairing(font.Name, path));
                    }
                    Console.WriteLine($"{family.Name}");
                    int max = pairings.Max(x => x.Name.Length);
                    foreach (Pairing p in pairings)
                    {
                        Console.WriteLine($"    {p.Name.PadRight(max)} {p.Path}");
                    }
                }
            }
            else if (arg[0] == "test")
            {
                if (arg.Length < 2)
                {
                    LogError("错误的参数");
                    continue;
                }
                string res = null;
                if (arg.Length == 2)
                {
                    try
                    {
                        string local = await PingUtils.Get(arg[1]);
                        if (local == null)
                        {
                            LogError("生成错误");
                        }
                        else
                        {
                            LogOut("生成成功");
                        }
                    }
                    catch (Exception e)
                    {
                        LogError("生成错误");
                        LogError(e);
                    }
                }
                else if (arg.Length == 3)
                {
                    res = await PingUtils.Get(arg[1], arg[2]);
                }
                else
                {
                    LogError("错误的参数");
                }
            }
        }
    }

    public static void LogError(Exception e)
        => logs.LogError(e);
    public static void LogError(string a)
        => logs.LogError(a);
    public static void LogOut(string a)
        => logs.LogOut(a);
    public static void SendMessageGroup(long group, string message)
    {
        RobotCore.SendGroupMessage(group, [message]);
    }

    public static void SendMessageGroupImg(long group, string local)
    {
        RobotCore.SendGroupMessage(group, [$"[CQ:image,file=file://{local}]"]);
    }
}
