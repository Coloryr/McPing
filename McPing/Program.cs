using ColoryrSDK;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace McPing
{
    class Program
    {
        public const string Version = "1.1.0";
        public static string RunLocal { get; private set; }
        public static ConfigObj Config { get; private set; }

        private static Logs logs;
        private static bool have;

        private static Robot robot = new();
        private static void Message(byte type, string data)
        {
            switch (type)
            {
                case 49:
                    var pack = JsonConvert.DeserializeObject<GroupMessageEventPack>(data);
                    if (Config.Group.Contains(pack.id))
                    {
                        var message = pack.message[^1].Split(' ');
                        if (message[0] == Config.Head)
                        {
                            if (have)
                            {
                                Task.Run(async () =>
                                {
                                    CancellationTokenSource cancel = new();
                                    var task = Task.Run(() =>
                                    {
                                        string local = PingUtils.Get(Config.DefaultIP, cancel.Token);
                                        if (local == null)
                                        {
                                            SendMessageGroup(pack.id, $"获取{Config.DefaultIP}错误");
                                        }
                                        else
                                        {
                                            SendMessageGroupImg(pack.id, local);
                                        }
                                    }, cancel.Token);
                                    int timeout = 8000;
                                    if (await Task.WhenAny(task, Task.Delay(timeout)) != task)
                                    {
                                        cancel.Cancel(false);
                                        SendMessageGroup(pack.id, $"获取{Config.DefaultIP}超时");
                                    }

                                });
                                break;
                            }
                            if (message.Length == 1)
                            {

                                SendMessageGroup(pack.id, $"输入{Config.Head} [IP] [端口](可选) 来生成服务器Motd图片");
                                break;
                            }
                            var ip = message[1];
                            if (message.Length > 2)
                            {
                                var port = message[2];
                                Task.Run(async () =>
                                {
                                    CancellationTokenSource cancel = new();
                                    var task = Task.Run(() =>
                                   {
                                       string local = PingUtils.Get(ip, port, cancel.Token);
                                       if (local == null)
                                       {
                                           SendMessageGroup(pack.id, $"获取{ip}:{port}错误");
                                       }
                                       else
                                       {
                                           SendMessageGroupImg(pack.id, local);
                                       }
                                   }, cancel.Token);
                                    int timeout = 8000;
                                    if (await Task.WhenAny(task, Task.Delay(timeout)) != task)
                                    {
                                        cancel.Cancel(false);
                                        SendMessageGroup(pack.id, $"获取{ip}:{port}超时");
                                    }

                                });
                            }
                            else
                            {
                                Task.Run(async () =>
                                {
                                    CancellationTokenSource cancel = new();
                                    var task = Task.Run(() =>
                                   {
                                       string local = PingUtils.Get(ip, cancel.Token);
                                       if (local == null)
                                       {
                                           SendMessageGroup(pack.id, $"获取{ip}错误");
                                       }
                                       else
                                       {
                                           SendMessageGroupImg(pack.id, local);
                                       }
                                   }, cancel.Token);
                                    int timeout = 8000;
                                    if (await Task.WhenAny(task, Task.Delay(timeout)) != task)
                                    {
                                        cancel.Cancel(false);
                                        SendMessageGroup(pack.id, $"获取{ip}超时");
                                    }
                                });
                            }
                        }
                    }
                    break;
            }
        }

        private static void Log(LogType type, string data)
            => LogOut($"机器人状态:{type} {data}");

        private static void State(StateType type)
            => LogOut($"机器人状态:{type}");

        static void Main(string[] args)
        {
            RunLocal = AppContext.BaseDirectory;
            logs = new Logs(RunLocal);
            Config = ConfigUtils.Config(new ConfigObj()
            {
                Robot = new RobotObj()
                {
                    IP = "127.0.0.1",
                    Port = 23333,
                    Check = false,
                    Time = 10000
                },
                Group = new(),
                RunQQ = 0,
                Show = new ShowObj()
                {
                    Font = "微软雅黑",
                    BGColor = "#1C1C1C",
                    GoodPingColor = "#7CFC00",
                    BadPingColor = "#FF4500",
                    PlayerColor = "#A020F0",
                    VersionColor = "#8B795E"
                },
                Head = "#mc",
                DefaultIP = ""
            }, RunLocal + "config.json");

            have = !string.IsNullOrWhiteSpace(Config.DefaultIP);

            if (!GenShow.Init())
            {
                LogError("初始化错误");
                return;
            }

            RobotConfig config = new()
            {
                IP = Config.Robot.IP,
                Port = Config.Robot.Port,
                Check = Config.Robot.Check,
                Name = "McPing",
                Pack = new() { 49 },
                RunQQ = Config.RunQQ,
                Time = Config.Robot.Time,
                CallAction = Message,
                LogAction = Log,
                StateAction = State
            };

            LogOut("正在连接ColorMirai");

            robot.Set(config);
            robot.Start();

            while (true)
            {
                string comm = Console.ReadLine();
                var arg = comm.Split(' ');
                if (arg[0] == "stop")
                {
                    LogOut("正在退出");
                    robot.Stop();
                    return;
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
                        Task.Run(async () =>
                        {
                            CancellationTokenSource cancel = new();
                            var task = Task.Run( () =>
                            {
                                string local = PingUtils.Get(arg[1], cancel.Token);
                                if (local == null)
                                {
                                    LogError("生成错误");
                                }
                                else
                                {
                                    LogOut("生成成功");
                                }
                            }, cancel.Token);
                            int timeout = 8000;
                            if (await Task.WhenAny(task, Task.Delay(timeout)) != task)
                            {
                                cancel.Cancel(false);
                                LogError("生成超时");
                            }
                        });
                    }
                    else if (arg.Length == 3)
                    {
                        res = PingUtils.Get(arg[1], arg[2], CancellationToken.None);
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
            var temp = BuildPack.Build(new SendGroupMessagePack
            {
                id = group,
                message = new()
                {
                    message
                }
            }, 52);
            robot.AddTask(temp);
        }

        public static void SendMessageGroupImg(long group, string local)
        {
            var temp = BuildPack.Build(new LoadFileSendToGroupImagePack
            {
                id = group,
                file = local
            }, 75);
            robot.AddTask(temp);
        }
    }
}
