using Heijden.Dns.Portable;
using Heijden.DNS;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace McPing.PingTools;

class PingUtils
{
    public static Task<string> Get(string IP)
    {
        if (IP.Contains(':'))
        {
            var temp = IP.LastIndexOf(':') + 1;
            if (!ushort.TryParse(IP[temp..], out var port))
            {
                return null;
            }
            return Task.Run(() =>
            {
                return Get(IP[0..(temp - 1)], port);
            });
        }
        return Task.Run(() => 
        {
            return Get(IP, 25565);
        });
    }
    public static Task<string> Get(string IP, string Port)
    {
        if (!ushort.TryParse(Port, out var port))
        {
            return null;
        }
        return Task.Run(() =>
        {
            return Get(IP, port);
        });
    }
    private static async Task<string> Get(string IP, ushort Port)
    {
        var tcp = new TcpClient()
        {
            ReceiveTimeout = 5000,
            SendTimeout = 5000
        };
        try
        {
            try
            {
                tcp.Connect(IP, Port);
            }
            catch (SocketException)
            {
                tcp.Close();
                tcp.Dispose();
                var resolver = new Resolver()
                {
                    Timeout = TimeSpan.FromSeconds(5)
                };
                var res = await resolver.Query("_minecraft._tcp." + IP, QType.SRV);
                if (res?.Answers?.FirstOrDefault()?.RECORD is RecordSRV result)
                {
                    tcp = new TcpClient()
                    {
                        ReceiveTimeout = 5000,
                        SendTimeout = 5000
                    };
                    try
                    {
                        tcp.Connect(IP = result.TARGET[..^1], Port = result.PORT);
                    }
                    catch
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            PCServerInfo info = new();
            if (info.StartGetServerInfo(tcp, IP, Port))
            {
                return GenShow.Gen(info);
            }
        }
        finally
        {
            tcp.Close();
            tcp.Dispose();
        }

        try
        {
            if (Port == 25565)
            {
                Port = 19132;
            }
            var info1 = new PEServerInfo(IP, Port);
            if (info1.MotdPe())
            {
                return GenShow.Gen(info1);
            }
        }
        catch
        {

        }

        return null;
    }
}
