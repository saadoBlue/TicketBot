using TicketBot.Core.Enums;

namespace TicketBot.Maps
{
    public class GuildMap
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
        public string PermittedRolesCSV
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

        public byte[] RolesMessagesBin
        {
            get;
            set;
        }
    }
}
