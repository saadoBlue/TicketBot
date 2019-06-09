using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using TicketBot.Core.Extensions;
using TicketBot.Guild.GuildClasses;

namespace TicketBot
{
    public class GuildManager
    {
        public GuildManager(DiscordSocketClient client)
        {
            DiscordClient = client;
        }

        public void Initialize()
        {
            guilds = new Dictionary<ulong, GuildInfo>();

            //Temporary saving
            //var binaryData = GetBinaryData();
            //if (binaryData.LongLength == 0)
            //    Save();
            //else
            //    guilds = FormatterExtensions.ToObject<Dictionary<ulong, GuildInfo>>(binaryData);

            //Task.Factory.StartNewDelayed(60 * 1000, Save);

        }

        public byte[] GetBinaryData()
        {
            if (!File.Exists("saves.bin"))
                File.Create("saves.bin");

            return File.ReadAllBytes("saves.bin");
        }

        public void Save()
        {
            lock (m_lock)
            {
                var binaryData = FormatterExtensions.ToBinary(guilds);
                File.WriteAllBytes("saves.bin", binaryData);
                Console.WriteLine("Saved Guilds");
            }
            Task.Factory.StartNewDelayed(60 * 1000, Save);
        }

        #region Propreties

        private DiscordSocketClient DiscordClient;
        private Dictionary<ulong, GuildInfo> guilds;
        private Emoji TicketEmote => new Emoji("🎫");
        private object m_lock;

        #endregion

        #region Functions

        public GuildInfo GetOrCreateGuild(SocketGuild guild)
        {
            GuildInfo guildInfo;
            if (guilds.ContainsKey(guild.Id))
            {
                guilds.TryGetValue(guild.Id, out guildInfo);
                return guildInfo;
            }

            guildInfo = new GuildInfo(guild.Id);
            guilds.Add(guild.Id, guildInfo);
            return guildInfo;
        }

        public Ticket CreateTicket(SocketGuild guild, string TicketName)
        {
            GuildInfo guildInfo = GetOrCreateGuild(guild);
            return guildInfo.CreateNewTicket(DiscordClient, TicketName);
        }

        public Ticket GetTicket(SocketGuild guild, ulong ticketId)
        {
            GuildInfo guildInfo = GetOrCreateGuild(guild);
            return guildInfo.GetTicket(ticketId);
        }

        public void SetupMessage(string TicketName, string Message, SocketGuild guild, SocketTextChannel channel)
        {
            GuildInfo guildInfo = GetOrCreateGuild(guild);
            var ticket = CreateTicket(guild, TicketName);

            EmbedBuilder v = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder() { Name = $"Ticket Tool ~ {TicketName}", IconUrl = @"https://cdn.discordapp.com/avatars/557628352828014614/04cdd55608f6f9942c9ab3bbcab3932c.png?size=512", Url = @"https://github.com/Saadbg/TicketBot" },
                Description = Message,
                Timestamp = DateTime.Now,
                Footer = new EmbedFooterBuilder() { Text = "Ticket Tool Support", IconUrl = @"https://cdn.discordapp.com/avatars/557628352828014614/04cdd55608f6f9942c9ab3bbcab3932c.png?size=512" }
            };

            var message = channel.SendMessageAsync("", false, v.Build());
            message.Wait();

            message.Result.AddReactionAsync(TicketEmote);

            guildInfo.CreateSetupMessage(message.Result.Id, ticket.Id, channel.Id);
        }

        public void CreateChannelInstance(SocketGuild guild, SocketGuildUser user, SocketReaction reaction)
        {
            if (user.IsBot) return;

            var Rmessage = (reaction.Channel as SocketTextChannel).GetMessageAsync(reaction.MessageId);
            Rmessage.Wait();

            var messge = Rmessage.Result;
            var message = messge as Discord.Rest.RestUserMessage;

            if (message == null) return;

            GuildInfo guildInfo = GetOrCreateGuild(guild);

            var setupMessage = guildInfo.GetSetupMessage(reaction.MessageId);
            if (setupMessage == null) return;

            var ticket = guildInfo.GetTicket(setupMessage.TicketId);
            if (ticket == null) return;

            message.RemoveReactionAsync(reaction.Emote, user);

            if (ticket.ActiveChildChannels.Values.Any(x => x.UserId == user.Id)) return;

            ticket.CreateNewChild(DiscordClient, user);
        }

        #endregion
    }
}
