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
}
