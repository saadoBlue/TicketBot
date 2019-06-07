using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace TicketBot.Guild.GuildClasses
{
    public class TicketChildChannel
    {
        public TicketChildChannel(ulong id, ulong parent, ulong userId, ulong ticketNumber)
        {
            Id = id;
            ChannelId = 0;
            ParentTicketId = parent;
            UserId= userId;
            TicketNumber = ticketNumber;
        }

        public string Name(string ticketName) => $"ticket-{ticketName.ToLower()}#{TicketNumber}";

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

        #region Functions

        public SocketTextChannel GetOrCreateGuildChannel(DiscordSocketClient client, Ticket ticket)
        {
            var guild = client.GetGuild(ticket.ParentGuildId);
            if (guild == null)
                return null;

            var channel = guild.GetTextChannel(ChannelId);
            if (channel != null)
                return channel;

            var category = ticket.GetOrCreateCategoryChannel(client);
            if (category == null)
                return null;

            var creation = guild.CreateTextChannelAsync(
                Name(ticket.Name),
                (param => {
                param.CategoryId = ticket.CategoryId;
                param.Topic = State.ToString();
                param.IsNsfw = false;
                param.Name = Name(ticket.Name);
                }
                ));
            creation.Wait();
            ChannelId = creation.Result.Id;

            return guild.GetTextChannel(ChannelId);
        }

        public void Delete(DiscordSocketClient client, Ticket ticket)
        {
            var channel = GetOrCreateGuildChannel(client, ticket);
            if (channel == null)
                return;

            channel.DeleteAsync();
        }

        #endregion
    }

    public enum TicketState
    {
        Open,
        Locked,
        Closed
    }
}
