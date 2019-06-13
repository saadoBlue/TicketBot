using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using TicketBot.Core.Extensions;

namespace TicketBot.Guild.GuildClasses
{
    [Serializable]
    public class TicketChildChannel
    {
        public TicketChildChannel(ulong id, ulong parentTicket, ulong parentGuild, ulong userId, ulong ticketNumber)
        {
            Id = id;
            ChannelId = 666;
            MainMessageId = 666;
            LockMessageId = 666;
            ParentTicketId = parentTicket;
            ParentGuildId = parentGuild;
            UserId= userId;
            TicketNumber = ticketNumber;
        }

        public string Name(string ticketName) => $"{ticketName.ToLower()}-{TicketNumber.ToString("D4")}";

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

        public ulong MainMessageId
        {
            get;
            private set;
        }
        public ulong LockMessageId
        {
            get;
            private set;
        }

        public Ticket Ticket => Program.guildManager.GetTicket(ParentGuildId, ParentTicketId);
        public GuildInfo Guild => Program.guildManager.GetGuildInfo(ParentGuildId);
        Emoji LockEmoji => new Emoji("🔒");
        Emoji UnlockEmoji => new Emoji("🔓");
        Emoji TranscriptEmoji => new Emoji("📑");
        Emoji DeleteEmoji => new Emoji("⛔");
        OverwritePermissions SpectatePerms => new OverwritePermissions(PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Allow, PermValue.Deny);
        OverwritePermissions AllowPerms => new OverwritePermissions(PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Allow, PermValue.Allow);
        OverwritePermissions ForbidPerms => new OverwritePermissions(PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny);

        #region Functions

        #region Channel
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

            creation.Result.AddPermissionOverwriteAsync(guild.EveryoneRole, ForbidPerms);
            foreach(var PermittedId in Guild.PermittedRoles)
            {
                creation.Result.AddPermissionOverwriteAsync(guild.GetRole(PermittedId), AllowPerms);// RESTRICTED ADMINS
            }

            SendMainMessage(client, creation.Result);

            return client.GetGuild(ticket.ParentGuildId).GetTextChannel(ChannelId);
        }

        public SocketTextChannel GetChannel(DiscordSocketClient client)
        {
            var guild = client.GetGuild(ParentGuildId);
            if (guild == null)
                return null;

            var channel = guild.GetTextChannel(ChannelId);
            if (channel != null)
                return channel;

            return null;
        }

        #endregion

        #region Main Message

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
        public Embed GetLockEmbed()
        {
            EmbedBuilder builder;
            switch (Guild.Lang)
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

        #region LockMessage

        public RestUserMessage GetOrCreateLockMessage(DiscordSocketClient client, SocketGuildUser m_user)
        {
            var guild = client.GetGuild(ParentGuildId);
            if (guild == null)
                return null;

            var channel = guild.GetTextChannel(ChannelId);
            if (channel == null)
                return null;

            var Rmessage = channel.GetMessageAsync(LockMessageId);
            Rmessage.Wait();
            if ((Rmessage.Result as RestUserMessage) != null) return Rmessage.Result as RestUserMessage;

            if (m_user != null) SendLockedMessage(client, m_user, channel);

            var message = channel.SendMessageAsync("", false, GetLockMessageEmbed());
            message.Wait();

            message.Result.AddReactionAsync(UnlockEmoji);
            message.Result.AddReactionAsync(TranscriptEmoji);
            message.Result.AddReactionAsync(DeleteEmoji);

            var user = guild.GetUser(UserId);
            channel.AddPermissionOverwriteAsync(user, SpectatePerms);

            
            
            LockMessageId = message.Result.Id;
            return message.Result;
        }

        public Embed GetLockMessageEmbed()
        {
            EmbedBuilder builder;
            switch (Guild.Lang)
            {
                case LangEnum.Frensh:
                    builder = new EmbedBuilder()
                    {
                        Author = new EmbedAuthorBuilder() { Name = $"Ticket Tool ~ Mod tools", IconUrl = @"https://cdn.discordapp.com/avatars/557628352828014614/04cdd55608f6f9942c9ab3bbcab3932c.png?size=512", Url = @"https://github.com/Saadbg/TicketBot" },
                        Description = "🔓 Réouvrir le ticket. \n"
                              + "📑 Sauvegarder le transcript et supprimer le ticket. \n"
                              + "⛔ Supprimer le ticket.",
                        Timestamp = DateTime.Now,
                        Footer = new EmbedFooterBuilder() { Text = $"{Guild.Name} Support", IconUrl = Guild.IconUrl }
                    };
                    return builder.Build();
                default:
                    builder = new EmbedBuilder()
                    {
                        Author = new EmbedAuthorBuilder() { Name = $"Ticket Tool ~ {Ticket.Name}", IconUrl = @"https://cdn.discordapp.com/avatars/557628352828014614/04cdd55608f6f9942c9ab3bbcab3932c.png?size=512", Url = @"https://github.com/Saadbg/TicketBot" },
                        Description = "🔓 Unlock the ticket. \n"
                              + "📑 Save the transcript and delete the ticket. \n"
                              + "⛔ Delete the ticket.",
                        Timestamp = DateTime.Now,
                        Footer = new EmbedFooterBuilder() { Text = $"{Guild.Name} Support", IconUrl = Guild.IconUrl }
                    };
                    return builder.Build();
            }
        }

        #endregion

        #region SimpleMessages

        public void SendLockedMessage(DiscordSocketClient client, SocketGuildUser user, SocketTextChannel channel)
        {
            var builder = new EmbedBuilder()
            {
                Color = Color.LightOrange,
                Description = Guild.Lang == LangEnum.Frensh ? $"Ticket verouillé par {user.Mention}" : $"Ticket locked by {user.Mention}"
            };
            channel.SendMessageAsync("", false, builder.Build());
        }
        public void SendTranscriptMessage(DiscordSocketClient client, SocketTextChannel channel)
        {
            var builder = new EmbedBuilder()
            {
                Color = Color.Gold,
                Description = Guild.Lang == LangEnum.Frensh ? $"Transcript enregistré." : $"Transcript saved."
            };
            channel.SendMessageAsync("", false, builder.Build());
        }

        public void SendClosedMessage(DiscordSocketClient client, SocketTextChannel channel)
        {
            var builder = new EmbedBuilder()
            {
                Color = Color.DarkRed,
                Description = Guild.Lang == LangEnum.Frensh ? $"Le ticket va se fermer dans 5 secondes..." : $"The ticket will be deleted in 5 seconds..."
            };
            channel.SendMessageAsync("", false, builder.Build());
        }

        #endregion

        #region Modifications

        public void Delete(DiscordSocketClient client, bool Intern = true)
        {
            if (Ticket.ActiveChildChannels.ContainsKey(Id) && Intern) Ticket.ActiveChildChannels.Remove(Id);

            var channel = GetChannel(client);
            if (channel == null)
                return;

            SendClosedMessage(client, channel);
            Task.Factory.StartNewDelayed(7000, () => ScheduledDelete(channel));
        }

        public void DeleteWithTranscript(DiscordSocketClient client)
        {
            if (Ticket.ActiveChildChannels.ContainsKey(Id)) Ticket.ActiveChildChannels.Remove(Id);

            var channel = GetChannel(client);
            if (channel == null)
                return;

            //Transcript Code using https://github.com/Tyrrrz/DiscordChatExporter Libs


            SendTranscriptMessage(client, channel);
            SendClosedMessage(client, channel);
            Task.Factory.StartNewDelayed(7000, () => ScheduledDelete(channel));
        }

        private void ScheduledDelete(SocketTextChannel channel) => channel.DeleteAsync();

        public void ChangeState(TicketState state, DiscordSocketClient client, SocketGuildUser user)
        {
            State = state;
            switch (state)
            {
                case TicketState.Locked:
                    Lock(client, user);
                    break;
                case TicketState.Open:
                    ReOpen(client);
                    break;
                default:
                    Delete(client);
                    break;
            }
        }

        public void ReOpen(DiscordSocketClient client)
        {
            GetOrCreateLockMessage(client, null).DeleteAsync();
            LockMessageId = 666;
            var message = GetOrCreateMainMessage(client);

            var guild = client.GetGuild(ParentGuildId);
            if (guild == null)
                return;

            message.RemoveAllReactionsAsync();
            message.AddReactionAsync(UnlockEmoji);
            var user = guild.GetUser(UserId);
            (message.Channel as SocketTextChannel).AddPermissionOverwriteAsync(user, AllowPerms);
        }

        public void Lock(DiscordSocketClient client, SocketGuildUser user)
        {
            var message = GetOrCreateMainMessage(client);
            message.RemoveAllReactionsAsync();
            message.AddReactionAsync(UnlockEmoji);
            GetOrCreateLockMessage(client, user);
        }

        #endregion

        #endregion
    }

    public enum TicketState
    {
        Open,
        Locked,
        Closed
    }
}
