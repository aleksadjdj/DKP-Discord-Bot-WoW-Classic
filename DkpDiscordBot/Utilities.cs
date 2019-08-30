using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
namespace DkpDiscordBot
{
    public static class Utilities
    {
        private static Dictionary<string, string> alerts;

        static Utilities()
        {
            string json = File.ReadAllText("SystemLang/alerts.json");
            var data = JsonConvert.DeserializeObject<dynamic>(json);

            alerts = data.ToObject<Dictionary<string, string>>();
        }

        public static string GetAlert(string key)
        {
            if (alerts.ContainsKey(key)) return alerts[key];

            return "<NO_DATA>";
        }


        public static string GetFormattedAlert(string key, params object[] parameter)
        {
            if (alerts.ContainsKey(key))
                return String.Format(alerts[key], parameter);
            return "";

        }


        public static string GetFormattedAlert(string key, object parameter)
        {
            return String.Format(alerts[key], new object[] { parameter });
        }
    }
}
