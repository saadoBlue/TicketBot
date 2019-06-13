using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace TicketBot.Guild.GuildClasses
{
    [Serializable]
    public class TicketChildChannel
    {
        public TicketChildChannel(ulong id, ulong parentTicket, ulong parentGuild, ulong userId, ulong ticketNumber)
        {
            Id = id;
            ChannelId = 0;
            MainMessageId = 0;
            LockMessageId = 0;
            ParentTicketId = parentTicket;
            ParentGuildId = parentGuild;
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
            private set;
        }

        private ulong MainMessageId
        {
            get;
            set;
        }
        private ulong LockMessageId
        {
            get;
            set;
        }

        public Ticket Ticket => Program.guildManager.GetTicket(ParentGuildId, ParentTicketId);
        public GuildInfo Guild => Program.guildManager.GetGuildInfo(ParentGuildId);
        Emoji LockEmoji => new Emoji("🔒");
        Emoji UnlockEmoji => new Emoji("🔓");
        Emoji TranscriptEmoji => new Emoji("📑");
        Emoji DeleteEmoji => new Emoji("⛔");

        #region Functions

        public SocketTextChannel GetOrCreateGuildChannel(DiscordSocketClient client, Ticket ticket, SocketGuildUser user)
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

            var ForbidPerms = new OverwritePermissions(PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny);
            var AllowPerms = new OverwritePermissions(PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Allow, PermValue.Allow);
            creation.Result.AddPermissionOverwriteAsync(guild.EveryoneRole, ForbidPerms);
            creation.Result.AddPermissionOverwriteAsync(guild.GetRole(586269577810542593), AllowPerms);// RESTRICTED ADMINS
            creation.Result.AddPermissionOverwriteAsync(guild.GetRole(586270008796119060), AllowPerms);// RESTRICTED ADMINS
            creation.Result.AddPermissionOverwriteAsync(user, AllowPerms); // ADING THE USER

            SendMainMessage(client, creation.Result);

            return client.GetGuild(ticket.ParentGuildId).GetTextChannel(ChannelId);
        }

        public SocketTextChannel GetChannel(DiscordSocketClient client, Ticket ticket)
        {
            var guild = client.GetGuild(ticket.ParentGuildId);
            if (guild == null)
                return null;

            var channel = guild.GetTextChannel(ChannelId);
            if (channel != null)
                return channel;

            return null;
        }

        public void SendMainMessage(DiscordSocketClient client, RestTextChannel channel)
        {
            var message = channel.SendMessageAsync("", false, GetLockEmbed());
            message.Wait();

            message.Result.AddReactionAsync(LockEmoji);

            MainMessageId = message.Result.Id;
        }
        public RestUserMessage GetOrCreateMainMessage(DiscordSocketClient client)
        {
            var guild = client.GetGuild(ParentGuildId);
            if (guild == null)
                return null;

            var channel = guild.GetTextChannel(ChannelId);
            if (channel == null)
                return null;

            var Rmessage = channel.GetMessageAsync(MainMessageId);
            Rmessage.Wait();
            if ((Rmessage.Result as RestUserMessage) != null) return Rmessage.Result as RestUserMessage;

            var message = channel.SendMessageAsync("", false, GetLockEmbed());
            message.Wait();

            message.Result.AddReactionAsync(LockEmoji);

            MainMessageId = message.Result.Id;
            return message.Result;
        }

        public void Delete(DiscordSocketClient client, Ticket ticket)
        {
            var channel = GetChannel(client, ticket);
            if (channel == null)
                return;

            channel.DeleteAsync();
        }

        public void ChangeState(TicketState state)
        {
            State = state;
        }

        public Embed GetLockEmbed()
        {
            EmbedBuilder builder;
            switch(Guild.Lang)
            {
                case LangEnum.Frensh:
                    builder = new EmbedBuilder()
                    {
                        Author = new EmbedAuthorBuilder() { Name = $"Ticket Tool ~ {Ticket.Name}", IconUrl = @"https://cdn.discordapp.com/avatars/557628352828014614/04cdd55608f6f9942c9ab3bbcab3932c.png?size=512", Url = @"https://github.com/Saadbg/TicketBot" },
                        Description = "Merci d'avoir créé un ticket. \n"
                              + "Notre staff va vous répondre au plus bref délai. \n"
                              + "Pour fermer ce ticket, réagissez avec cette emoji: 🔒",
                        Timestamp = DateTime.Now,
                        Footer = new EmbedFooterBuilder() { Text = $"{Guild.Name} Support", IconUrl = Guild.IconUrl }
                    };
                    return builder.Build();
                default:
                    builder = new EmbedBuilder()
                    {
                        Author = new EmbedAuthorBuilder() { Name = $"Ticket Tool ~ {Ticket.Name}", IconUrl = @"https://cdn.discordapp.com/avatars/557628352828014614/04cdd55608f6f9942c9ab3bbcab3932c.png?size=512", Url = @"https://github.com/Saadbg/TicketBot" },
                        Description = "Thanks for creating a ticket. \n"
                              + "Our staff will reply you as soon as possible. \n"
                              + "To close the ticket, react with this emoji: 🔒",
                        Timestamp = DateTime.Now,
                        Footer = new EmbedFooterBuilder() { Text = $"{Guild.Name} Support", IconUrl = Guild.IconUrl }
                    };
                    return builder.Build();
            }
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
