using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DkpDiscordBot.Core.UserAccounts;
using NReco.ImageGenerator;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DkpDiscordBot.Modules
{
    public class Misc : ModuleBase<SocketCommandContext>
    {
        private readonly static Random rand = new Random();
        private readonly static Stopwatch stopwatchRaidingTime = new Stopwatch();
        private readonly static Stopwatch stopwatchAuctionTime = new Stopwatch();

        private static bool _raidStartSignal = false;

        private static bool _auctionSignal = false;
        private static string _auctionItemName;

        public static uint _lastBid;
        public static ulong _lastUserIdBid;
        public static uint _bidCounter;

        [Command("help")]
        public async Task Help()
        {
            var embed = new EmbedBuilder();
            embed.WithTitle("DKP-Bot");
            embed.WithColor(new Color(0, 255, 0));
            embed.WithThumbnailUrl("https://i.redd.it/9gqliaxco0201.png");
            embed.WithDescription("descriptions/help/about/contact");
            embed.AddField("\u200B", "\u200B");

            embed.AddField("!roll", "(player) Random number between 1-100.");
            //embed.AddField("!dice", "(player) Random number between 1-6.");
            embed.AddField("!mydkp", "(player) Show current dkp");
            embed.AddField("!stat @nickname", "(player) Show current dkp of selected players.");
            embed.AddField("!list", "(leader) Display all user/points from data list.");
            embed.AddField("!add @nickname", "(leader) Add user to data list.");
            embed.AddField("!remove @nickname", "(leader) Remove user from data list.");
            embed.AddField("\u200B", "\u200B");

            embed.AddField("!add-dkp VALUE @nickname", "(leader) Add dkp to specified user");
            embed.AddField("!set-dkp VALUE @nickname", "(leader) Set exactly number of dkp to specific player");
            embed.AddField("!remove-dkp VALUE @nickname", "(leader) Remove exactly number of dkp to specific player");
            embed.AddField("!reward VALUE", "(leader) Reward all online members with exactly number of dkp (player needs to have \'In Raid\' role)");
            embed.AddField("\u200B", "\u200B");

            embed.AddField("!inraid", "(leader) Check who has \"In Raid\" role");
            embed.AddField("!start", "(leader) Start the raid and timer");
            embed.AddField("!stop", "(leader) Stop the raid and timer, add elapsed time to players (with \'In Raid\' role)");

            embed.AddField("\u200B", "\u200B");

            embed.AddField("!auction ITEM", $"(leader) Start a new auction. Default value set to {Config.bot.auctionTime} seconds.");
            embed.AddField("!bid VALUE", $"(player) Allows players to bid their dkp for current auction item. Must be divisible with number {Config.bot.minBid}.");

            await Context.Channel.SendMessageAsync("", false, embed.Build(), null);
        }

        [Command("roll")]
        public async Task Roll()
        {
            var embed = new EmbedBuilder();
            embed.WithColor(new Color(235, 210, 52));
            embed.WithTitle(Context.User.Username);

            embed.WithDescription(" rolled (1-100): " + (StaticRandom.Instance.Next(100) + 1).ToString());
            await Context.Channel.SendMessageAsync("", false, embed.Build(), null);
        }

        #region DKP
        [Command("stat")]
        public async Task Stat([Remainder]string arg = "")
        {
            SocketUser target = null;
            var mentionedUser = Context.Message.MentionedUsers.FirstOrDefault();
            target = mentionedUser ?? Context.User;


            var account = UserAccounts.GetAccount(target);
            TimeSpan time = TimeSpan.FromSeconds(account.Time);
            await Context.Channel.SendMessageAsync($"{target.Username}, " +
                $" has {account.PointsDKP} dkp and total raiding time:{time.ToString(@"hh\:mm\:ss")} hours.");
        }

        [Command("list")]
        public async Task ShowList()
        {
            if (!IsUserRaidLeader((SocketGuildUser)Context.User))
                return;

            var sb = new StringBuilder();
            var accounts = UserAccounts.GetListAccounts();
            int i = 1;
            foreach (var account in accounts)
            {
                var curUsername = String.Format("{0}{1}{2}", Context.Guild.GetUser(account.UserID).Username, "#", Context.Guild.GetUser(account.UserID).DiscriminatorValue);
                TimeSpan time = TimeSpan.FromSeconds(account.Time);
                sb.Append(String.Format("{0}.{1}  -  DKP:[{2}]  -  Time:[{3}]" + Environment.NewLine, i, curUsername, account.PointsDKP, time.ToString(@"hh\:mm\:ss")));
                i++;
            }

            var embed = new EmbedBuilder();
            embed.WithColor(new Color(10, 255, 10));
            embed.WithTitle("DATA-MEMBER LIST:");

            embed.WithDescription(sb.ToString());
            await Context.Channel.SendMessageAsync("", false, embed.Build(), null);
        }

        [Command("inraid")]
        public async Task InRaid()
        {
            if (!IsUserRaidLeader((SocketGuildUser)Context.User))
                return;

            var sb = new StringBuilder();
            var onlineUsers = Context.Guild.Users;
            int i = 1;
            foreach (var onlineUser in onlineUsers)
            {
                // if user dont have "In Raid" role, continue
                if (!IsUserCurrentlyInRaid((SocketGuildUser)onlineUser))
                    continue;

                var accounts = UserAccounts.GetListAccounts();
                string dkp = "";
                foreach (var account in accounts)
                {
                    if (onlineUser.Id == account.UserID)
                    {
                        dkp = account.PointsDKP.ToString();
                        break;
                    }
                }

                var curUsername = String.Format("{0}{1}{2}", Context.Guild.GetUser(onlineUser.Id).Username, "#", onlineUser.DiscriminatorValue);
                sb.Append(String.Format("{0}.{1} - [dkp:{2}]" + Environment.NewLine, i, curUsername, dkp));
                i++;
            }

            var embed = new EmbedBuilder();
            embed.WithColor(new Color(10, 255, 10));
            embed.WithTitle($"IN RAID MEMBERS: [{i - 1}] ");

            embed.WithDescription(sb.ToString());
            await Context.Channel.SendMessageAsync("", false, embed.Build(), null);
        }

        [Command("mydkp")]
        public async Task MyDKP()
        {
            var account = UserAccounts.GetAccount(Context.User);
            TimeSpan time = TimeSpan.FromSeconds(account.Time);
            await Context.Channel.SendMessageAsync($"{Context.User}, " +
                $" you have {account.PointsDKP} dkp and total raiding time:{time.ToString(@"hh\:mm\:ss")} hours.");
        }

        #region acution

        [Command("bid")]
        public async Task Bid(uint dkp)
        {
            if (!IsUserCurrentlyInRaid((SocketGuildUser)Context.User))
                return;

            if (!_auctionSignal)
            {
                await Context.Channel.SendMessageAsync($"** Auction not started yet! **");
                return;
            }

            if (dkp % Config.bot.minBid != 0)
            {
                await Context.Channel.SendMessageAsync($"~~ Bid must be divisible with number: {Config.bot.minBid}");
                return;
            }

            if (dkp == 0)
            {
                await Context.Channel.SendMessageAsync($"~~ Bid must be greater then: ZERO");
                return;
            }


            if (Context.User.Id == _lastUserIdBid)
            {
                await Context.Channel.SendMessageAsync($"~~ You already place your bid!");
                return;
            }

            if (dkp <= _lastBid)
            {
                await Context.Channel.SendMessageAsync($"~~ Bid must be greater then last bid: {_lastBid}");
                return;
            }

            var account = UserAccounts.GetListAccounts().Where(x => x.UserID == Context.User.Id).SingleOrDefault();
            if (account != null)
            {
                if (account.PointsDKP >= dkp) // OK
                {
                    _lastBid = dkp;
                    _lastUserIdBid = Context.User.Id;
                    _bidCounter++;
                    var embed = new EmbedBuilder();
                    embed.WithTitle($"**{_bidCounter}. BID ACCEPTED FOR:**");
                    embed.WithDescription($"**{Context.User.Username} - dkp:[{dkp}]**");
                    embed.WithColor(new Color(0, 255, 0));
                    await Context.Channel.SendMessageAsync("", false, embed.Build(), null);

                    //await Context.Channel.SendMessageAsync($"**Bid accepted for {Context.User.Username} - dkp:[{dkp}]**");
                }
                else
                {
                    await Context.Channel.SendMessageAsync($"~~ Not enough dkp:[{account.PointsDKP}] - you bid with:[{dkp}]");
                }
            }
        }

        [Command("auction")]
        public async Task Auction([Remainder]string arg = "")
        {
            if (!IsUserRaidLeader((SocketGuildUser)Context.User))
                return;

            if (_auctionSignal)
            {
                await Context.Channel.SendMessageAsync($"**Auction already running!**");
                return;
            }
            _bidCounter = 0;
            _auctionItemName = arg;


            string html = ItemGrabber.GetItemInfo(_auctionItemName);
            var converter = new HtmlToImageConverter
            {
                Width = 340,

            };
            var jpgBytes = converter.GenerateImage(html, NReco.ImageGenerator.ImageFormat.Jpeg);

            new Task(new Action(AuctionFunction)).Start();
            await Context.Channel.SendMessageAsync($"**Auction start for item: {arg} - Bid time: [{Config.bot.auctionTime}] sec.**");
            await Context.Channel.SendFileAsync(new MemoryStream(jpgBytes), "itemPic.jpg");
        }

        private async void AuctionFunction()
        {
            int tempSec = 0;
            int auctionTime = Config.bot.auctionTime;
            bool signalTen = true;
            bool signalFive = true;
            try
            {
                stopwatchAuctionTime.Reset();
                stopwatchAuctionTime.Start();
                _auctionSignal = true;

                while (tempSec <= auctionTime)
                {
                    Thread.Sleep(100);
                    TimeSpan ts = stopwatchAuctionTime.Elapsed;
                    tempSec = ts.Seconds;

                    //display last 10 sec of auction time
                    if (auctionTime - tempSec == 10 && signalTen)
                    {
                        signalTen = false;
                        await Context.Channel.SendMessageAsync($"Auction ends in 10 sec...");
                    }

                    //display last 5 sec of auction time
                    if (auctionTime - tempSec == 5 && signalFive)
                    {
                        signalFive = false;
                        await Context.Channel.SendMessageAsync($"Auction ends in 5 sec...");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Auction Thread:\n" + ex.Message);
            }

            tempSec = 0;
            signalTen = true;
            signalFive = true;
            _auctionSignal = false;

            stopwatchAuctionTime.Stop();
            stopwatchAuctionTime.Reset();

            var account = UserAccounts.GetListAccounts().Where(x => x.UserID == _lastUserIdBid).SingleOrDefault();
            if (account != null)
            {
                account.PointsDKP -= _lastBid;
                UserAccounts.SaveAccounts();

                await Context.Channel.SendMessageAsync($"**Auction has ended!** (total bids:{_bidCounter}) - **{account.Username}, has won the {_auctionItemName}.** {account.Username}, your current dkp: [{account.PointsDKP}]");
            }
            else
            {
                await Context.Channel.SendMessageAsync($"**Auction has ended!** - For item:**{_auctionItemName}** - NO BIDS PLACED!");
            }

            _lastBid = 0;
            _lastUserIdBid = 0;
            _auctionItemName = String.Empty;
        }
        #endregion

        [Command("add")]
        public async Task AddUser([Remainder]string arg = "")
        {
            if (!IsUserRaidLeader((SocketGuildUser)Context.User))
                return;

            SocketUser target = null;
            var mentionedUser = Context.Message.MentionedUsers.FirstOrDefault();
            target = mentionedUser ?? Context.User;

            if (target == null)
                return;

            var account = UserAccounts.GetAccount(target);
            UserAccounts.SaveAccounts();

            await Context.Channel.SendMessageAsync($"{target.Username}, added to list.");
        }

        [Command("remove")]
        public async Task RemoveUser([Remainder]string arg = "")
        {
            if (!IsUserRaidLeader((SocketGuildUser)Context.User))
                return;

            SocketUser target = null;
            var mentionedUser = Context.Message.MentionedUsers.FirstOrDefault();
            target = mentionedUser ?? Context.User;

            if (target == null)
                return;

            UserAccounts.RemoveAccount(target);
            UserAccounts.SaveAccounts();

            await Context.Channel.SendMessageAsync($"{target.Username}, removed from list.");
        }

        [Command("add-dkp")]
        public async Task AddDKP(uint dkp, [Remainder]string arg = "")
        {
            if (!IsUserRaidLeader((SocketGuildUser)Context.User))
                return;

            SocketUser target = null;
            var mentionedUser = Context.Message.MentionedUsers.FirstOrDefault();
            target = mentionedUser ?? Context.User;

            if (target == null)
                return;


            var account = UserAccounts.GetAccount(target);

            account.PointsDKP += dkp;
            UserAccounts.SaveAccounts();
            await Context.Channel.SendMessageAsync($"{target.Username}, gained {dkp} dkp.");
        }

        [Command("remove-dkp")]
        public async Task RemoveDKP(uint dkp, [Remainder]string arg = "")
        {
            if (!IsUserRaidLeader((SocketGuildUser)Context.User))
                return;

            SocketUser target = null;
            var mentionedUser = Context.Message.MentionedUsers.FirstOrDefault();
            target = mentionedUser ?? Context.User;

            if (target == null)
                return;


            var account = UserAccounts.GetAccount(target);
            if (dkp > account.PointsDKP)
                account.PointsDKP = 0;
            else
                account.PointsDKP -= dkp;

            UserAccounts.SaveAccounts();
            await Context.Channel.SendMessageAsync($"{target.Username}, removed {dkp} dkp. Current dkp:{account.PointsDKP}");
        }

        [Command("set-dkp")]
        public async Task SetDKP(uint dkp, [Remainder]string arg = "")
        {
            if (!IsUserRaidLeader((SocketGuildUser)Context.User))
                return;

            SocketUser target = null;
            var mentionedUser = Context.Message.MentionedUsers.FirstOrDefault();
            target = mentionedUser ?? Context.User;

            if (target == null)
                return;

            var account = UserAccounts.GetAccount(target);

            account.PointsDKP = dkp;

            UserAccounts.SaveAccounts();
            await Context.Channel.SendMessageAsync($"{target.Username}, set to {dkp} dkp.");
        }

        [Command("reward")]
        public async Task RewardDKP(uint dkp)
        {
            if (!IsUserRaidLeader((SocketGuildUser)Context.User))
                return;

            var onlineUsers = Context.Guild.Users.Where(x => x.Status.ToString() == "Online");

            var accounts = UserAccounts.GetListAccounts();
            foreach (var account in accounts)
            {
                foreach (var onlineUser in onlineUsers)
                {
                    // if user dont have "In Raid" role, continue
                    if (!IsUserCurrentlyInRaid((SocketGuildUser)onlineUser))
                        continue;

                    if (account.UserID == onlineUser.Id)
                    {
                        account.PointsDKP += dkp;
                        continue;
                    }
                }
            }

            UserAccounts.SaveAccounts();
            await Context.Channel.SendMessageAsync($"**All raid members got {dkp} dkp.**");
        }
        #endregion

        #region start-stop
        [Command("start")]
        public async Task StartRaid()
        {
            if (_raidStartSignal)
            {
                TimeSpan time = TimeSpan.FromSeconds(GetPassedTime());
                await Context.Channel.SendMessageAsync($"Start is already activated: **[{time.ToString(@"hh\:mm\:ss")}]**");
                return;
            }

            if (!IsUserRaidLeader((SocketGuildUser)Context.User))
                return;

            stopwatchRaidingTime.Reset();
            stopwatchRaidingTime.Start();
            _raidStartSignal = true;

            var sb = new StringBuilder();
            var users = Context.Guild.Users;
            int i = 1;
            foreach (var user in users)
            {
                if (!IsUserCurrentlyInRaid(user))
                    continue;

                sb.Append($"{i}.[{user.Username}] - [In Raid] " + Environment.NewLine);
                i++;
            }

            await Context.Channel.SendMessageAsync($"** - RAID STARTED -  [{DateTime.Now.ToString("HH:mm:ss tt")}]** :sweat_smile: " + Environment.NewLine + sb.ToString());
        }

        private ulong GetPassedTime()
        {
            ulong hh = (ulong)stopwatchRaidingTime.Elapsed.Hours * 60 * 60;
            ulong mm = (ulong)stopwatchRaidingTime.Elapsed.Minutes * 60;
            ulong ss = (ulong)stopwatchRaidingTime.Elapsed.Seconds;

            return hh + mm + ss;
        }

        [Command("stop")]
        public async Task StopRaid()
        {
            if (_raidStartSignal == false)
            {
                await Context.Channel.SendMessageAsync($"Raid is not activated!");
                return;
            }

            if (!IsUserRaidLeader((SocketGuildUser)Context.User))
                return;

            stopwatchRaidingTime.Stop();
            _raidStartSignal = false;

            TimeSpan time = TimeSpan.FromSeconds(GetPassedTime());


            var onlineUsers = Context.Guild.Users;
            var accounts = UserAccounts.GetListAccounts();

            foreach (var account in accounts)
            {
                foreach (var onlineUser in onlineUsers)
                {
                    // if user have "In Raid" role add time
                    if (!IsUserCurrentlyInRaid((SocketGuildUser)onlineUser))
                        continue;

                    if (account.UserID == onlineUser.Id)
                    {
                        account.Time += GetPassedTime();
                        break;
                    }
                }
            }

            UserAccounts.SaveAccounts();
            await Context.Channel.SendMessageAsync($"** RAID STOPPED ** -- Time elapsed: **[{time.ToString(@"hh\:mm\:ss")}]** ");
        }
        #endregion

        /* NOT IN USE
        [Command("secret")]
        //[RequireUserPermission(GuildPermission.Administrator)]
        public async Task RevealSecret([Remainder] string arg = "")
        {
            if (!IsUserRaidLeader((SocketGuildUser)Context.User))
                return;

            var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync(Utilities.GetAlert("SECRET"));
        }

        [Command("echo")]
        public async Task Echo([Remainder] string msg)
        {
            await Context.Channel.SendMessageAsync(msg);
        }

        [Command("pick")]
        public async Task PickOne([Remainder] string msg)
        {
            msg = msg.Trim();
            string[] options = msg.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            string selection = options[rand.Next(0, options.Length)];

            var embed = new EmbedBuilder();
            embed.WithTitle("Choice for " + Context.User.Username);
            embed.WithDescription(selection);
            embed.WithColor(new Color(rand.Next(256), rand.Next(256), rand.Next(256)));

            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("data")]
        public async Task GetData()
        {
            await Context.Channel.SendMessageAsync("Data Has " + DataStorage.GetPairsCount() + "  pairs.");
            DataStorage.AddPairToStorage("Count" + DataStorage.GetPairsCount(), "TheCount" + DataStorage.GetPairsCount());
        }

        [Command("add-dkp")]
        public async Task AddDKP(uint dkp)
        {
            if (!IsUserRaidLeader((SocketGuildUser)Context.User))
                return;

            var account = UserAccounts.GetAccount(Context.User);
            account.PointsDKP += dkp;
            UserAccounts.SaveAccounts();
            await Context.Channel.SendMessageAsync($"You gained {dkp} dkp.");
        }
    

        [Command("dice")]
        public async Task Dice()
        {
            await Context.Channel.SendMessageAsync(Context.User.Username + "'s dice fell on (1-6): "
                + (StaticRandom.Instance.Next(6) + 1).ToString());
        }

        */

        #region check roles
        private bool IsUserRaidLeader(SocketGuildUser user)
        {
            string raidLeaderRole = Config.bot.raidRole;

            foreach (var role in user.Roles)
                if (role.Name == raidLeaderRole)
                    return true;

            return false;
        }

        private bool IsUserCurrentlyInRaid(SocketGuildUser user)
        {
            string inRaidRole = Config.bot.inRaid;

            foreach (var role in user.Roles)
                if (role.Name == inRaidRole)
                    return true;

            return false;
        }
        #endregion
    }
}

