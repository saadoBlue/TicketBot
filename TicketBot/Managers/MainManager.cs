using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TicketBot.Core.Enums;
using TicketBot.Core.Extensions;
using TicketBot.Guild;
using TicketBot.Maps;
using TicketBot.Maps.Messages;
using TicketBot.ORM;

namespace TicketBot.Managers
{
    public class MainManager
    {
        public MainManager(DiscordSocketClient client)
        {
            DiscordClient = client;
        }

        public void Initialize()
        {
            Orm = new DapperORM();
            guilds = Orm.GetGuildEngines().ToDictionary(x => x.Id);
            NewGuilds = new List<GuildEngine>();
            Task.Factory.StartNewDelayed(15 * 1000, Save);
        }

        public void Save()
        {
            lock (m_lock)
            {
                if (NewGuilds.Any())
                    foreach (var newGuild in NewGuilds)
                    {
                        Orm.Insert(GuildManager.MapGuild(newGuild));
                    }

                if (guilds.Any())
                    foreach (var guild in guilds)
                    {
                        Orm.Save(GuildManager.MapGuild(guild.Value));
                    }

                NewGuilds.Clear();
            }
            Console.WriteLine($"{DateTime.Now.ToShortTimeString()} GuildManager: Saved guilds !");
            Task.Factory.StartNewDelayed(60 * 1000, Save);
        }

        #region Propreties

        private DiscordSocketClient DiscordClient;
        private DapperORM Orm;
        private Dictionary<ulong, GuildEngine> guilds;
        private List<GuildEngine> NewGuilds;
        private Emoji TicketEmote => new Emoji("🎫");
        private object m_lock = new object();

        #endregion

        #region Functions

        #region Guilds

        public GuildEngine GetOrCreateGuild(SocketGuild guild)
        {
            GuildEngine guildEngine;
            if (guilds.ContainsKey(guild.Id))
            {
                guilds.TryGetValue(guild.Id, out guildEngine);
                return guildEngine;
            }

            if (!Directory.Exists($"./Transcripts/{guild.Id}")) Directory.CreateDirectory($"./Transcripts/{guild.Id}");
            guildEngine = new GuildEngine(guild.Id, guild.Name);
            guilds.Add(guild.Id, guildEngine);
            NewGuilds.Add(guildEngine);
            return guildEngine;
        }

        public GuildEngine GetGuild(ulong Id)
        {
            GuildEngine value;
            guilds.TryGetValue(Id, out value);
            return value;
        }

        #endregion

        #region Ticket

        public Ticket CreateTicket(SocketGuild guild, string TicketName)
        {
            GuildEngine guildEngine = GetOrCreateGuild(guild);
            return GuildManager.CreateNewTicket(DiscordClient, guildEngine, TicketName);
        }

        public Ticket GetTicket(ulong GuildId, ulong TicketId)
        {
            GuildEngine guild;
            guilds.TryGetValue(GuildId, out guild);
            if (guild == null) return null;

            return GuildManager.GetTicket(guild, TicketId);
        }

        public Ticket GetTicket(SocketGuild guild, ulong ticketId)
        {
            return GetTicket(guild.Id, ticketId);
        }

        #endregion

        #region ChildChannel

        public void HandleChannelDeletion(SocketGuildChannel channel)
        {
            var child = guilds.Values.SelectMany(x => x.Tickets.Values.SelectMany(v => v.ActiveChildChannels.Values)).FirstOrDefault(g => g.ChannelId == channel.Id);
            if (child != null)
            {
                TicketManager.ForceDeleteChild(DiscordClient, child);
            }
        }

        #endregion

        #region SetupMessages

        public void SetupMessage(string TicketName, string Message, SocketGuild guild, SocketTextChannel channel)
        {
            GuildEngine guildE = GetOrCreateGuild(guild);
            var ticket = CreateTicket(guild, TicketName);

            EmbedBuilder builder = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder() { Name = $"Ticket Tool ~ {TicketName}", IconUrl = @"https://cdn.discordapp.com/avatars/557628352828014614/04cdd55608f6f9942c9ab3bbcab3932c.png?size=512", Url = @"https://github.com/Saadbg/TicketBot" },
                Description = Message,
                Timestamp = DateTime.Now,
                Footer = new EmbedFooterBuilder() { Text = $"{guildE.Name} Support", IconUrl = guildE.IconUrl }
            };

            var message = channel.SendMessageAsync("", false, builder.Build());
            message.Wait();

            message.Result.AddReactionAsync(TicketEmote);

            GuildManager.CreateSetupMessage(guildE, message.Result.Id, ticket.Id, channel.Id);
        }

        public void HandleMessageDeletion(ulong MessageId, ulong GuildId)
        {
            var guild = GetGuild(GuildId);
            if (guild != null)
            {
                SetupMessage setupMessage;
                guild.SetupMessages.TryGetValue(MessageId, out setupMessage);

                if (setupMessage != null)
                {
                    Ticket ticket;
                    guild.Tickets.TryGetValue(setupMessage.TicketId, out ticket);

                    if (ticket != null)
                        TicketManager.DeleteTicket(DiscordClient, ticket);
                }
            }
        }

        #endregion

        #endregion

        #region Handlers

        public void HandleReaction(SocketGuild guild, SocketGuildUser user, SocketReaction reaction)
        {
            if (user.IsBot) return;

            var Rmessage = (reaction.Channel as SocketTextChannel).GetMessageAsync(reaction.MessageId);
            Rmessage.Wait();

            var messge = Rmessage.Result;
            var message = messge as Discord.Rest.RestUserMessage;

            if (message == null) return;

            GuildEngine guildE = GetOrCreateGuild(guild);

            var setupMessage = GuildManager.GetSetupMessage(guildE, reaction.MessageId);
            if (setupMessage != null)
            {
                HandleSetupMessage(guildE, setupMessage, reaction, message);
                return;
            }

            var child = GuildManager.GetChildByReactionMessageId(guildE, reaction.MessageId);
            if (child != null)
            {
                HandleChildReaction(child, reaction, message);
            }
        }

        public void HandleSetupMessage(GuildEngine guildE, SetupMessage setupMessage, SocketReaction reaction, Discord.Rest.RestUserMessage message)
        {
            var user = reaction.User.Value as SocketGuildUser;
            var ticket = GetTicket(guildE.Id, setupMessage.TicketId);
            if (ticket == null) return;

            message.RemoveReactionAsync(reaction.Emote, user);

            if (ticket.ActiveChildChannels.Values.Any(x => x.UserId == user.Id)) return;

            TicketManager.CreateNewChild(DiscordClient, user, ticket);
        }

        public void HandleChildReaction(TicketChild child, SocketReaction reaction, Discord.Rest.RestUserMessage message)
        {
            var user = reaction.User.Value as SocketGuildUser;
            if (child.State == TicketState.Locked && reaction.UserId == child.UserId)
            {
                if (!user.GuildPermissions.Administrator && !user.Roles.Any(g => TicketManager.GetGuild(child.ParentGuildId).PermittedRoles.Contains(g.Id)))
                {
                    message.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                    return;
                }
            }

            if (message.Id == child.MainMessageId)
            {
                switch (child.State)
                {
                    case TicketState.Open:
                        TicketManager.ChangeChildState(TicketState.Locked, DiscordClient, user, child);
                        break;
                    case TicketState.Locked:
                        TicketManager.ChangeChildState(TicketState.Open, DiscordClient, user, child);
                        break;
                }
            }

            else
            {
                switch (reaction.Emote.Name)
                {
                    case "⛔":
                        TicketManager.DeleteChild(DiscordClient, child);
                        break;
                    case "📑":
                        TicketManager.DeleteChildWithTranscript(DiscordClient, child);
                        break;
                    case "🔓":
                        TicketManager.ChangeChildState(TicketState.Open, DiscordClient, user, child);
                        break;
                }
            }
        }

        #endregion
    }
}
