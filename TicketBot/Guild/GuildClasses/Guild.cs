using System;
using System.Collections.Generic;
using System.Text;

namespace TicketBot.Guild.GuildClasses
{
    public class Guild
    {
        public Guild(ulong id)
        {
            Id = id;
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

        public ulong TicketsNumber
        {
            get;
            set;
        }

        #region Ticket Functions

        public Ticket CreateNewTicket(string Name)
        {
            TicketsNumber++;
            var ticketId = PopId();
            Ticket ticket = new Ticket(ticketId, Id, Name, TicketsNumber);
            Tickets.Add(ticketId, ticket);
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
