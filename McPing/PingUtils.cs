using Heijden.Dns.Portable;
using Heijden.DNS;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace McPing
{
    class PingUtils
    {
        public static string Get(string IP, CancellationToken cancellationToken)
        {
            if (IP.Contains(":"))
            {
                var temp = IP.Split(':');
                if (!ushort.TryParse(temp[1], out var port))
                    return null;
                return Get(temp[0], port, cancellationToken).Result;
            }
            return Get(IP, 25565, cancellationToken).Result;
        }
        public static string Get(string IP,string Port, CancellationToken cancellationToken)
        {
            if (!ushort.TryParse(Port, out var port))
                return null;
            return Get(IP, port, cancellationToken).Result;
        }
        private static async Task<string> Get(string IP, ushort Port, CancellationToken cancellationToken)
        {
            string origin;
            TcpClient tcp;
            try
            {
                tcp = new TcpClient();
                await tcp.ConnectAsync(IP, Port, cancellationToken);
                origin = $"{IP}_{Port}";
            }
            catch (SocketException)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }
                origin = IP;
                var res = new Resolver().Query("_minecraft._tcp." + IP, QType.SRV);
                res.Wait(cancellationToken);
                if (res.Result?.Answers?.FirstOrDefault()?.RECORD is RecordSRV result)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return null;
                    }
                    tcp = new TcpClient();
                    await tcp.ConnectAsync(IP = result.TARGET[..^1], Port = result.PORT, cancellationToken);
                }
                else
                {
                    return null;
                }
            }
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }
            ServerInfo info = new();
            if (info.StartGetServerInfo(tcp, IP, Port, origin))
            {
                return GenShow.Gen(info);
            }
            return null;
        }
    }
    class ServerInfo
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
                                    string text;
                                    string color;
                                    if (item["extra"] is JArray array)
                                        foreach (var item2 in array)
                                        {
                                            text = item2["text"].ToString();
                                            color = item2["color"]?.ToString();
                                            color = GetColor(color);
                                            MOTD += color + text;
                                        }
                                    text = item["text"].ToString();
                                    if (text.Length != 0)
                                    {
                                        color = item["color"]?.ToString();
                                        color = GetColor(color);
                                        MOTD += color + text;
                                    }
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
                    return "§f";
            }
        }
    }
}
