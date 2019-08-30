using Newtonsoft.Json;
using System.IO;

namespace DkpDiscordBot
{
    public class Config
    {
        private const string configFolder = "Resources";
        private const string configFile = "config.json";
        private const string fullPathFile = configFolder + "/" + configFile;

        public static BotConfig bot;

        static Config()
        {
            if (!Directory.Exists(configFolder))
                Directory.CreateDirectory(configFolder);

            if (!File.Exists(fullPathFile))
            {
                bot = new BotConfig();
                string json = JsonConvert.SerializeObject(bot, Formatting.Indented);
                File.WriteAllText(fullPathFile, json);
            }
            else
            {
                string json = File.ReadAllText(fullPathFile);
                bot = JsonConvert.DeserializeObject<BotConfig>(json);
            }
        }
    }

    public struct BotConfig
    {
        public string token;
        public string cmdPrefix;
        public string raidRole;
        public string inRaid;
        public int minBid;
        public int auctionTime;
    }
}