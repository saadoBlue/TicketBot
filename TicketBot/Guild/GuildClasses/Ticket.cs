using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace TicketBot.Guild.GuildClasses
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
            ActiveChildChannels = new Dictionary<ulong, TicketChildChannel>();
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

        public Dictionary<ulong, TicketChildChannel> ActiveChildChannels
        {
            get;
            set;
        }

        public ulong TicketsCreatedNumber
        {
            get;
            set;
        }

        #region Child Functions

        public TicketChildChannel CreateNewChild(DiscordSocketClient client, SocketGuildUser user)
        {
            TicketsCreatedNumber++;
            var ChildId = PopId();
            TicketChildChannel child = new TicketChildChannel(ChildId, Id, ParentGuildId, user.Id, TicketsCreatedNumber);
            ActiveChildChannels.Add(ChildId, child);
            child.GetOrCreateGuildChannel(client, this, user);
            return child;
        }

        public bool RemoveChild(DiscordSocketClient client, ulong ChildId)
        {
            if (!ActiveChildChannels.ContainsKey(ChildId))
                return false;

            TicketChildChannel child;
            ActiveChildChannels.TryGetValue(ChildId, out child);

            return RemoveChild(client, child);
        }
        public bool RemoveChild(DiscordSocketClient client, TicketChildChannel Child)
        {
            var ChildId = Child.Id;
            if (!ActiveChildChannels.ContainsKey(ChildId))
                return false;

            Child.Delete(client, this);
            ActiveChildChannels.Remove(ChildId);
            return true;
        }

        public ulong PopId()
        {
            ulong newId = 0;

            if (ActiveChildChannels == null) ActiveChildChannels = new Dictionary<ulong, TicketChildChannel>();

                while (ActiveChildChannels.ContainsKey(newId))
                newId++;

            return newId;
        }

        #endregion

        #region Category Functions

        public SocketCategoryChannel GetOrCreateCategoryChannel(DiscordSocketClient client)
        {
            var guild = client.GetGuild(ParentGuildId);
            if (guild == null)
                return null;

            var category = guild.GetCategoryChannel(CategoryId);
            if (category != null)
                return category;

            var creation = guild.CreateCategoryChannelAsync(Name, (param => { param.Position = guild.CategoryChannels.Count; }));
            creation.Wait();
            CategoryId = creation.Result.Id;

            return guild.GetCategoryChannel(CategoryId);
        }

        public void Delete(DiscordSocketClient client)
        {
            var category = GetOrCreateCategoryChannel(client);
            if (category == null)
                return;

            ActiveChildChannels?.Values.ToList().ForEach(x => x.Delete(client, this));
            
            category.DeleteAsync();
        }

        #endregion
    }
}
