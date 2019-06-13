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
            Name = Name;
            IconUrl = @"https://cdn.discordapp.com/avatars/557628352828014614/04cdd55608f6f9942c9ab3bbcab3932c.png?size=512";
            Lang = LangEnum.English;
            SetupMessages = new Dictionary<ulong, SetupMessage>();
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

        #region Ticket Functions
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

        public bool RemoveTicket(ulong TicketId)
        {
            if (!Tickets.ContainsKey(TicketId))
                return false;

            Tickets.Remove(TicketId);
            return true;
        }
        public bool RemoveTicket(Ticket Ticket)
        {
            return RemoveTicket(Ticket.Id);
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

