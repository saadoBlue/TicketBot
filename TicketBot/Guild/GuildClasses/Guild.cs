using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

namespace TicketBot.Guild.GuildClasses
{
    [Serializable]
    public class GuildInfo
    {
        public GuildInfo(ulong id)
        {
            Id = id;
            SetupMessages = new Dictionary<ulong, SetupMessage>();
        }

        public ulong Id
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
            if(SetupMessages.ContainsKey(MessageId))
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
}
