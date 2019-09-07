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
        public static int ImgHeight;
        private const int Height = 18;

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
                var targetSecondPattern = "g_items;_[";
                int index = -1;
                string htmlLine = "";
                StringBuilder sb = new StringBuilder();
                foreach (var line in lines)
                {
                    if (line.Contains(targetSecondPattern))
                    {
                        htmlLine = line;
                        index = line.IndexOf(targetSecondPattern);
                        break;
                    }
                }

                index += targetSecondPattern.Length;
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
                ImgHeight = 40;
                return "<style>.b2{color: #1eff00;}</style><a class=\"b2\">Can't load item!<a/>";
            }
        }


        private static bool LengthCheck(int strLengt)
        {
            if (strLengt > 40)
                return true;

            return false;
        }

        private static string FormatToHTML(List<string> list)
        {
            ImgHeight = Height;
            var sb = new StringBuilder();
            string itemName = "";
            bool signalFirstSet = true;
            sb.Append("<table cellpadding=\"6\" border=\"1\"><tr><td align=\"left\"><p class=\"sansserif\">");


            for (int i = 0; i < list.Count; i++)
            {
                ImgHeight += Height;

                if (i == 0) // add item color header
                {
                    sb.Append($"<b class=\"b{(int)itemColor}\">{list[0]}</b><br />");
                    itemName = list[0].Substring(0, list[0].IndexOf(" "));
                    continue;
                }

                string s = list[i].Trim();
                if (s[0] == '+') // set white color to stats 
                {
                    sb.Append($"<a class=\"b1\">{ list[i] }</a><br />");
                    if (LengthCheck(list[i].Length)) ImgHeight += Height;
                    continue;
                }

                if (list[i].Contains("Set:")) // set white color to stats 
                {
                    sb.Append($"<a class=\"b0\">{ list[i] } { list[i + 1] }</a><br />");
                    if (LengthCheck(list[i].Length + list[i + 1].Length)) ImgHeight += Height;
                    i++;
                    continue;
                }

                // use or equip set green color
                if (list[i].Equals("Equip:") || list[i].Equals("Use:") || list[i].Equals("Chance on hit:"))
                {
                    sb.Append($"<a class=\"b2\">{ list[i] } { list[i + 1] }</a><br />");
                    if (LengthCheck(list[i].Length + list[i + 1].Length)) ImgHeight += Height;
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

                    sb.Append($"<a class=\"b1\">{ list[i] }</a> <a class=\"c{(int)cs}\">{ list[i + 1] }</a><br />");
                    if (LengthCheck(list[i].Length + list[i + 1].Length)) ImgHeight += Height;

                    i++;

                    continue;
                }

                if (list[i].Contains("No description:"))
                {
                    sb.Append($"<a class=\"b2\">{ list[i] }</a><br />");
                    if (LengthCheck(list[i].Length)) ImgHeight += Height;
                    continue;
                }

                if (list[i].Contains("Requires Level")) // set requires to red color
                {
                    sb.Append($"<a class=\"b1\">{ list[i] }</a><br />");
                    if (LengthCheck(list[i].Length)) ImgHeight += Height;
                    continue;
                }

                if (list[i].Contains("Requires")) // set requires to red color
                {
                    sb.Append($"<a class=\"b8\">{ list[i] }</a><br />");
                    if (LengthCheck(list[i].Length)) ImgHeight += Height;
                    continue;
                }

                if (Regex.IsMatch(list[i], "^\"(.+)\"$")) // if line have "" set text to golden color
                {
                    sb.Append($"<a class=\"b7\">{ list[i] }</a><br />");
                    if (LengthCheck(list[i].Length)) ImgHeight += Height;
                    continue;
                }

                // format weapon/armor type in same line 
                var typeList = new List<string>()
                {
                    "One-hand", "Main Hand",  "Two-hand", "Ranged", "Off Hand", "Chest", "Feet", "Hands", "Head", "Legs", "Shoulder", "Waist", "Wrist",
                };
                foreach (var item in typeList)
                {
                    if (list[i].Contains(item))
                    {
                        sb.Append($"<a class=\"b1\">{list[i]} {list[i + 1]}</a><br />");
                        if (LengthCheck(list[i].Length + list[i + 1].Length)) ImgHeight += Height;
                        i += 2;


                        continue;
                    }
                }

                // ex 155 - 233 Damage (next line is speed 3.60)
                // we need to format that in same line
                char firstChar = (list[i])[0];
                if (Char.IsDigit(firstChar) && !list[i].Contains("Armor"))
                {
                    sb.Append($"<a class=\"b1\">{list[i]}  {list[i + 1]}</a><br />");
                    if (LengthCheck(list[i].Length + list[i + 1].Length)) ImgHeight += Height;
                    i++;
                    continue;
                }

                if (list[i].Contains(itemName))
                {
                    if (signalFirstSet)
                    {
                        sb.Append($"<a class=\"b0\">{ list[i + 1] + " " + list[i] }</a><br />");
                        if (LengthCheck(list[i].Length + list[i + 1].Length)) ImgHeight += Height;
                        i++;
                        signalFirstSet = false;
                        continue;
                    }

                    sb.Append($"<a class=\"b0\">{ "&nbsp;&nbsp;" + list[i] }</a><br />");
                    if (LengthCheck(list[i].Length)) ImgHeight += Height;

                    continue;
                }

                // rest of text will be white color
                sb.Append($"<a class=\"b1\">{ list[i] }</a><br />");
            }

            ImgHeight += Height;

            sb.Append("</p></td></tr></table>");

            sb.Append("<style>");
            sb.Append("body {background-color: #070c21;}");
            sb.Append("p.sansserif { font-family:Verdana,sans-serif; font-size: 12px; line-height: 17px;}");

            sb.Append(".b0 {color: #9d9d9d;}");
            sb.Append(".b1 {color: #ffffff;}");
            sb.Append(".b2 {color: #1eff00;}");
            sb.Append(".b3 {color: #0070dd;}");
            sb.Append(".b4 {color: #a335ee;}");
            sb.Append(".b5 {color: #ff8000;}");
            sb.Append(".b6 {color: #e6cc80;}");
            sb.Append(".b7 {color: #ffd100;}");
            sb.Append(".b8 {color: #ff0000;}");

            sb.Append(".c0 {color: #FF7D0A;}"); // druid color
            sb.Append(".c1 {color: #ABD473;}"); // hunter color
            sb.Append(".c2 {color: #40C7EB;}"); // mage color
            sb.Append(".c3 {color: #F58CBA;}"); // paladin color
            sb.Append(".c4 {color: #FFFFFF;}"); // priest color
            sb.Append(".c5 {color: #FFF569;}"); // rogue color
            sb.Append(".c6 {color: #0070DE;}"); // shaman color
            sb.Append(".c7 {color: #8787ED;}"); // warlock color
            sb.Append(".c8 {color: #C79C6E;}"); // warrior color
            sb.Append("</style>");

            return sb.ToString();
        }
    }
}
