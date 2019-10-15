using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketBot.Core.Enums;
using TicketBot.Core.Extensions;
using TicketBot.Guild;
using TicketBot.Maps;

namespace TicketBot.Managers
{
    public class TicketManager
    {
        #region Child Functions

        public static Ticket GetTicket(ulong ParentGuildId, ulong ParentTicketId) => GuildManager.GetTicket(GetGuild(ParentGuildId), ParentTicketId);

        public static string GetChildName(string ticketName, ulong TicketNumber) => $"{ticketName.ToLower()}-{TicketNumber.ToString("D4")}";
        public static GuildEngine GetGuild(ulong ParentGuildId) => GuildManager.GetGuildEngine(ParentGuildId);
        public static Emoji LockEmoji => new Emoji("🔒");
        public static Emoji UnlockEmoji => new Emoji("🔓");
        public static Emoji TranscriptEmoji => new Emoji("📑");
        public static Emoji DeleteEmoji => new Emoji("⛔");
        public static OverwritePermissions SpectatePerms => new OverwritePermissions(PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Allow, PermValue.Deny);
        public static OverwritePermissions AllowPerms => new OverwritePermissions(PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Allow, PermValue.Allow);
        public static OverwritePermissions ForbidPerms => new OverwritePermissions(PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny);

        public static TicketChild CreateNewChild(DiscordSocketClient client, SocketGuildUser user, Ticket ticket)
        {
            ticket.TicketsCreatedNumber++;
            var ChildId = PopId(ticket);
            TicketChild child = new TicketChild(ChildId, ticket.Id, ticket.ParentGuildId, user.Id, ticket.TicketsCreatedNumber);
            ticket.ActiveChildChannels.Add(ChildId, child);
            GetOrCreateGuildChannel(client, ticket, user, child);
            return child;
        }

        public static bool RemoveChild(DiscordSocketClient client, Ticket ticket, ulong ChildId)
        {
            if (!ticket.ActiveChildChannels.ContainsKey(ChildId))
                return false;

            TicketChild child;
            ticket.ActiveChildChannels.TryGetValue(ChildId, out child);

            return RemoveChild(client, ticket, child);
        }
        public static bool RemoveChild(DiscordSocketClient client, Ticket ticket, TicketChild Child)
        {
            DeleteChild(client, Child, false);

            var ChildId = Child.Id;
            if (!ticket.ActiveChildChannels.ContainsKey(ChildId))
                return false;


            ticket.ActiveChildChannels.Remove(ChildId);
            return true;
        }

        public static ulong PopId(Ticket ticket)
        {
            ulong newId = 0;

            if (ticket.ActiveChildChannels == null) ticket.ActiveChildChannels = new Dictionary<ulong, TicketChild>();

            while (ticket.ActiveChildChannels.ContainsKey(newId))
                newId++;

            return newId;
        }


        #region Channel
        public  static SocketTextChannel GetOrCreateGuildChannel(DiscordSocketClient client, Ticket ticket, SocketGuildUser user, TicketChild child)
        {
            var guild = client.GetGuild(ticket.ParentGuildId);
            if (guild == null)
                return null;

            var channel = guild.GetTextChannel(child.ChannelId);
            if (channel != null)
                return channel;

            var category = GetOrCreateCategoryChannel(client, ticket);
            if (category == null)
                return null;

            var creation = guild.CreateTextChannelAsync(
                GetChildName(ticket.Name, child.TicketNumber),
                (param => {
                    param.CategoryId = ticket.CategoryId;
                    param.Topic = child.State.ToString();
                    param.IsNsfw = false;
                    param.Name = GetChildName(ticket.Name, child.TicketNumber);
                }
                ));
            creation.Wait();
            child.ChannelId = creation.Result.Id;

            creation.Result.AddPermissionOverwriteAsync(guild.EveryoneRole, ForbidPerms);
            foreach (var PermittedId in GetGuild(child.ParentGuildId).PermittedRoles)
            {
                creation.Result.AddPermissionOverwriteAsync(guild.GetRole(PermittedId), AllowPerms);// RESTRICTED ADMINS
            }

            SendMainMessage(client, creation.Result, user, child);

            return client.GetGuild(ticket.ParentGuildId).GetTextChannel(child.ChannelId);
        }

        public static SocketTextChannel GetChannel(DiscordSocketClient client, TicketChild child)
        {
            var guild = client.GetGuild(child.ParentGuildId);
            if (guild == null)
                return null;

            var channel = guild.GetTextChannel(child.ChannelId);
            if (channel != null)
                return channel;

            return null;
        }

        #endregion

        #region Main Message

        public static void SendMainMessage(DiscordSocketClient client, RestTextChannel channel, SocketUser user, TicketChild child)
        {
            var message = channel.SendMessageAsync("", false, GetLockEmbed(child, user.Mention));
            message.Wait();

            message.Result.AddReactionAsync(LockEmoji);

            child.MainMessageId = message.Result.Id;
        }
        public static RestUserMessage GetOrCreateMainMessage(DiscordSocketClient client, TicketChild child)
        {
            var guild = client.GetGuild(child.ParentGuildId);
            if (guild == null)
                return null;

            var channel = guild.GetTextChannel(child.ChannelId);
            if (channel == null)
                return null;

            var Rmessage = channel.GetMessageAsync(child.MainMessageId);
            Rmessage.Wait();
            if ((Rmessage.Result as RestUserMessage) != null) return Rmessage.Result as RestUserMessage;

            var user = guild.GetUser(child.UserId);
            var message = channel.SendMessageAsync("", false, GetLockEmbed(child, user.Mention));
            message.Wait();

            message.Result.AddReactionAsync(LockEmoji);

            child.MainMessageId = message.Result.Id;
            return message.Result;
        }
        public static Embed GetLockEmbed(TicketChild child, string Mention)
        {
            EmbedBuilder builder;
            var guild = GetGuild(child.ParentGuildId);
            var ticket = GetTicket(child.ParentGuildId, child.ParentTicketId);
            switch (guild.Lang)
            {
                case LangEnum.Frensh:
                    builder = new EmbedBuilder()
                    {
                        Author = new EmbedAuthorBuilder() { Name = $"Ticket Tool ~ {ticket.Name}", IconUrl = @"https://cdn.discordapp.com/avatars/557628352828014614/04cdd55608f6f9942c9ab3bbcab3932c.png?size=512", Url = @"https://github.com/Saadbg/TicketBot" },
                        Description = $"Merci d'avoir créé un ticket {Mention}. \n"
                              + "Notre staff va vous répondre au plus bref délai. \n"
                              + "Pour fermer ce ticket, réagissez avec cette emoji: 🔒",
                        Timestamp = DateTime.Now,
                        Footer = new EmbedFooterBuilder() { Text = $"{guild.Name} Support", IconUrl = guild.IconUrl }
                    };
                    return builder.Build();
                default:
                    builder = new EmbedBuilder()
                    {
                        Author = new EmbedAuthorBuilder() { Name = $"Ticket Tool ~ {ticket.Name}", IconUrl = @"https://cdn.discordapp.com/avatars/557628352828014614/04cdd55608f6f9942c9ab3bbcab3932c.png?size=512", Url = @"https://github.com/Saadbg/TicketBot" },
                        Description = $"Thanks for creating a ticket {Mention}. \n"
                              + "Our staff will reply you as soon as possible. \n"
                              + "To close the ticket, react with this emoji: 🔒",
                        Timestamp = DateTime.Now,
                        Footer = new EmbedFooterBuilder() { Text = $"{guild.Name} Support", IconUrl = guild.IconUrl }
                    };
                    return builder.Build();
            }
        }
        #endregion

        #region LockMessage

        public static RestUserMessage GetOrCreateLockMessage(DiscordSocketClient client, SocketGuildUser m_user, TicketChild child)
        {
            var guild = client.GetGuild(child.ParentGuildId);
            if (guild == null)
                return null;

            var channel = guild.GetTextChannel(child.ChannelId);
            if (channel == null)
                return null;

            var Rmessage = channel.GetMessageAsync(child.LockMessageId);
            Rmessage.Wait();
            if ((Rmessage.Result as RestUserMessage) != null) return Rmessage.Result as RestUserMessage;

            if (m_user != null) SendLockedMessage(client, m_user, channel, child);

            var message = channel.SendMessageAsync("", false, GetLockMessageEmbed(child));
            message.Wait();

            message.Result.AddReactionAsync(UnlockEmoji);
            message.Result.AddReactionAsync(TranscriptEmoji);
            message.Result.AddReactionAsync(DeleteEmoji);

            var user = guild.GetUser(child.UserId);
            channel.AddPermissionOverwriteAsync(user, SpectatePerms);



            child.LockMessageId = message.Result.Id;
            return message.Result;
        }

        public static Embed GetLockMessageEmbed(TicketChild child)
        {
            EmbedBuilder builder;
            var guild = GetGuild(child.ParentGuildId);
            switch (guild.Lang)
            {
                case LangEnum.Frensh:
                    builder = new EmbedBuilder()
                    {
                        Author = new EmbedAuthorBuilder() { Name = $"Ticket Tool ~ Mod tools", IconUrl = @"https://cdn.discordapp.com/avatars/557628352828014614/04cdd55608f6f9942c9ab3bbcab3932c.png?size=512", Url = @"https://github.com/Saadbg/TicketBot" },
                        Description = "🔓 Réouvrir le ticket. \n"
                              + "📑 Sauvegarder le transcript et supprimer le ticket. \n"
                              + "⛔ Supprimer le ticket.",
                        Timestamp = DateTime.Now,
                        Footer = new EmbedFooterBuilder() { Text = $"{guild.Name} Support", IconUrl = guild.IconUrl }
                    };
                    return builder.Build();
                default:
                    builder = new EmbedBuilder()
                    {
                        Author = new EmbedAuthorBuilder() { Name = $"Ticket Tool ~ Mod tools", IconUrl = @"https://cdn.discordapp.com/avatars/557628352828014614/04cdd55608f6f9942c9ab3bbcab3932c.png?size=512", Url = @"https://github.com/Saadbg/TicketBot" },
                        Description = "🔓 Unlock the ticket. \n"
                              + "📑 Save the transcript and delete the ticket. \n"
                              + "⛔ Delete the ticket.",
                        Timestamp = DateTime.Now,
                        Footer = new EmbedFooterBuilder() { Text = $"{guild.Name} Support", IconUrl = guild.IconUrl }
                    };
                    return builder.Build();
            }
        }

        #endregion

        #region SimpleMessages

        public static void SendLockedMessage(DiscordSocketClient client, SocketGuildUser user, SocketTextChannel channel, TicketChild child)
        {
            var guild = GetGuild(child.ParentGuildId);
            var builder = new EmbedBuilder()
            {
                Color = Color.LightOrange,
                Description = guild.Lang == LangEnum.Frensh ? $"Ticket verouillé par {user.Mention}" : $"Ticket locked by {user.Mention}"
            };
            channel.SendMessageAsync("", false, builder.Build());
        }
        public static void SendTranscriptMessage(DiscordSocketClient client, SocketTextChannel channel, TicketChild child)
        {
            var guild = GetGuild(child.ParentGuildId);
            var builder = new EmbedBuilder()
            {
                Color = Color.Gold,
                Description = guild.Lang == LangEnum.Frensh ? $"Transcript enregistré." : $"Transcript saved."
            };
            channel.SendMessageAsync("", false, builder.Build());
        }

        public static void SendClosedMessage(DiscordSocketClient client, SocketTextChannel channel, TicketChild child)
        {
            var guild = GetGuild(child.ParentGuildId);
            var builder = new EmbedBuilder()
            {
                Color = Color.DarkRed,
                Description = guild.Lang == LangEnum.Frensh ? $"Le ticket va se fermer dans 5 secondes..." : $"The ticket will be deleted in 5 seconds..."
            };
            channel.SendMessageAsync("", false, builder.Build());
        }

        #endregion

        #region Modifications

        public static void ForceDeleteChild(DiscordSocketClient client, TicketChild child)
        {
            var ticket = GetTicket(child.ParentGuildId, child.ParentTicketId);
            if (ticket.ActiveChildChannels.ContainsKey(child.Id)) ticket.ActiveChildChannels.Remove(child.Id);
        }

        public static void DeleteChild(DiscordSocketClient client, TicketChild child, bool Intern = true)
        {
            var ticket = GetTicket(child.ParentGuildId, child.ParentTicketId);
            if (ticket.ActiveChildChannels.ContainsKey(child.Id) && Intern) ticket.ActiveChildChannels.Remove(child.Id);

            var channel = GetChannel(client, child);
            if (channel == null)
                return;

            SendClosedMessage(client, channel, child);
            Task.Factory.StartNewDelayed(7000, () => ScheduledChildDelete(channel));
        }

        public static void DeleteChildWithTranscript(DiscordSocketClient client, TicketChild child)
        {
            var ticket = GetTicket(child.ParentGuildId, child.ParentTicketId);
            if (ticket.ActiveChildChannels.ContainsKey(child.Id)) ticket.ActiveChildChannels.Remove(child.Id);

            var channel = GetChannel(client, child);
            if (channel == null)
                return;

           // var g = channel.GetMessagesAsync(100);
            

            SendTranscriptMessage(client, channel, child);
            SendClosedMessage(client, channel, child);
            Task.Factory.StartNewDelayed(5000, () => ScheduledChildDelete(channel));
        }

        public static void ScheduledChildDelete(SocketTextChannel channel) => channel.DeleteAsync();

        public static void ChangeChildState(TicketState state, DiscordSocketClient client, SocketGuildUser user, TicketChild child)
        {
            child.State = state;
            switch (state)
            {
                case TicketState.Locked:
                    ChildLock(client, user, child);
                    break;
                case TicketState.Open:
                    ChildReOpen(client, child);
                    break;
                default:
                    DeleteChild(client, child);
                    break;
            }
        }

        public static void ChildReOpen(DiscordSocketClient client, TicketChild child)
        {
            GetOrCreateLockMessage(client, null, child).DeleteAsync();
            child.LockMessageId = 666;
            var message = GetOrCreateMainMessage(client, child);

            var guild = client.GetGuild(child.ParentGuildId);
            if (guild == null)
                return;

            message.RemoveAllReactionsAsync();
            message.AddReactionAsync(UnlockEmoji);
            var user = guild.GetUser(child.UserId);
            (message.Channel as SocketTextChannel).AddPermissionOverwriteAsync(user, AllowPerms);
        }

        public static void ChildLock(DiscordSocketClient client, SocketGuildUser user, TicketChild child)
        {
            var message = GetOrCreateMainMessage(client, child);
            message.RemoveAllReactionsAsync();
            message.AddReactionAsync(UnlockEmoji);
            GetOrCreateLockMessage(client, user, child);
        }

        #endregion


        #endregion

        #region Category Functions

        public static SocketCategoryChannel GetOrCreateCategoryChannel(DiscordSocketClient client, Ticket ticket)
        {
            var guild = client.GetGuild(ticket.ParentGuildId);
            if (guild == null)
                return null;

            var category = guild.GetCategoryChannel(ticket.CategoryId);
            if (category != null)
                return category;

            var creation = guild.CreateCategoryChannelAsync(ticket.Name, (param => { param.Position = guild.CategoryChannels.Count; }));
            creation.Wait();
            ticket.CategoryId = creation.Result.Id;

            return guild.GetCategoryChannel(ticket.CategoryId);
        }

        public static void DeleteTicket(DiscordSocketClient client, Ticket ticket)
        {
            var category = GetOrCreateCategoryChannel(client, ticket);
            if (category == null)
                return;

            ticket.ActiveChildChannels?.Values.ToList().ForEach(x => DeleteChild(client, x, false));
            ticket.ActiveChildChannels.Clear();
            category.DeleteAsync();
        }

        #endregion
    }
}
