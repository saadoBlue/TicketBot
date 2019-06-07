using System;
using Discord.WebSocket;
using Discord.Rest;
using System.Threading.Tasks;
using Discord;

namespace TicketBot
{
    class Program
    {
        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            await Task.Factory.StartNew(InitializeBot);
            await Task.Delay(-1);
        }

        #region Propreties
        private static DiscordSocketClient client;
        private const string Token = "NTg2MzA1MTMyODY2NTY4MjMx.XPmGHQ.lFWVSCpZjT7x2HFDb8bZT5rXHJ8";
        #endregion

        #region Functions
        public async static Task InitializeBot()
        {
            client = new DiscordSocketClient();
            client.Log += Log;
            await client.LoginAsync(TokenType.Bot, Token);
            await client.StartAsync();
        }

        private static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
        #endregion
    }
}
