using System.Collections.Generic;
using TicketBot.Classes.AdditionalData;
using TicketBot.Core.Enums;
using TicketBot.Maps;
using TicketBot.Maps.Messages;

namespace TicketBot.Guild
{
    public class GuildEngine
    {
        public GuildEngine(ulong id, string name)
        {
            Id = id;
            Name = name;
            IconUrl = @"https://cdn.discordapp.com/avatars/557628352828014614/04cdd55608f6f9942c9ab3bbcab3932c.png?size=512";
            Lang = LangEnum.English;
            SetupMessages = new Dictionary<ulong, SetupMessage>();
            RolesMessagesData = new Dictionary<ulong, RolesMessageData>();
            PermittedRoles = new List<ulong>() { 666 };
        }

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

        public Dictionary<ulong, RolesMessageData> RolesMessagesData
        {
            get;
            set;
        }

        public List<ulong> PermittedRoles
        {
            get;
            set;
        }
    }
}

