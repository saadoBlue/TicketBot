using System;
using System.Collections.Generic;
using System.Text;
using TicketBot.Core.Enums;

namespace TicketBot.Maps
{
    [Serializable]
    public class TicketChild
    {
        public TicketChild(ulong id, ulong parentTicket, ulong parentGuild, ulong userId, ulong ticketNumber)
        {
            Id = id;
            ChannelId = 666;
            MainMessageId = 666;
            LockMessageId = 666;
            ParentTicketId = parentTicket;
            ParentGuildId = parentGuild;
            UserId = userId;
            TicketNumber = ticketNumber;
        }

        public ulong Id
        {
            get;
            set;
        }

        public ulong ChannelId
        {
            get;
            set;
        }

        public ulong ParentTicketId
        {
            get;
            set;
        }
        public ulong ParentGuildId
        {
            get;
            set;
        }

        public ulong UserId
        {
            get;
            set;
        }

        public ulong TicketNumber
        {
            get;
            set;
        }

        public TicketState State
        {
            get;
            set;
        }

        public ulong MainMessageId
        {
            get;
            set;
        }
        public ulong LockMessageId
        {
            get;
            set;
        }
    }
}
