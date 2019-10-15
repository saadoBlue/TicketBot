using System;
using System.Collections.Generic;
using System.Text;

namespace TicketBot.Maps.Messages
{
    [Serializable]
    public class SetupMessage
    {

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
