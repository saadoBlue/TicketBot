using System;
using System.Collections.Generic;
using System.Text;

namespace TicketBot.Guild.GuildClasses
{
    [Serializable]
    public class SetupMessage
    {
        public SetupMessage(ulong messageId, ulong ticketId, ulong channelId)
        {
            MessageId = messageId;
            TicketId = ticketId;
            ChannelId = channelId;
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

        public ulong ChannelId
        {
            get;
            set;
        }
    }

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

    [Serializable]
    public class LockMessage
    {
        public LockMessage(ulong messageId, ulong ticketId, ulong childId)
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
