using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McPing.PingTools
{
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
}
