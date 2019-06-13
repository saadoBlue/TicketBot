using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using TicketBot.Core.Extensions;
using TicketBot.Guild.GuildClasses;
using TicketBot.ORM;

namespace TicketBot
{
    public class GuildManager
    {
        public GuildManager(DiscordSocketClient client)
        {
            DiscordClient = client;
        }

        public void Initialize()
        {
            Orm = new DapperORM();
            guilds = Orm.GetGuildInfos().ToDictionary(x => x.Id);
            NewGuilds = new List<GuildInfo>();
            Task.Factory.StartNewDelayed(15 * 1000, Save);

        }

        public void Save()
        {
            lock (m_lock)
            {
                if (NewGuilds.Any())
                    foreach (var newGuild in NewGuilds)
                    {
                        Orm.Insert(SwitchGuildToDatabase(newGuild));
                    }

                if (guilds.Any())
                    foreach (var guild in guilds)
                    {
                        Orm.Save(SwitchGuildToDatabase(guild.Value));
                    }

                NewGuilds.Clear();
            }
            Console.WriteLine($"{DateTime.Now.ToShortTimeString()} GuildManager: Saved guilds !");
            Task.Factory.StartNewDelayed(60 * 1000, Save);
        }

        #region Propreties

        private DiscordSocketClient DiscordClient;
        private DapperORM Orm;
        private Dictionary<ulong, GuildInfo> guilds;
        private List<GuildInfo> NewGuilds;
        private Emoji TicketEmote => new Emoji("🎫");
        private object m_lock = new object();

        #endregion

        #region Functions

        #region GuildInfo
        public GuildInfo GetOrCreateGuild(SocketGuild guild)
        {
            GuildInfo guildInfo;
            if (guilds.ContainsKey(guild.Id))
            {
                guilds.TryGetValue(guild.Id, out guildInfo);
                return guildInfo;
            }

            guildInfo = new GuildInfo(guild.Id, guild.Name);
            guilds.Add(guild.Id, guildInfo);
            NewGuilds.Add(guildInfo);
            return guildInfo;
        }

        public GuildInfo GetGuildInfo(ulong Id)
        {
            GuildInfo value;
            guilds.TryGetValue(Id, out value);
            return value;
        }
        
        public GuildInfo SwitchGuildToInfo(GuildDatabase database)
        {
            var tickets = FormatterExtensions.ToObject<List<Ticket>>(database.TicketsBin);
            var smessages = FormatterExtensions.ToObject<List<SetupMessage>>(database.SetupMessagesBin);
            GuildInfo info = new GuildInfo(database.Id, database.Name)
            {
                Lang = database.Lang,
                IconUrl = database.IconUrl,
                Tickets = tickets.ToDictionary(x => x.Id),
                SetupMessages = smessages.ToDictionary(x => x.MessageId)
            };
            return info;
        }

        public GuildDatabase SwitchGuildToDatabase(GuildInfo guild)
        {
            var database = new GuildDatabase()
            {
                Id = guild.Id,
                Name = guild.Name,
                Lang = guild.Lang,
                IconUrl = guild.IconUrl,
                TicketsBin = FormatterExtensions.ToBinary(guild.Tickets.Values.ToList()),
                SetupMessagesBin = FormatterExtensions.ToBinary(guild.SetupMessages.Values.ToList())
            };

            return database;
        }

        #endregion

        #region Ticket

        public Ticket CreateTicket(SocketGuild guild, string TicketName)
        {
            GuildInfo guildInfo = GetOrCreateGuild(guild);
            return guildInfo.CreateNewTicket(DiscordClient, TicketName);
        }

        public Ticket GetTicket(ulong GuildId, ulong TicketId)
        {
            GuildInfo guild;
            guilds.TryGetValue(GuildId, out guild);
            if (guild == null) return null;

            Ticket ticket;
            guild.Tickets.TryGetValue(TicketId, out ticket);
            return ticket;
        }

        public Ticket GetTicket(SocketGuild guild, ulong ticketId)
        {
            GuildInfo guildInfo = GetOrCreateGuild(guild);
            return guildInfo.GetTicket(ticketId);
        }

        #endregion

        #region ChildChannel

        public void CreateChannelInstance(SocketGuild guild, SocketGuildUser user, SocketReaction reaction)
        {
            if (user.IsBot) return;

            var Rmessage = (reaction.Channel as SocketTextChannel).GetMessageAsync(reaction.MessageId);
            Rmessage.Wait();

            var messge = Rmessage.Result;
            var message = messge as Discord.Rest.RestUserMessage;

            if (message == null) return;

            GuildInfo guildInfo = GetOrCreateGuild(guild);

            var setupMessage = guildInfo.GetSetupMessage(reaction.MessageId);
            if (setupMessage == null) return;

            var ticket = guildInfo.GetTicket(setupMessage.TicketId);
            if (ticket == null) return;

            message.RemoveReactionAsync(reaction.Emote, user);

            if (ticket.ActiveChildChannels.Values.Any(x => x.UserId == user.Id)) return;

            ticket.CreateNewChild(DiscordClient, user);
        }

        #endregion

        #region SetupMessages

        public void SetupMessage(string TicketName, string Message, SocketGuild guild, SocketTextChannel channel)
        {
            GuildInfo guildInfo = GetOrCreateGuild(guild);
            var ticket = CreateTicket(guild, TicketName);

            EmbedBuilder builder = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder() { Name = $"Ticket Tool ~ {TicketName}", IconUrl = @"https://cdn.discordapp.com/avatars/557628352828014614/04cdd55608f6f9942c9ab3bbcab3932c.png?size=512", Url = @"https://github.com/Saadbg/TicketBot" },
                Description = Message,
                Timestamp = DateTime.Now,
                Footer = new EmbedFooterBuilder() { Text = $"{guildInfo.Name} Support", IconUrl = guildInfo.IconUrl}
            };

            var message = channel.SendMessageAsync("", false, builder.Build());
            message.Wait();

            message.Result.AddReactionAsync(TicketEmote);

            guildInfo.CreateSetupMessage(message.Result.Id, ticket.Id, channel.Id);
        }

        #endregion

        #endregion
    }
}
