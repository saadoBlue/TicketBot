using Dapper;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using TicketBot.Guild.GuildClasses;

namespace TicketBot.ORM
{
    public class DapperORM
    {

        private const string ConnectionString = "SERVER=localhost;" + "DATABASE=ticket_tool;" + "UID=root;" + "PASSWORD=;";

        private MySqlConnection Connection;
        public DapperORM()
        {
            Connection = new MySqlConnection(ConnectionString);
            Connection.Open();
        }

        public List<GuildInfo> GetGuildInfos() => Connection.Query<GuildDatabase>("SELECT * FROM guilds").Select(x => Program.guildManager.SwitchGuildToInfo(x)).ToList();


        public void Save(GuildDatabase database)
        {
            var query = GetUpdateQuery(database);
            query.ExecuteNonQuery();
        }

        public void Insert(GuildDatabase database)
        {
            var query = GetInsertQuery(database);
            query.ExecuteNonQuery();
        }

        public MySqlCommand GetUpdateQuery(GuildDatabase database)
        {
            var command = new MySqlCommand($"UPDATE guilds SET Name = '{database.Name}', Lang = '{(int)database.Lang}', IconUrl = '{database.IconUrl}', TicketsBin = @tbin, SetupMessagesBin = @smbin where Id = {database.Id};", Connection);
            command.Parameters.Add("@tbin", MySqlDbType.LongBlob).Value = database.TicketsBin;
            command.Parameters.Add("@smbin", MySqlDbType.LongBlob).Value = database.SetupMessagesBin;
            return command;
        }

        public MySqlCommand GetInsertQuery(GuildDatabase database)
        {
            var command = new MySqlCommand($"INSERT INTO guilds(Id, Name, Lang, IconUrl, TicketsBin, SetupMessagesBin) VALUES({database.Id},'{database.Name}',{(int)database.Lang},'{database.IconUrl}',@tbin,@smbin);", Connection);
            command.Parameters.Add("@tbin", MySqlDbType.LongBlob).Value = database.TicketsBin;
            command.Parameters.Add("@smbin", MySqlDbType.LongBlob).Value = database.SetupMessagesBin;
            return command;
        }

    }
}
