using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text;

namespace McPing.Robot;

public static class RobotCore
{
    public static OneBotSDK Robot;

    public static void Message(object data)
    {
        if (data is GroupMessagePack pack)
        {
            Program.Message(pack);
        }
    }

    public static void Start()
    {
        Robot = new OneBotSDK(Program.Config.Robot.Url,
            Program.Config.Robot.Authorization);
        Robot.Start();
    }

    public static void SendPrivateMessage(long to, List<string> list)
    {
        var msg = new StringBuilder();
        foreach (var item in list)
        {
            msg.Append(item);
        }
        var obj = new JObject()
        {
            { "action", "send_private_msg" },
            { "params", new JObject()
            {
                { "user_id", to },
                { "message", msg.ToString() }
            }
            }
        };

        Robot.Send(obj.ToString());
    }

    public static void SendGroupMessage(long group, List<string> list)
    {
        var msg = new StringBuilder();
        foreach (var item in list)
        {
            msg.Append(item);
        }
        var obj = new JObject()
        {
            { "action", "send_group_msg" },
            { "params", new JObject()
            {
                { "group_id", group },
                { "message", msg.ToString() }
            }
            }
        };

        Robot.Send(obj.ToString());
    }

    public static void Stop()
    {
        Robot.Stop();
    }
}