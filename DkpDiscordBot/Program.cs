using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace DkpDiscordBot
{
    class Program
    {
        private DiscordSocketClient _client;
        private CommandHandler _handler;

        static void Main(string[] args) => new Program().StartAsync().GetAwaiter().GetResult();

        public async Task StartAsync()
        {
            //string name = "nameTest_abc";
            //string botName = "botNameTest_abc";
            //string message = Utilities.GetFormattedAlert("WELCOME_&NAME_&BOTNAME", name, botName);
            //Console.WriteLine(message);
            //return;

            if (Config.bot.token == "" || Config.bot.token == null)
                return;

            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose
            });

            _client.Log += Log;
            await _client.LoginAsync(TokenType.Bot, Config.bot.token);
            await _client.StartAsync();

            _handler = new CommandHandler();
            await _handler.InitAsync(_client);
            await Task.Delay(-1);
        }

        private async Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.Message);
        }
    }
}
