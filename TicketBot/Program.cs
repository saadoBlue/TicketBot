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

            if ((message.Author as SocketGuildUser).Roles.All(x => !x.Permissions.Administrator) && message.Author.Id != guild.Owner.Id)
                return Task.CompletedTask;

            if (message.Content.StartsWith("$help"))
            {
                var helpEmbedBuilder = new EmbedBuilder()
                {
                    Author = new EmbedAuthorBuilder() { Name = $"Ticket Tool ~ Commands", IconUrl = @"https://cdn.discordapp.com/avatars/557628352828014614/04cdd55608f6f9942c9ab3bbcab3932c.png?size=512", Url = @"https://github.com/Saadbg/TicketBot" },
                    Timestamp = DateTime.Now,
                    Footer = new EmbedFooterBuilder() { Text = "Commands tab" },
                    Description =
                      "**Prefix** : **$** \n \n"
                    + "**setup** : Creates a ticket. \n"
                    + "     ~exemple: \"$setup ***TicketName***  #channel ***A message displayed***  \" \n \n"
                    + "**roles** : manages mentionned roles permissions \n"
                    + "    -**add** : adds roles to tickets staff. \n"
                    + "    -**remove** : removes roles from tickets staff. \n"
                    + "     ~exemple: \"$roles ***add/remove*** ***@Role1  @Role2  @Role3*** \" \n \n"
                    + "**lang** : manages the display language \n"
                    + "    -**fr/french** : changes the language to French. \n"
                    + "    -**en/english** : changes the language to English. \n"
                    + "     ~exemple: \"$lang ***en/fr*** \" \n \n"
                    + "**name** : Changes the ticket support team name \n"
                    + "     ~exemple: \"$name ***New name*** \" \n \n"
                    + "**icon** : Changes the ticket support team icon \n"
                    + "     ~exemple: \"$icon ***http://newIconUrl.com/Icon.jpg*** \" \n \n"
                };
                channel.SendMessageAsync("", false, helpEmbedBuilder.Build());
            }

            else if (message.Content.StartsWith("$setup "))
            {
                var CommandArguments = message.Content.Split(' ');

                if (CommandArguments.Length < 3) return Task.CompletedTask;

                var commandIdentifier = CommandArguments[0];
                var ticketName = CommandArguments[1];
                var Message = message.Content.Replace($"$setup {ticketName} {CommandArguments[2]} ", "");

                if (message.MentionedChannels.Count != 1)
                    return Task.CompletedTask;

                var mentionnedChannel = message.MentionedChannels.FirstOrDefault();
                guildManager.SetupMessage(ticketName, Message, guild, mentionnedChannel as SocketTextChannel);

                channel.SendMessageAsync($"Ticket {ticketName} Created.");
            }

            else if(message.Content.ToLower().StartsWith("$roles add "))
            {
                var mentionnedRoles = message.MentionedRoles;
                if (mentionnedRoles != null && mentionnedRoles.Any()) guildManager.AddModerationCommand(guild, mentionnedRoles.Where(x => !x.IsEveryone).Select(x => x.Id).ToArray());
                channel.SendMessageAsync($"Roles Added.");
            }

            else if (message.Content.ToLower().StartsWith("$roles remove "))
            {
                var mentionnedRoles = message.MentionedRoles;
                if (mentionnedRoles != null && mentionnedRoles.Any()) guildManager.RemoveModerationCommand(guild, mentionnedRoles.Where(x => !x.IsEveryone).Select(x => x.Id).ToArray());
                channel.SendMessageAsync($"Roles Removed.");
            }

            else if (message.Content.ToLower().StartsWith("$lang fr") || message.Content.ToLower().StartsWith("$lang french"))
            {
                guildManager.LangChangeCommand(guild, Guild.LangEnum.Frensh);
                channel.SendMessageAsync($"Langage changé en Français.");
            }

            else if (message.Content.ToLower().StartsWith("$lang en") || message.Content.ToLower().StartsWith("$lang english"))
            {
                guildManager.LangChangeCommand(guild, Guild.LangEnum.English);
                channel.SendMessageAsync($"Language changed to English.");
            }

            else if(message.Content.StartsWith("$icon "))
            {
                string IconUrl = message.Content.Replace("$icon ", "");
                guildManager.IconChangeCommand(guild, IconUrl);
                channel.SendMessageAsync($"Icon changed to {IconUrl}.");
            }

            else if (message.Content.StartsWith("$name "))
            {
                string name = message.Content.Replace("$name ", "");
                guildManager.NameChangeCommand(guild, name);
                channel.SendMessageAsync($"Name changed to {name}.");
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
