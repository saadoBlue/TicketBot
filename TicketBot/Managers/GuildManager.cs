using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TicketBot.Classes.AdditionalData;
using TicketBot.Core.Enums;
using TicketBot.Core.Extensions;
using TicketBot.Guild;
using TicketBot.Maps;
using TicketBot.Maps.Messages;

namespace TicketBot.Managers
{
    public class GuildManager
    {
        #region Modifications

        public static void LangChangeCommand(SocketGuild guild, LangEnum lang)
        {
            var guildInfo = GetOrCreateGuild(guild);
            guildInfo.Lang = lang;
        }

        public static void NameChangeCommand(SocketGuild guild, string name)
        {
            var guildInfo = GetOrCreateGuild(guild);
            guildInfo.Name = name;
        }

        public static void IconChangeCommand(SocketGuild guild, string icon)
        {
            var guildInfo = GetOrCreateGuild(guild);
            guildInfo.IconUrl = icon;
        }

        public static void AddModerationCommand(SocketGuild guild, ulong[] Roles)
        {
            var guildInfo = GetOrCreateGuild(guild);
            foreach (var role in Roles)
            {
                if (!guildInfo.PermittedRoles.Contains(role)) guildInfo.PermittedRoles.Add(role);
            }
        }

        public static void RemoveModerationCommand(SocketGuild guild, ulong[] Roles)
        {
            var guildInfo = GetOrCreateGuild(guild);
            foreach (var role in Roles)
            {
                if (guildInfo.PermittedRoles.Contains(role)) guildInfo.PermittedRoles.Remove(role);
            }
        }

        #endregion

        #region Management

        public static GuildEngine GetOrCreateGuild(SocketGuild guild) => Program.mainManager.GetOrCreateGuild(guild);

        public static GuildEngine GetGuildEngine(ulong Id) => Program.mainManager.GetGuild(Id);

        public static GuildEngine UnMapGuild(GuildMap database)
        {
            var tickets = FormatterExtensions.ToObject<List<Ticket>>(database.TicketsBin);
            var smessages = FormatterExtensions.ToObject<List<SetupMessage>>(database.SetupMessagesBin);
            var sroles = FormatterExtensions.ToObject<List<RolesMessageData>>(database.RolesMessagesBin);
            GuildEngine info = new GuildEngine(database.Id, database.Name)
            {
                Lang = database.Lang,
                IconUrl = database.IconUrl,
                Tickets = tickets.ToDictionary(x => x.Id),
                SetupMessages = smessages.ToDictionary(x => x.MessageId),
                RolesMessagesData = sroles.ToDictionary(x => x.MessageId),
                PermittedRoles = FormatterExtensions.FromCSV<ulong>(database.PermittedRolesCSV, ";").ToList()
            };
            return info;
        }

        public static GuildMap MapGuild(GuildEngine guild)
        {
            var map = new GuildMap()
            {
                Id = guild.Id,
                Name = guild.Name,
                Lang = guild.Lang,
                IconUrl = guild.IconUrl,
                TicketsBin = FormatterExtensions.ToBinary(guild.Tickets.Values.ToList()),
                SetupMessagesBin = FormatterExtensions.ToBinary(guild.SetupMessages.Values.ToList()),
                RolesMessagesBin = FormatterExtensions.ToBinary(guild.RolesMessagesData.Values.ToList()),
                PermittedRolesCSV = guild.PermittedRoles.ToCSV(";")
            };

            return map;
        }

        #endregion

        #region SetupMessages

        public static SetupMessage CreateSetupMessage(GuildEngine guild, ulong messageId, ulong ticketId, ulong channelId)
        {
            if (guild.SetupMessages.ContainsKey(messageId))
            {
                SetupMessage m_message;
                guild.SetupMessages.TryGetValue(messageId, out m_message);
                return m_message;
            }
            SetupMessage message = new SetupMessage { MessageId = messageId, TicketId = ticketId, ChannelId = channelId};
            guild.SetupMessages.Add(messageId, message);
            return message;
        }

        public static SetupMessage GetSetupMessage(GuildEngine guild, ulong MessageId)
        {
            if (guild.SetupMessages.ContainsKey(MessageId))
            {
                SetupMessage m_message;
                guild.SetupMessages.TryGetValue(MessageId, out m_message);
                return m_message;
            }
            else return null;
        }

        public static SetupMessage GetSetupMessageByTicket(GuildEngine guild, ulong TicketId)
        {
            return guild.SetupMessages.Values.FirstOrDefault(x => x.TicketId == TicketId);
        }

        #endregion

        #region Ticket Functions

        public static Ticket CreateNewTicket(DiscordSocketClient client, GuildEngine guild, string Name)
        {
            var ticketId = PopId(guild);
            Ticket ticket = new Ticket(ticketId, guild.Id, Name);
            TicketManager.GetOrCreateCategoryChannel(client, ticket);
            guild.Tickets.Add(ticketId, ticket);
            return ticket;
        }

        public static Ticket GetTicket(GuildEngine guild, ulong Id)
        {
            Ticket ticket;
            guild.Tickets.TryGetValue(Id, out ticket);
            return ticket;
        }

        public static void RemoveTicket(DiscordSocketClient client, GuildEngine guild, ulong TicketId, bool clear)
        {
            if (guild.Tickets.ContainsKey(TicketId))
            {
                var ticket = GetTicket(guild, TicketId);
                RemoveTicket(client, guild, ticket, clear);
            }
        }
        public static void RemoveTicket(DiscordSocketClient client, GuildEngine guild, Ticket ticket, bool clear = false)
        {
            TicketManager.DeleteTicket(client, ticket);

            var setmessage = GetSetupMessageByTicket(guild, ticket.Id);
            if (setmessage != null) guild.SetupMessages.Remove(setmessage.MessageId);

            if (clear) return;

            if (!guild.Tickets.ContainsKey(ticket.Id))
                return;

            guild.Tickets.Remove(ticket.Id);
        }

        public static void ResetTickets(DiscordSocketClient client, GuildEngine guild)
        {
            foreach (var tickt in guild.Tickets.Values)
            {
                RemoveTicket(client, guild, tickt, true);
            }

            guild.Tickets.Clear();
        }

        public static TicketChild GetChildByReactionMessageId(GuildEngine guild, ulong messageId)
        {
            var ticket = guild.Tickets.SelectMany(x => x.Value.ActiveChildChannels.Values).FirstOrDefault(x => messageId == x.LockMessageId || messageId == x.MainMessageId);
            return ticket;
        }

        public static ulong PopId(GuildEngine guild)
        {
            ulong newId = 0;

            if (guild.Tickets == null)
                guild.Tickets = new Dictionary<ulong, Ticket>();

            while (guild.Tickets.ContainsKey(newId))
                newId++;

            return newId;
        }

        #endregion
    }
}
