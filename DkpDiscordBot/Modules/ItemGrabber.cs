using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace DkpDiscordBot.Modules
{
    // NEED TO FIX THIS CLASS 
    public class ItemGrabber
    {
        //https://wow.gamepedia.com/Quality
        private static ItemColor itemColor;

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

        private enum ClassColor
        {
            Error = -1,
            Druid = 0,
            Hunter = 1,
            Mage = 2,
            Paladin = 3,
            Priest = 4,
            Rogue = 5,
            Shaman = 6,
            Warlock = 7,
            Warrior = 8,

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


            if (target == null)
            {
                var secondTargetPattern = "g_items;_[";
                int index = -1;
                string htmlLine = String.Empty;
                var sb = new StringBuilder();
                foreach (var line in lines)
                {
                    if (line.Contains(secondTargetPattern))
                    {
                        htmlLine = line;
                        index = line.IndexOf(secondTargetPattern);
                        break;
                    }
                }

                index += secondTargetPattern.Length;
                char c = htmlLine[index];
                while (Char.IsDigit(c))
                {
                    sb.Append(c);
                    index++;
                    c = htmlLine[index];
                }

                //try again if there are more same items ... just give us 1st result from db 
                Int32.TryParse(sb.ToString(), out int resultItemNumber);
                html = GetData("https://classicdb.ch/?item=" + resultItemNumber);
                string[] lines2 = html.Replace("\r", "").Split('\n');
                target = lines2.Where(x => x.Contains(targetLinePattern)).FirstOrDefault();
            }


            if (target != null)
            {

                target = target.Trim();
                var selectColorIndex = target.Substring(targetLinePattern.Length, 1);
                //System.Console.WriteLine("Selected Item Color:" + selectColorIndex);
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
                return "<style>.b2{color: #1eff00;}</style><a class=\"b2\">Can't load item!<a/>";
            }
        }

        private static string FormatToHTML(List<string> list)
        {
            var sb = new StringBuilder();
            string itemName = "";
            bool signalFirstSet = true;
            bool isSetItem = false;
            sb.AppendLine("<table cellpadding=\"6\" border=\"1\"><tr><td align=\"left\"><p class=\"sansserif\">");

            bool firstBenefit = true;


            // item name header
            sb.AppendLine($"<b class=\"b{(int)itemColor}\">{list[0]}</b><br />");
            itemName = list[0].Substring(0, list[0].Trim().IndexOf(" "));

            foreach (var item in list)
            {
                if (item.Trim().Contains("(0/"))
                    isSetItem = true;
            }


            for (int i = 1; i < list.Count; i++)
            {
                // set white color to stats
                string s = list[i].Trim();
                if (s[0] == '+')
                {
                    sb.AppendLine($"<a class=\"b1\">{ list[i] }</a><br />");
                    continue;
                }

                if (list[i].Contains("Binds when picked up") || list[i].Contains("Binds when equipped") || list[i].Contains("Unique") ||
                     list[i].Contains("Durability") || list[i].Contains("Block") || list[i].Contains("Armor")
                     || list[i].Contains("Requires Level")) // set white color to stats 
                {
                    sb.AppendLine($"<a class=\"b1\">{ list[i] }</a><br />");
                    continue;
                }

                if (list[i].Contains(") Set:")) // set white color to stats 
                {
                    if (firstBenefit)
                    {
                        firstBenefit = false;
                        sb.Append("<br />");
                    }
                    sb.AppendLine($"<a class=\"b0\">{ list[i] }&nbsp;{ list[i + 1] }</a><br />");
                    i++;
                    continue;
                }

                // use or equip set green color
                if (list[i].Equals("Equip:") || list[i].Equals("Use:") || list[i].Equals("Chance on hit:"))
                {
                    sb.AppendLine($"<a class=\"b2\">{ list[i] }&nbsp;{ list[i + 1] }</a><br />");
                    i++;
                    continue;
                }

                if (list[i].Equals("Classes:"))
                {
                    string className = list[i + 1].Trim();
                    ClassColor cs = ClassColor.Error;

                    switch (className)
                    {
                        case "Druid":
                            cs = ClassColor.Druid;
                            break;
                        case "Hunter":
                            cs = ClassColor.Hunter;
                            break;
                        case "Mage":
                            cs = ClassColor.Mage;
                            break;
                        case "Paladin":
                            cs = ClassColor.Paladin;
                            break;
                        case "Priest":
                            cs = ClassColor.Paladin;
                            break;
                        case "Rogue":
                            cs = ClassColor.Rogue;
                            break;
                        case "Shaman":
                            cs = ClassColor.Shaman;
                            break;
                        case "Warlock":
                            cs = ClassColor.Warlock;
                            break;
                        case "Warrior":
                            cs = ClassColor.Warrior;
                            break;
                    }

                    sb.AppendLine($"<a class=\"b1\">{ list[i] }</a>&nbsp;<a class=\"c{(int)cs}\">{ list[i + 1] }</a><br />");
                    i++;

                    continue;
                }

                if (list[i].Contains("No description:"))
                {
                    sb.AppendLine($"<a class=\"b2\">{ list[i] }</a><br />");
                    continue;
                }

                /*
                if (list[i].Contains("Requires Level")) // set requires to red color
                {
                    sb.AppendLine($"<a class=\"b1\">{ list[i] }</a><br />");
                    continue;
                }

                if (list[i].Contains("Requires")) // set requires to red color
                {
                    sb.AppendLine($"<a class=\"b8\">{ list[i] }</a><br />");
                    continue;
                }
                */

                // if line have on start and end "", set text to golden color
                if (Regex.IsMatch(list[i], "^\"(.+)\"$"))
                {
                    sb.AppendLine($"<a class=\"b7\">{ list[i] }</a><br />");
                    continue;
                }

                // format weapon/armor type in same line 
                if (signalFirstSet == true)
                {
                    var typeList = new List<string>()
                    {
                        "One-hand", "Main Hand",  "Two-hand", "Ranged", "Off Hand", "Chest", "Feet", "Hands", "Head", "Legs", "Shoulder", "Waist", "Wrist",
                        "Relic"
                    };
                    foreach (var item in typeList)
                    {
                        if (list[i].Contains(item))
                        {
                            sb.AppendLine($"<a class=\"b1\">{list[i]} &nbsp; {list[i + 1]}</a><br />");
                            i += 2;
                            continue;
                        }
                    }
                }

                // 155 - 233 Damage (next line is speed 3.60) ex
                // we need to format that in same line
                char firstChar = (list[i])[0];
                if (Char.IsDigit(firstChar) && !list[i].Contains("Armor"))
                {
                    sb.AppendLine($"<a class=\"b1\">{list[i]} &nbsp; {list[i + 1]}</a><br />");
                    i++;
                    continue;
                }

                if (isSetItem)
                {
                    //first line is gold color 
                    if (list[i].Contains("(0/"))
                    {
                        sb.Replace(list[i - 1], "");
                        sb.AppendLine($"<a class=\"b7\">{ list[i - 1] + "&nbsp;" + list[i] }</a><br />");
                        signalFirstSet = false;
                        continue;
                    }

                    // list of set items
                    if (signalFirstSet == false)
                    {
                        sb.AppendLine($"<a class=\"b0\">{ "&nbsp;&nbsp;" + list[i] }</a><br />");
                        continue;
                    }
                }



                // if exist rest of text will be white color
                sb.AppendLine($"<a class=\"b1\">{ list[i] }</a><br />");
            }

            sb.AppendLine("<style>");
            sb.AppendLine("body {background-color: #070c21;}");
            sb.AppendLine("p.sansserif { font-family:Verdana,sans-serif; font-size: 12px; line-height: 17px;}");

            sb.AppendLine(".b0 {color: #9d9d9d;}"); // poor
            sb.AppendLine(".b1 {color: #ffffff;}"); // common
            sb.AppendLine(".b2 {color: #1eff00;}"); // uncommon
            sb.AppendLine(".b3 {color: #0070dd;}"); // rare
            sb.AppendLine(".b4 {color: #a335ee;}"); // unique
            sb.AppendLine(".b5 {color: #ff8000;}"); // legendary
            sb.AppendLine(".b6 {color: #e6cc80;}"); // artifact

            sb.AppendLine(".b7 {color: #ffd100;}"); // gold - "text"
            sb.AppendLine(".b8 {color: #ff0000;}"); // red - required

            sb.AppendLine(".c0 {color: #FF7D0A;}"); // druid color
            sb.AppendLine(".c1 {color: #ABD473;}"); // hunter color
            sb.AppendLine(".c2 {color: #40C7EB;}"); // mage color
            sb.AppendLine(".c3 {color: #F58CBA;}"); // paladin color
            sb.AppendLine(".c4 {color: #FFFFFF;}"); // priest color
            sb.AppendLine(".c5 {color: #FFF569;}"); // rogue color
            sb.AppendLine(".c6 {color: #0070DE;}"); // shaman color
            sb.AppendLine(".c7 {color: #8787ED;}"); // warlock color
            sb.AppendLine(".c8 {color: #C79C6E;}"); // warrior color
            sb.AppendLine("</style>");

            return sb.ToString();
        }
    }
}
