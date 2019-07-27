using System;
using System.Collections.Generic;
using System.Text;

namespace TicketBot.Maps.Messages
{
    [Serializable]
    public class MainMessage
    {
        public MainMessage(ulong messageId, ulong ticketId, ulong childId)
        {
            MessageId = messageId;
            TicketId = ticketId;
            ChildId = childId;
        }

        public ulong MessageId
        {
            get;
            set;
        }

        public ulong TicketId
        {
            get;
            set;
        }

        public ulong ChildId
        {
            get;
            set;
        }
    }
}
