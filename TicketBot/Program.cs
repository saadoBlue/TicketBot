using System;
using Discord.WebSocket;
using Discord.Rest;
using System.Threading.Tasks;
using Discord;
using System.Linq;

namespace TicketBot
{
    class Program
    {
        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            await Task.Factory.StartNew(InitializeBot);
            guildManager = new GuildManager(client);
            guildManager.Initialize();
            await Task.Delay(-1);
        }

        #region Propreties
        private static DiscordSocketClient client;
        private const string Token = "NTg2MzA1MTMyODY2NTY4MjMx.XPmGHQ.lFWVSCpZjT7x2HFDb8bZT5rXHJ8";
        public static GuildManager guildManager;
        #endregion

        #region Functions
        public async static Task InitializeBot()
        {
            client = new DiscordSocketClient();
            client.Log += Log;
            await client.LoginAsync(TokenType.Bot, Token);
            await client.StartAsync();
            client.ReactionAdded += Client_ReactionAdded;
            client.MessageReceived += Client_MessageReceived;
        }

        private static Task Client_MessageReceived(SocketMessage message)
        {
            if (message == null || !(message.Channel is SocketGuildChannel))
                return Task.CompletedTask;

            var guild = (message.Channel as SocketGuildChannel).Guild;

            if (message.Content.StartsWith("$"))
            {
                if ((message.Author as SocketGuildUser).Roles.All(x => !x.Permissions.Administrator) && message.Author.Id != guild.Owner.Id)
                    return Task.CompletedTask;

                var CommandArguments = message.Content.Split('~');

                if (CommandArguments.Length < 3) return Task.CompletedTask;

                var commandIdentifier = CommandArguments[0];
                var ticketName = CommandArguments[1];
                var Message = CommandArguments.Length > 3 ? CommandArguments[3] : "";

                switch(commandIdentifier)
                {
                    case "$setup":
                        if (message.MentionedChannels.Count != 1) return Task.CompletedTask;
                        var mentionnedChannel = message.MentionedChannels.FirstOrDefault();
                        guildManager.SetupMessage(ticketName, Message, guild, mentionnedChannel as SocketTextChannel);
                        break;
                }
            }

            return Task.CompletedTask;
        }

        private static Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var User = reaction.User.Value as SocketGuildUser;
            var Channel = channel as SocketGuildChannel;
            if(User != null && Channel != null)
                guildManager.HandleReaction(Channel.Guild, User, reaction);

            return Task.CompletedTask;
        }

        private static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
        #endregion
    }
}
