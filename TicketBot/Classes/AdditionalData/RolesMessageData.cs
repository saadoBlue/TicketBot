using System;
using System.Collections.Generic;
using System.Text;

namespace TicketBot.Classes.AdditionalData
{
    public class RolesMessageData
    {
        public ulong MessageId
        {
            get;
            set;
        }

        public ulong ChannelId
        {
            get;
            set;
        }

        public ulong[] RolesId
        {
            get;
            set;
        }

        public ulong[] RolesEmotesId
        {
            get;
            set;
        }
    }
}
