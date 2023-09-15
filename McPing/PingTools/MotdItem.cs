using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McPing.PingTools;

public enum StateType
{
    GOOD,
    NO_RESPONSE,
    BAD_CONNECT,
    EXCEPTION
}

public class ServerDescriptionJsonConverter : JsonConverter<Chat>
{
    public static Chat StringToChar(string str1)
    {
        if (string.IsNullOrWhiteSpace(str1))
            return new Chat() { Text = "" };

        var lines = str1.Split("\n");
        var chat = new Chat()
        {
            Extra = new()
        };

        foreach (var item in lines)
        {
            var chat1 = new Chat();
            bool mode = false;
            for (var a = 0; a < item.Length; a++)
            {
                var char1 = item[a];
                if (char1 == '§' && mode == false)
                {
                    if (!string.IsNullOrEmpty(chat1.Text))
                    {
                        chat.Extra.Add(chat1);
                    }
                    chat1 = new()
                    {
                        Bold = chat1.Bold,
                        Underlined = chat1.Underlined,
                        Obfuscated = chat1.Obfuscated,
                        Strikethrough = chat1.Strikethrough,
                        Italic = chat1.Italic,
                        Color = chat1.Color
                    };
                    mode = true;
                }
                else if (mode == true)
                {
                    mode = false;
                    if (ServerMotd.MinecraftColors.TryGetValue(char1, out var color))
                    {
                        chat1.Color = color;
                    }
                    else if (char1 == 'r' || char1 == 'R')
                    {
                        chat1.Underlined = false;
                        chat1.Obfuscated = false;
                        chat1.Strikethrough = false;
                        chat1.Italic = false;
                        chat1.Bold = false;
                        chat1.Color = "#FFFFFF";
                    }
                    else if (char1 == 'k' || char1 == 'K')
                    {
                        chat1.Obfuscated = true;
                    }
                    else if (char1 == 'l' || char1 == 'L')
                    {
                        chat1.Bold = true;
                    }
                    else if (char1 == 'm' || char1 == 'M')
                    {
                        chat1.Strikethrough = true;
                    }
                    else if (char1 == 'n' || char1 == 'N')
                    {
                        chat1.Underlined = true;
                    }
                    else if (char1 == 'o' || char1 == 'O')
                    {
                        chat1.Italic = true;
                    }
                }
                else
                {
                    chat1.Text += char1;
                }
            }

            chat.Extra.Add(chat1);

            if (lines.Length != 1)
            {
                chat.Extra.Add(new Chat()
                {
                    Text = "\n"
                });
            }
        }

        return chat;
    }

    public override Chat ReadJson(JsonReader reader, Type objectType, Chat existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.String)
        {
            var str1 = reader.Value?.ToString();
            return StringToChar(str1);
        }
        else
        {
            JObject obj = JObject.Load(reader);
            Chat chat = new();
            serializer.Populate(obj.CreateReader(), chat);
            return chat;
        }
    }

    public override void WriteJson(JsonWriter writer, Chat value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }
}

[JsonConverter(typeof(ServerDescriptionJsonConverter))]
public class Chat
{
    [JsonProperty("text")]
    public string Text { get; set; }

    [JsonProperty("bold")]
    public bool Bold { get; set; }

    [JsonProperty("italic")]
    public bool Italic { get; set; }

    [JsonProperty("underlined")]
    public bool Underlined { get; set; }

    [JsonProperty("strikethrough")]
    public bool Strikethrough { get; set; }

    [JsonProperty("obfuscated")]
    public bool Obfuscated { get; set; }

    [JsonProperty("color")]
    public string Color { get; set; }

    [JsonProperty("extra")]
    public List<Chat> Extra { get; set; }

    public override string ToString()
    {
        return ServerMotd.CleanFormat(this.ToPlainTextString());
    }
}

public record ServerVersionInfo
{
    public string Name { get; set; }

    public int Protocol { get; set; }
}

public record ServerPlayerInfo
{
    public int Max { get; set; }

    public int Online { get; set; }

    public List<Player> Sample { get; set; }
}

public record Player
{
    public string Name { get; set; }

    public string Id { get; set; }
}

public record ModInfo
{
    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("modList")]
    public List<Mod> ModList { get; set; }
}

public record Mod
{
    [JsonProperty("modid")]
    public string ModId { get; set; }

    [JsonProperty("version")]
    public string Version { get; set; }
}

public class ServerMotdObj
{
    /// <summary>
    /// Server's address, support srv.
    /// </summary>
    [JsonIgnore]
    public string ServerAddress { get; set; }

    /// <summary>
    /// Server's runing port
    /// </summary>
    [JsonIgnore]
    public int ServerPort { get; set; }

    /// <summary>
    /// The server's name, it's used to display server name in ui.
    /// </summary>
    [JsonIgnore]
    public string ServerName { get; set; }

    /// <summary>
    /// The server version info such name or protocol
    /// </summary>
    [JsonProperty("version")]
    public ServerVersionInfo Version { get; set; }

    /// <summary>
    /// The server player info such max or current player count and sample.
    /// </summary>
    [JsonProperty("players")]
    public ServerPlayerInfo Players { get; set; }

    /// <summary>
    /// Server's description (aka motd)
    /// </summary>
    [JsonProperty("description")]
    public Chat Description { get; set; }

    /// <summary>
    /// server's favicon. is a png image that is base64 encoded
    /// </summary>
    [JsonProperty("favicon")]
    public string Favicon { get; set; }

    /// <summary>
    /// Server's mod info including mod type and mod list (if is avaliable)
    /// </summary>
    [JsonProperty("modinfo")]
    public ModInfo ModInfo { get; set; }

    [JsonIgnore]
    public byte[] FaviconByteArray { get { return Convert.FromBase64String(Favicon.Replace("data:image/png;base64,", "")); } }

    /// <summary>
    /// The ping delay time.(ms)
    /// </summary>
    [JsonIgnore]
    public long Ping { get; set; }

    /// <summary>
    /// The handshake state
    /// </summary>
    [JsonIgnore]
    public StateType State { get; set; }

    /// <summary>
    /// The handshake message
    /// </summary>
    [JsonIgnore]
    public string Message { get; set; }

    [JsonIgnore]
    public bool AcceptTextures { get; set; }

    public ServerMotdObj(string ip, int port)
    {
        ServerAddress = ip;
        ServerPort = port;
        Favicon = "data:image/png;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7";
    }
}
