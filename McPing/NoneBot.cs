using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace McPing;

public static class NoneBot
{
    public class ClientHandler(Action onConnectionLost) : SimpleChannelInboundHandler<IByteBuffer>
    {
        public override bool IsSharable => true;

        protected override void ChannelRead0(IChannelHandlerContext ctx, IByteBuffer msg)
        {
            var len = msg.ReadInt();
            var bytes = new byte[len];
            msg.ReadBytes(bytes);
            string response = Encoding.UTF8.GetString(bytes);
            Program.Message(response);
        }

        public override async void ChannelActive(IChannelHandlerContext ctx)
        {
            //// 连接建立后发送消息
            //byte[] bytes = Encoding.UTF8.GetBytes("Hello from C#");
            //var buffer =  Unpooled.Buffer();
            //buffer.WriteInt(bytes.Length).WriteBytes(bytes);
            //await ctx.WriteAndFlushAsync(buffer);
        }

        public override void ChannelInactive(IChannelHandlerContext ctx)
        {
            Console.WriteLine("Connection lost!");
            onConnectionLost?.Invoke(); // 触发重连逻辑
            base.ChannelInactive(ctx);
        }
    }

    private static Bootstrap _bootstrap;
    private static IEventLoopGroup _eventLoopGroup;
    private static int _reconnectAttempts = 0; // 当前重连次数
    private const int MaxReconnectAttempts = 5; // 最大重连次数
    private static IChannel channel;
    private const int BaseReconnectDelay = 1000; // 基础重连延迟(ms)

    public static async Task StartAsync(string host, int port)
    {
        _eventLoopGroup = new MultithreadEventLoopGroup();
        _bootstrap = new Bootstrap()
            .Group(_eventLoopGroup)
            .Channel<TcpSocketChannel>()
            .Option(ChannelOption.TcpNodelay, true)
            .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
            {
                channel.Pipeline.AddLast(new ClientHandler(OnConnectionLost));
            }));

        await ConnectWithRetryAsync(host, port);
    }

    // 核心重连方法（含指数退避）
    private static async Task ConnectWithRetryAsync(string host, int port)
    {
        try
        {
            channel = await _bootstrap.ConnectAsync(IPAddress.Parse(host), port);
            _reconnectAttempts = 0; // 连接成功时重置计数
            Console.WriteLine($"Connected to {host}:{port}");
        }
        catch (Exception ex)
        {
            if (_reconnectAttempts < MaxReconnectAttempts)
            {
                int delay = BaseReconnectDelay * (int)Math.Pow(2, _reconnectAttempts); // 指数退避
                Console.WriteLine($"Reconnect attempt {_reconnectAttempts + 1} in {delay}ms. Error: {ex.Message}");

                await Task.Delay(delay);
                _reconnectAttempts++;
                await ConnectWithRetryAsync(host, port); // 递归重试
            }
            else
            {
                Console.WriteLine("Max reconnect attempts reached. Aborting.");
                await ShutdownAsync();
            }
        }
    }

    public static async void Send(string data)
    {
        if (channel != null && channel.IsWritable)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            var buffer = Unpooled.Buffer();
            buffer.WriteInt(bytes.Length).WriteBytes(bytes);
            await channel.WriteAndFlushAsync(buffer);
        }
    }

    // 连接断开回调（由Handler触发）
    private static void OnConnectionLost()
    {
        var config = Program.Config.Robot;
        Task.Run(() => ConnectWithRetryAsync(config.Url, config.Port));
    }

    // 资源清理
    public static async Task ShutdownAsync()
    {
        await _eventLoopGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
    }

    public static async void Init()
    {
        var config = Program.Config.Robot;
        await StartAsync(config.Url, config.Port);
    }
}
