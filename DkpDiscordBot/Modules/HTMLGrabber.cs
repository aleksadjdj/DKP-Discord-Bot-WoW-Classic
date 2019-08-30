using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace DkpDiscordBot.Modules
{

    // NEED TO FIX THIS CLASS 
    public class HTMLGrabber
    {
        //https://wow.gamepedia.com/Quality
        private static ItemColor itemColor;

        public static int Height;


        private enum ItemColor
        {
            Poor = 0,
            Common = 1,
            Uncommon = 2,
            Rare = 3,
            Epic = 4,
            Legendary = 5,
            Artifact = 6
        }

        private static string GetData(string url)
        {
            string htmlCode = "";

            using (WebClient client = new WebClient())
            {
                htmlCode = client.DownloadString(url);
            }
            return htmlCode;
        }


        private static string StripHTML(string input)
        {
            return Regex.Replace(input, "<.*?>", "\n");
        }

        public static string GetItemInfo(string inputName)
        {
            inputName = inputName.Replace("[", "").Replace("]", "").Trim();
            var html = GetData("https://classicdb.ch/?search=" + inputName);

            string[] lines = html.Replace("\r", "").Split('\n');

            string targetLinePattern = "<td><table><tr><td><b class=\"q";

            var target = lines.Where(x => x.Contains(targetLinePattern)).FirstOrDefault();

            if (target != null)
            {
                target = target.Trim();
                var selectColorIndex = target.Substring(targetLinePattern.Length, 1);
                System.Console.WriteLine("Selected Item Color:" + selectColorIndex);
                Int32.TryParse(selectColorIndex, out int colorIndex);
                itemColor = (ItemColor)colorIndex;

                target = StripHTML(target);

                var targetList = target.Split('\n').ToList();

                var result = new List<string>();

                foreach (var item in targetList)
                {
                    if (item != "")
                        result.Add(item.Trim());

                }
                return FormatToHTML(result);
            }
            else
            {
                Height = 40;
                return "<style>.b2{color: #1eff00;}</style><a class=\"b2\">NO ITEM DATA<a/>";
            }
        }

        private static string FormatToHTML(List<string> list)
        {
            Height = 25;
            var sb = new StringBuilder();

            sb.Append("<table border=\"2\"><tr><th align=\"left\">");

            for (int i = 0; i < list.Count; i++)
            {
                Height += 22;

                if (i == 0) // add item color header
                {
                    sb.Append($"<b class=\"b{(int)itemColor}\">{list[0]}</b><br />");
                    continue;
                }

                string s = list[i];
                if (s[0] == '+') // set white color to stats 
                {
                    sb.Append($"<a class=\"b1\">{ list[i] }</a><br />");
                    continue;
                }

                if (list[i].Equals("Equip:") || list[i].Equals("Use:") || list[i].Equals("Chance on hit:")) // ih have use or equip add green color
                {
                    sb.Append($"<a class=\"b2\">{ list[i] }{ list[i + 1] }</a><br />");
                    i++;
                    continue;
                }

                if (list[i].Contains("No description:"))
                {
                    sb.Append($"<a class=\"b2\">{ list[i] }</a><br />");
                    continue;
                }

                if (list[i].Contains("Requires Level")) // set requres to red color
                {
                    sb.Append($"<a class=\"b1\">{ list[i] }</a><br />");
                    continue;
                }


                if (list[i].Contains("Requires")) // set requres to red color
                {
                    sb.Append($"<a class=\"b8\">{ list[i] }</a><br />");
                    continue;
                }


                if (Regex.IsMatch(list[i], "^\"(.+)\"$")) // if line have "" set text to golden color
                {
                    sb.Append($"<a class=\"b7\">{ list[i] }</a><br />");
                    continue;
                }

                if (i == 3) // hand and  weapon tipe in same line format
                {
                    sb.Append($"<b class=\"b1\">{list[i]} {list[i + 1]}</b><br />");
                    i++;
                    continue;
                }

                if (i == 5) // dmg and speed in same line format
                {
                    sb.Append($"<b class=\"b1\">{list[i]} - {list[i + 1]}</b><br />");
                    i++;
                    continue;
                }


                // rest of text is white color
                sb.Append($"<a class=\"b1\">{ list[i] }</a><br />");


            }

            sb.Append("</th></tr></table>");

            sb.Append("<style>");
            sb.Append("body {background-color: #070c21;}");

            sb.Append(".b0 {color: #9d9d9d;}");
            sb.Append(".b1 {color: #ffffff;}");
            sb.Append(".b2 {color: #1eff00;}");
            sb.Append(".b3 {color: #0070dd;}");
            sb.Append(".b4 {color: #a335ee;}");
            sb.Append(".b5 {color: #ff8000;}");
            sb.Append(".b6 {color: #e6cc80;}");
            sb.Append(".b7 {color: #ffd100;}");
            sb.Append(".b8 {color: #ff0000;}");
            sb.Append("</style>");


            return sb.ToString();
        }

    }
}
