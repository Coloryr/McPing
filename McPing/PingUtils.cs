using Heijden.Dns.Portable;
using Heijden.DNS;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace McPing
{
    class PingUtils
    {
        public static Task<string> Get(string IP)
        {
            if (IP.Contains(':'))
            {
                var temp = IP.LastIndexOf(':') + 1;
                if (!ushort.TryParse(IP[temp..], out var port))
                    return null;
                return Get(IP[0..(temp - 1)], port);
            }
            return Get(IP, 25565);
        }
        public static Task<string> Get(string IP, string Port)
        {
            if (!ushort.TryParse(Port, out var port))
                return null;
            return Get(IP, port);
        }
        private static async Task<string> Get(string IP, ushort Port)
        {
            try
            {
                string origin;
                TcpClient tcp;
                try
                {
                    tcp = new TcpClient()
                    {
                        ReceiveTimeout = 5000
                    };
                    await tcp.ConnectAsync(IP, Port);
                    origin = $"{IP}_{Port}";
                }
                catch (SocketException)
                {
                    origin = IP;
                    var resolver = new Resolver()
                    {
                        Timeout = TimeSpan.FromSeconds(5)
                    };
                    var res = await resolver.Query("_minecraft._tcp." + IP, QType.SRV);
                    if (res?.Answers?.FirstOrDefault()?.RECORD is RecordSRV result)
                    {
                        tcp = new TcpClient();
                        await tcp.ConnectAsync(IP = result.TARGET[..^1], Port = result.PORT);
                    }
                    else
                    {
                        return null;
                    }
                }
                PCServerInfo info = new();
                if (info.StartGetServerInfo(tcp, IP, Port, origin))
                {
                    return GenShow.Gen(info);
                }
            }
            catch
            {

            }

            try
            {
                if (Port == 25565)
                {
                    Port = 19132;
                }
                var info = new PEServerInfo(IP, Port);
                if (info.MotdPe())
                {
                    return GenShow.Gen(info);
                }
            }
            catch
            {

            }

            return null;
        }
    }

    interface IServerInfo
    {
        string IP { get; }
        byte[] IconData { get; }
        string MOTD { get; }
        string GameVersion { get; }
        int CurrentPlayerCount { get; }
        int MaxPlayerCount { get; }
        long Ping { get; }
    }

    class PEServerInfo : IServerInfo
    {
        public string IP { get; private set; }
        public int Port { get; private set; }
        public byte[] IconData { get; private set; }
        public string MOTD { get; private set; }
        public string GameVersion { get; private set; }
        public int CurrentPlayerCount { get; private set; }
        public int MaxPlayerCount { get; private set; }
        public long Ping { get; private set; }

        private static readonly byte[] msg = new byte[] { 0x00, 0xFF, 0xFF, 0x00, 0xFE, 0xFE, 0xFE, 0xFE, 0xFD, 0xFD, 0xFD, 0xFD, 0x12, 0x34, 0x56, 0x78, };

        public PEServerInfo(string ip, int port)
        {
            IP = ip;
            Port = port;
            IconData = null;
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
                socket.Connect(IP, Port);
                socket.Send(list.ToArray());
                int length = socket.Receive(buffer);
                pingWatcher.Stop();

                var res = Encoding.UTF8.GetString(buffer, 0, length).Split(";");

                int.TryParse(res[4], out int a);
                CurrentPlayerCount = a;
                int.TryParse(res[5], out a);
                MaxPlayerCount = a;

                GameVersion += $"{res[3]} {res[8]}";
                MOTD = res[1];
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

    class PCServerInfo : IServerInfo
    {
        public string IP { get; private set; }
        /// <summary>
        /// 获取服务器MOTD
        /// </summary>
        public string MOTD { get; private set; }

        /// <summary>
        /// 获取服务器的最大玩家数量
        /// </summary>
        public int MaxPlayerCount { get; private set; }

        /// <summary>
        /// 获取服务器的在线人数
        /// </summary>
        public int CurrentPlayerCount { get; private set; }

        /// <summary>
        /// 获取服务器版本号
        /// </summary>
        public int ProtocolVersion { get; private set; }

        /// <summary>
        /// 获取服务器游戏版本
        /// </summary>
        public string GameVersion { get; private set; }

        /// <summary>
        /// 获取服务器详细的服务器信息JsonResult
        /// </summary>
        public string JsonResult { get; private set; }

        /// <summary>
        /// 获取服务器Forge信息（如果可用）
        /// </summary>
        public ForgeInfo ForgeInfo { get; private set; }

        /// <summary>
        /// 获取服务器在线玩家的名称（如果可用）
        /// </summary>
        public List<string> OnlinePlayersName { get; private set; }

        /// <summary>
        /// 获取此次连接服务器的延迟(ms)
        /// </summary>
        public long Ping { get; private set; }

        /// <summary>
        /// Icon DATA
        /// </summary>
        public byte[] IconData { get; set; }

        /// <summary>
        /// 获取与特定格式代码相关联的颜色代码
        /// </summary>

        public bool StartGetServerInfo(TcpClient tcp, string IP, ushort Port, string orgin)
        {
            try
            {
                this.IP = orgin;
                tcp.ReceiveBufferSize = 1024 * 1024;

                byte[] packet_id = ProtocolHandler.getVarInt(0);
                byte[] protocol_version = ProtocolHandler.getVarInt(754);
                byte[] server_adress_val = Encoding.UTF8.GetBytes(IP);
                byte[] server_adress_len = ProtocolHandler.getVarInt(server_adress_val.Length);
                byte[] server_port = BitConverter.GetBytes(Port); 
                Array.Reverse(server_port);
                byte[] next_state = ProtocolHandler.getVarInt(1);
                byte[] packet2 = ProtocolHandler.concatBytes(packet_id, protocol_version, server_adress_len, server_adress_val, server_port, next_state);
                byte[] tosend = ProtocolHandler.concatBytes(ProtocolHandler.getVarInt(packet2.Length), packet2);

                byte[] status_request = ProtocolHandler.getVarInt(0);
                byte[] request_packet = ProtocolHandler.concatBytes(ProtocolHandler.getVarInt(status_request.Length), status_request);

                tcp.Client.Send(tosend, SocketFlags.None);

                tcp.Client.Send(request_packet, SocketFlags.None);
                ProtocolHandler handler = new(tcp);
                int packetLength = handler.readNextVarIntRAW();
                if (packetLength > 0)
                {
                    List<byte> packetData = new(handler.readDataRAW(packetLength));
                    if (ProtocolHandler.readNextVarInt(packetData) == 0x00) //Read Packet ID
                    {
                        string result = ProtocolHandler.readNextString(packetData); //Get the Json data
                        this.JsonResult = result;
                        SetInfoFromJsonText(result);
                    }
                }

                byte[] ping_id = ProtocolHandler.getVarInt(1);
                byte[] ping_content = BitConverter.GetBytes((long)233);
                byte[] ping_packet = ProtocolHandler.concatBytes(ping_id, ping_content);
                byte[] ping_tosend = ProtocolHandler.concatBytes(ProtocolHandler.getVarInt(ping_packet.Length), ping_packet);

                try
                {
                    tcp.ReceiveTimeout = 1000;

                    Stopwatch pingWatcher = new();

                    pingWatcher.Start();
                    tcp.Client.Send(ping_tosend, SocketFlags.None);

                    int pingLenghth = handler.readNextVarIntRAW();
                    pingWatcher.Stop();
                    if (pingLenghth > 0)
                    {
                        List<byte> packetData = new(handler.readDataRAW(pingLenghth));
                        if (ProtocolHandler.readNextVarInt(packetData) == 0x01) //Read Packet ID
                        {
                            long content = ProtocolHandler.readNextByte(packetData); //Get the Json data
                            if (content == 233)
                            {
                                Ping = pingWatcher.ElapsedMilliseconds;
                            }
                        }
                    }
                    return true;
                }
                catch (Exception e)
                {
                    Ping = 999;
                    return true;
                }
            }
            catch (Exception e)
            {
                Program.LogError(e);
            }
            finally
            {
                tcp.Close();
            }
            return false;
        }

        private string Get(JToken obj)
        {
            string temp = "";
            string text;
            string color;
            JObject obj1 = obj as JObject;
            if (obj1?.ContainsKey("strikethrough") == true)
            {
                var strikethrough = (bool)obj1["strikethrough"];
                temp += strikethrough ? GetColor("strikethrough") : "";
            }

            if (obj1?.ContainsKey("underlined") == true)
            {
                var underlined = (bool)obj1["underlined"];
                temp += underlined ? GetColor("underline") : "";
            }

            if (obj1?.ContainsKey("italic") == true)
            {
                var italic = (bool)obj1["italic"];
                temp += italic ? GetColor("italic") : "";
            }
            if (obj["extra"] is JArray array)
                foreach (var item2 in array)
                {
                    text = item2["text"].ToString();

                    color = item2["color"]?.ToString();
                    color = GetColor(color);
                    temp += color + text;
                    if (item2["extra"] != null)
                    {
                        temp += Get(item2);
                    }
                }
            text = obj["text"].ToString();
            if (text.Length != 0)
            {
                color = obj["color"]?.ToString();
                color = GetColor(color);
                temp += color + text;
            }
            return temp;
        }

        private void SetInfoFromJsonText(string JsonText)
        {
            try
            {
                if (!string.IsNullOrEmpty(JsonText) && JsonText.StartsWith("{") && JsonText.EndsWith("}"))
                {
                    JObject jsonData = JObject.Parse(JsonText);

                    if (jsonData.ContainsKey("version"))
                    {
                        JObject versionData = jsonData["version"] as JObject;
                        GameVersion = versionData["name"].ToString();
                        ProtocolVersion = int.Parse(versionData["protocol"].ToString());
                    }

                    if (jsonData.ContainsKey("players"))
                    {
                        JObject playerData = jsonData["players"] as JObject;
                        this.MaxPlayerCount = int.Parse(playerData["max"].ToString());
                        this.CurrentPlayerCount = int.Parse(playerData["online"].ToString());
                        if (playerData.ContainsKey("sample"))
                        {
                            this.OnlinePlayersName = new List<string>();
                            foreach (JObject name in playerData["sample"])
                            {
                                if (name.ContainsKey("name"))
                                {
                                    string playername = name["name"].ToString();
                                    this.OnlinePlayersName.Add(playername);
                                }
                            }
                        }
                    }

                    if (jsonData.ContainsKey("description"))
                    {
                        JToken descriptionData = jsonData["description"];
                        if (descriptionData.Type == JTokenType.String)
                        {
                            MOTD = descriptionData.ToString();
                        }
                        else if (descriptionData.Type == JTokenType.Object)
                        {
                            JObject descriptionDataObj = descriptionData as JObject;
                            if (descriptionDataObj.ContainsKey("text"))
                            {
                                MOTD += descriptionDataObj["text"].ToString();
                            }
                            if (descriptionDataObj.ContainsKey("extra"))
                            {
                                foreach (JObject item in descriptionDataObj["extra"])
                                {
                                    MOTD += Get(item);
                                }
                            }

                            if (descriptionDataObj.ContainsKey("translate"))
                            {
                                MOTD += descriptionDataObj["translate"].ToString();
                            }
                        }
                    }

                    // Check for forge on the server.
                    if (jsonData.ContainsKey("modinfo") && jsonData["modinfo"].Type == JTokenType.Object)
                    {
                        JObject modData = jsonData["modinfo"] as JObject;
                        if (modData.ContainsKey("type") && modData["type"].ToString() == "FML")
                        {
                            ForgeInfo = new ForgeInfo(modData);
                            if (!ForgeInfo.Mods.Any())
                            {
                                ForgeInfo = null;
                            }
                        }
                    }

                    if (jsonData.ContainsKey("favicon"))
                    {
                        try
                        {
                            string datastring = jsonData["favicon"].ToString();
                            byte[] arr = Convert.FromBase64String(datastring.Replace("data:image/png;base64,", ""));
                            IconData = arr;
                        }
                        catch
                        {
                            IconData = null;
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private string GetColor(string color)
        {
            switch (color)
            {
                case "black":
                    return "§0";
                case "dark_blue":
                    return "§1";
                case "dark_green":
                    return "§2";
                case "dark_aqua":
                    return "§3";
                case "dark_red":
                    return "§4";
                case "dark_purple":
                    return "§5";
                case "gold":
                    return "§6";
                case "gray":
                    return "§7";
                case "dark_gray":
                    return "§8";
                case "blue":
                    return "§9";
                case "green":
                    return "§a";
                case "aqua":
                    return "§b";
                case "red":
                    return "§c";
                case "light_purple":
                    return "§d";
                case "yellow":
                    return "§e";
                case "white":
                    return "§f";
                case "obfuscated":
                    return "§k";
                case "bold":
                    return "§l";
                case "strikethrough":
                    return "§m";
                case "underline":
                    return "§n";
                case "italic":
                    return "§o";
                case "reset":
                    return "§r";
                default:
                    if (color?.StartsWith("#") == true)
                    {
                        return "§" + color;
                    }
                    return "";
            }
        }
    }
}
