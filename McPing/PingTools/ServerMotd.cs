using Heijden.Dns.Portable;
using Heijden.DNS;
using McPing.PingTools;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace McPing.PingTools;

public static class ServerMotd
{
    /// <summary>
    /// 转字符串
    /// </summary>
    /// <param name="chat"></param>
    /// <returns></returns>
    public static string ToPlainTextString(this Chat chat)
    {
        StringBuilder stringBuilder = new(chat.Text);
        if (chat.Extra != null)
        {
            foreach (var item in chat.Extra)
            {
                stringBuilder.Append(item.ToPlainTextString());
            }
        }
        return stringBuilder.ToString();
    }

    /// <summary>
    /// 清除格式
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
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

    /// <summary>
    /// 获取与特定格式代码相关联的颜色代码
    /// </summary>
    public static Dictionary<char, string> MinecraftColors { get; private set; } =
        new Dictionary<char, string>()
    {
        { '0', "#000000" },
        { '1', "#0000AA" },
        { '2', "#00AA00" },
        { '3', "#00AAAA" },
        { '4', "#AA0000" },
        { '5', "#AA00AA" },
        { '6', "#FFAA00" },
        { '7', "#AAAAAA" },
        { '8', "#555555" },
        { '9', "#5555FF" },
        { 'a', "#55FF55" },
        { 'b', "#55FFFF" },
        { 'c', "#FF5555" },
        { 'd', "#FF55FF" },
        { 'e', "#FFFF55" },
        { 'f', "#FFFFFF" }
    };
}
