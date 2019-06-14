using System;
using Discord.WebSocket;
using Discord.Rest;
using System.Threading.Tasks;
using Discord;
using System.Linq;
using System.IO;

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
            if (!Directory.Exists("./Transcripts")) Directory.CreateDirectory("./Transcripts");
            await Task.Delay(-1);
        }

        #region Propreties
        private static DiscordSocketClient client;
        public const string Token = "NTg2MzA1MTMyODY2NTY4MjMx.XPmGHQ.lFWVSCpZjT7x2HFDb8bZT5rXHJ8";
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
            client.ChannelDestroyed += Client_ChannelDestroyed;
            client.MessageDeleted += Client_MessageDeleted;
        }

        private static Task Client_MessageDeleted(Cacheable<IMessage, ulong> arg1, ISocketMessageChannel arg2)
        {
            var channel = arg2 as SocketGuildChannel;
            if (channel != null)
            {
                var messageId = arg1.Id;
                guildManager.HandleMessageDeletion(messageId, channel.Guild.Id);
            }
            return Task.CompletedTask;
        }

        private static Task Client_ChannelDestroyed(SocketChannel arg)
        {
            var channel = arg as SocketGuildChannel;
            if (channel != null)
                guildManager.HandleChannelDeletion(channel);

            return Task.CompletedTask;
        }

        private static Task Client_MessageReceived(SocketMessage message)
        {
            var channel = message.Channel as SocketTextChannel;
            if (message == null || channel == null)
                return Task.CompletedTask;

            var guild = channel.Guild;

            if(guild == null)
                return Task.CompletedTask;

            if (message.Content.StartsWith("$setup"))
            {
                if ((message.Author as SocketGuildUser).Roles.All(x => !x.Permissions.Administrator) && message.Author.Id != guild.Owner.Id)
                    return Task.CompletedTask;

                var CommandArguments = message.Content.Split(' ');

                if (CommandArguments.Length < 3) return Task.CompletedTask;

                var commandIdentifier = CommandArguments[0];
                var ticketName = CommandArguments[1];
                var Message = message.Content.Replace($"$setup {ticketName} {CommandArguments[2]} ", "");

                switch(commandIdentifier)
                {
                    case "$setup":
                        if (message.MentionedChannels.Count != 1) return Task.CompletedTask;
                        var mentionnedChannel = message.MentionedChannels.FirstOrDefault();
                        guildManager.SetupMessage(ticketName, Message, guild, mentionnedChannel as SocketTextChannel);
                        break;
                }

                channel.SendMessageAsync($"Ticket {ticketName} Created.");
            }

            else if(message.Content.StartsWith("$roles -add"))
            {
                var mentionnedRoles = message.MentionedRoles;
                if (mentionnedRoles != null && mentionnedRoles.Any()) guildManager.AddModerationCommand(guild, mentionnedRoles.Where(x => !x.IsEveryone).Select(x => x.Id).ToArray());
                channel.SendMessageAsync($"Roles Added.");
            }

            else if (message.Content.StartsWith("$roles -remove"))
            {
                var mentionnedRoles = message.MentionedRoles;
                if (mentionnedRoles != null && mentionnedRoles.Any()) guildManager.RemoveModerationCommand(guild, mentionnedRoles.Where(x => !x.IsEveryone).Select(x => x.Id).ToArray());
                channel.SendMessageAsync($"Roles Removed.");
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
