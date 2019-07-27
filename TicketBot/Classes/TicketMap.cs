using System;
using System.Collections.Generic;
using System.Text;

namespace TicketBot.Maps
{
    [Serializable]
    public class Ticket
    {
        public Ticket(ulong id, ulong guild, string name)
        {
            Id = id;
            ParentGuildId = guild;
            Name = name;
            CategoryId = 0;
            TicketsCreatedNumber = 0;
            ActiveChildChannels = new Dictionary<ulong, TicketChild>();
        }

        public ulong Id
        {
            get;
            set;
        }

        public ulong ParentGuildId
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public ulong CategoryId
        {
            get;
            set;
        }

        public Dictionary<ulong, TicketChild> ActiveChildChannels
        {
            get;
            set;
        }

        public ulong TicketsCreatedNumber
        {
            get;
            set;
        }
    }
}
