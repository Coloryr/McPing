using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace McPing.PingTools;

public class PCServerInfo : IServerInfo
{
    /// <summary>
    /// 获取服务器MOTD
    /// </summary>
    public ServerMotdObj ServerMotd { get; private set; }

    /// <summary>
    /// Icon DATA
    /// </summary>
    public byte[] IconData { get; set; }

    /// <summary>
    /// 获取与特定格式代码相关联的颜色代码
    /// </summary>

    public bool StartGetServerInfo(TcpClient tcp, string ip, ushort port)
    {
        try
        {
            ServerMotd = new ServerMotdObj(ip, port);
            tcp.ReceiveBufferSize = 1024 * 1024;

            byte[] packet_id = ProtocolHandler.GetVarInt(0);
            byte[] protocol_version = ProtocolHandler.GetVarInt(754);
            byte[] server_adress_val = Encoding.UTF8.GetBytes(ip);
            byte[] server_adress_len = ProtocolHandler.GetVarInt(server_adress_val.Length);
            byte[] server_port = BitConverter.GetBytes(port);
            Array.Reverse(server_port);
            byte[] next_state = ProtocolHandler.GetVarInt(1);
            byte[] packet2 = ProtocolHandler.ConcatBytes(packet_id, protocol_version, server_adress_len, server_adress_val, server_port, next_state);
            byte[] tosend = ProtocolHandler.ConcatBytes(ProtocolHandler.GetVarInt(packet2.Length), packet2);

            byte[] status_request = ProtocolHandler.GetVarInt(0);
            byte[] request_packet = ProtocolHandler.ConcatBytes(ProtocolHandler.GetVarInt(status_request.Length), status_request);

            tcp.Client.Send(tosend, SocketFlags.None);
            tcp.Client.Send(request_packet, SocketFlags.None);
           
            ProtocolHandler handler = new(tcp);
           
            int packetLength = handler.ReadNextVarIntRAW();
            
            if (packetLength > 0)
            {
                List<byte> packetData = new(handler.ReadDataRAW(packetLength));
                if (ProtocolHandler.ReadNextVarInt(packetData) == 0x00) //Read Packet ID
                {
                    string result = ProtocolHandler.ReadNextString(packetData); //Get the Json data
                    JsonConvert.PopulateObject(result, ServerMotd);

                    if (!string.IsNullOrEmpty(ServerMotd.Description.Text)
                        && ServerMotd.Description.Extra == null && ServerMotd.Description.Text.Contains('§'))
                    {
                        ServerMotd.Description = ServerDescriptionJsonConverter.StringToChar(ServerMotd.Description.Text);
                    }
                }
            }

            return true;
        }
        catch (Exception e)
        {
            Program.LogError(e);
        }
        return false;
    }

    public static string CleanFormat(string str)
    {
        str = str.Replace(@"\n", "\n");

        int index;
        do
        {
            index = str.IndexOf('§');
            if (index >= 0)
            {
                str = str.Remove(index, 2);
            }
        } while (index >= 0);

        return str.Trim();
    }
}
