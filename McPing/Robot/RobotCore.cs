using Newtonsoft.Json.Linq;
using OneBotSharp;
using OneBotSharp.Objs.Api;
using OneBotSharp.Objs.Event;
using OneBotSharp.Objs.Message;
using OneBotSharp.Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace McPing.Robot;

public static class RobotCore
{
    public static IOneBot<ISendRecvPipe> Robot;
    private static Thread thread;
    private static bool run;
    private static bool send;
    private static bool restart;
    private static ConcurrentQueue<Action> list = [];

    public static void Start()
    {
        Robot = Bot.MakePipe(Program.Config.Robot.Url,
            Program.Config.Robot.Authorization);
        Robot.Pipe.EventRecv += Robot_EventRecv;
        Robot.Pipe.StateChange += Pipe_StateChange;
        thread = new Thread(Run);
        run = true;
        thread.Start();
        Connect();
    }

    private static void Run()
    {
        while (run)
        {
            if (send)
            {
                while (list.TryDequeue(out var runa))
                {
                    runa();
                }
                Thread.Sleep(20);
            }
            else if (restart)
            {
                restart = false;
                Connect();
                Thread.Sleep(5000);
            }
        }
    }

    private static async void Pipe_StateChange(ISendRecvPipe arg1, ISendRecvPipe.PipeState arg2)
    {
        if (arg2 is ISendRecvPipe.PipeState.ConnectFail
            or ISendRecvPipe.PipeState.Disconnected)
        {
            send = false;
            if (run == false)
            {
                return;
            }
            await Robot.Close();
            restart = true;
        }
        else if (arg2 == ISendRecvPipe.PipeState.Connected)
        {
            send = true;
        }
    }

    private static void Connect()
    {
        try
        {
            Robot.Start();
        }
        catch (Exception e)
        {
            if (run == false)
            {
                return;
            }
            Program.LogError(e);
            restart = true;
        }
    }

    private static void Robot_EventRecv(ISendRecvPipe pipe, EventBase obj)
    {
        if (obj is EventGroupMessage message)
        {
            Program.Message(message);
        }
    }

    public static void Stop()
    {
        run = false;
        Robot.Close();
        Robot.Dispose();
    }

    internal static void SendPrivateMessage(long sendQQ, List<MsgBase> value)
    {
        list.Enqueue(() =>
        {
            Robot.Pipe.SendPrivateMsg(SendPrivateMsg.Build(sendQQ, value));
        });
    }

    internal static void SendGroupMessage(long sendQQ, List<MsgBase> value)
    {
        list.Enqueue(() =>
        {
            Robot.Pipe.SendGroupMsg(SendGroupMsg.Build(sendQQ, value));
        });
    }
}