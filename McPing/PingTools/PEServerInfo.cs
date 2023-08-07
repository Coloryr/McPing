using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace McPing.PingTools;

public class PEServerInfo : IServerInfo
{
    public byte[] IconData { get; private set; }
    public string str { get; private set; }
    public string GameVersion { get; private set; }
    public int CurrentPlayerCount { get; private set; }
    public int MaxPlayerCount { get; private set; }
    public long Ping { get; private set; }

    public ServerMotdObj MOTD  { get; private set; }

    private static readonly byte[] msg = new byte[] { 0x00, 0xFF, 0xFF, 0x00, 0xFE, 0xFE, 0xFE, 0xFE, 0xFD, 0xFD, 0xFD, 0xFD, 0x12, 0x34, 0x56, 0x78, };

    public PEServerInfo(string ip, int port)
    {
        MOTD = new(ip, port);
    }

    public bool MotdPe()
    {
        try
        {
            byte[] buffer = new byte[1024 * 1024 * 2];
            Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            var time = Encoding.UTF8.GetBytes(Convert.ToInt32((DateTime.Now - DateTime.Parse("1970-1-1")).TotalSeconds).ToString(), 0, 8).ToList();
            time.Reverse();
            var list = new List<byte>
            {
                0x01
            };
            list.AddRange(time);
            list.AddRange(msg);

            Stopwatch pingWatcher = new();
            pingWatcher.Start();
            socket.ReceiveTimeout = 5000;
            socket.SendTimeout = 5000;
            socket.Connect(MOTD.ServerAddress, MOTD.ServerPort);
            socket.Send(list.ToArray());
            int length = socket.Receive(buffer);
            pingWatcher.Stop();

            var res = Encoding.UTF8.GetString(buffer, 0, length).Split(";");

            MOTD.Players = new();
            int.TryParse(res[4], out int a);
            MOTD.Players.Online = a;
            int.TryParse(res[5], out a);
            MOTD.Players.Max = a;

            MOTD.Version = new()
            {
                Name = $"{res[3]} {res[8]}"
            };

            MOTD.Description = ServerDescriptionJsonConverter.StringToChar(res[1]);
            //("motd", res[1]);
            //("protocolVersion", res[2]);
            //("version", res[3]);
            //("playerCount", res[4]);
            //("maximumPlayerCount", res[5]);
            //("subMotd", res[7]);
            //("gameType", res[8]);
            //("nintendoLimited", res[9]);
            //("ipv4Port", res[10]);
            //("ipv6Port", res[11]);
            //("rawText", res[12]);
            Ping = pingWatcher.ElapsedMilliseconds;
            return true;
        }
        catch
        {

        }

        return false;
    }
}
