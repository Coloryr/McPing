using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace McPing.PingTools
{
    /// <summary>
    /// Contains information about a modded server install.
    /// </summary>
    public class ForgeInfo
    {
        /// <summary>
        /// Represents an individual forge mod.
        /// </summary>
        public class ForgeMod
        {
            public ForgeMod(string ModID, string Version)
            {
                this.ModID = ModID;
                this.Version = Version;
            }

            public readonly string ModID;
            public readonly string Version;

            public override string ToString()
            {
                return ModID + " [" + Version + ']';
            }
        }

        public List<ForgeMod> Mods;

        /// <summary>
        /// Create a new ForgeInfo from the given data.
        /// </summary>
        /// <param name="data">The modinfo JSON tag.</param>
        internal ForgeInfo(JToken data)
        {

            Mods = new List<ForgeMod>();
            foreach (JToken mod in data["modList"])
            {
                string modid = mod["modid"].ToString();
                string version = mod["version"].ToString();

                Mods.Add(new ForgeMod(modid, version));
            }
        }
    }
}
