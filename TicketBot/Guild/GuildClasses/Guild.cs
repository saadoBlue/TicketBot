using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TicketBot.Core.Extensions;
using TicketBot.Guild;
using TicketBot.Guild.GuildClasses;

namespace TicketBot.Guild.GuildClasses
{
    public class GuildInfo
    {
        public GuildInfo(ulong id, string name)
        {
            Id = id;
            Name = name;
            IconUrl = @"https://cdn.discordapp.com/avatars/557628352828014614/04cdd55608f6f9942c9ab3bbcab3932c.png?size=512";
            Lang = LangEnum.English;
            SetupMessages = new Dictionary<ulong, SetupMessage>();
            PermittedRoles = new List<ulong>() { 666 };
        }

        public ulong Id
        {
            get;
            set;
        }
        public string Name
        {
            get;
            set;
        }

        public LangEnum Lang
        {
            get;
            set;
        }

        public string IconUrl
        {
            get;
            set;
        }
        public Dictionary<ulong, Ticket> Tickets
        {
            get;
            set;
        }

        public Dictionary<ulong, SetupMessage> SetupMessages
        {
            get;
            set;
        }

        public List<ulong> PermittedRoles
        {
            get;
            set;
        }

        #region Messages Functions

        public SetupMessage CreateSetupMessage(ulong MessageId, ulong TicketId, ulong ChannelId)
        {
            if (SetupMessages.ContainsKey(MessageId))
            {
                SetupMessage m_message;
                SetupMessages.TryGetValue(MessageId, out m_message);
                return m_message;
            }
            SetupMessage message = new SetupMessage(MessageId, TicketId, ChannelId);
            SetupMessages.Add(MessageId, message);
            return message;
        }

        public SetupMessage GetSetupMessage(ulong MessageId)
        {
            if (SetupMessages.ContainsKey(MessageId))
            {
                SetupMessage m_message;
                SetupMessages.TryGetValue(MessageId, out m_message);
                return m_message;
            }
            else return null;
        }

        public SetupMessage GetSetupMessageByTicket(ulong TicketId)
        {
            return SetupMessages.Values.FirstOrDefault(x => x.TicketId == TicketId);
        }

        public TicketChildChannel GetChildByReactionMessageId(ulong messageId)
        {
            var ticket = Tickets.SelectMany(x => x.Value.ActiveChildChannels.Values).FirstOrDefault(x => messageId == x.LockMessageId || messageId == x.MainMessageId);
            return ticket;
        }

        #endregion

        #region Ticket Functions

        public Ticket CreateNewTicket(DiscordSocketClient client, string Name)
        {
            var ticketId = PopId();
            Ticket ticket = new Ticket(ticketId, Id, Name);
            ticket.GetOrCreateCategoryChannel(client);
            Tickets.Add(ticketId, ticket);
            return ticket;
        }

        public Ticket GetTicket(ulong Id)
        {
            Ticket ticket;
            Tickets.TryGetValue(Id, out ticket);
            return ticket;
        }

        public bool RemoveTicket(DiscordSocketClient client, ulong TicketId, bool clear)
        {
            if (!Tickets.ContainsKey(TicketId))
                return false;

            var ticket = GetTicket(TicketId);
            return RemoveTicket(client, ticket, clear);
        }
        public bool RemoveTicket(DiscordSocketClient client, Ticket ticket, bool clear = false)
        {
            ticket.Delete(client);

            var setmessage = GetSetupMessage(ticket.Id);
            if (setmessage != null) SetupMessages.Remove(setmessage.MessageId);

            if (clear) return true;

            if (!Tickets.ContainsKey(ticket.Id))
                return false;

            Tickets.Remove(ticket.Id);

            return true;
        }

        public void Reset(DiscordSocketClient client)
        {
            foreach(var tickt in Tickets.Values)
            {
                RemoveTicket(client, tickt, true);
            }
            Tickets.Clear();
        }
        public ulong PopId()
        {
            ulong newId = 0;

            if (Tickets == null)
                Tickets = new Dictionary<ulong, Ticket>();

            while (Tickets.ContainsKey(newId))
                newId++;

            return newId;
        }

        #endregion
    }

    public class GuildDatabase
    {
        public ulong Id
        {
            get;
            set;
        }
        public string Name
        {
            get;
            set;
        }

        public LangEnum Lang
        {
            get;
            set;
        }

        public string IconUrl
        {
            get;
            set;
        }
        public string PermittedRolesCSV
        {
            get;
            set;
        }

        public byte[] TicketsBin
        {
            get;
            set;
        }

        public byte[] SetupMessagesBin
        {
            get;
            set;
        }
    }
}

