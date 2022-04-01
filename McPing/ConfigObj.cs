using System.Collections.Generic;

namespace McPing
{
    class RobotObj
    {
        public string IP { get; set; }
        public ushort Port { get; set; }
        public int Time { get; set; }
        public bool Check { get; set; }
    }
    class ShowObj
    {
        public string FontNormal { get; set; }
        public string FontEmoji { get; set; }
        public string FontBold { get; set; }
        public string FontItalic { get; set; }
        public string BGColor { get; set; }
        public string GoodPingColor { get; set; }
        public string BadPingColor { get; set; }
        public string PlayerColor { get; set; }
        public string VersionColor { get; set; }

    }
    class ConfigObj
    {
        public RobotObj Robot { get; set; }
        public ShowObj Show { get; set; }
        public List<long> Group { get; set; }
        public long RunQQ { get; set; }
        public string Head { get; set; }
        public string DefaultIP { get; set; }
        public int Delay { get; set; }
        public bool NoInput { get; set; }
    }
}
