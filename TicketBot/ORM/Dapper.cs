using Dapper;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Linq;
using TicketBot.Guild;
using TicketBot.Maps;

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

        public List<GuildEngine> GetGuildEngines() => Connection.Query<GuildMap>("SELECT * FROM guilds").Select(x => Managers.GuildManager.UnMapGuild(x)).ToList();


        public void Save(GuildMap database)
        {
            var query = GetUpdateQuery(database);
            query.ExecuteNonQuery();
        }

        public void Insert(GuildMap database)
        {
            var query = GetInsertQuery(database);
            query.ExecuteNonQuery();
        }

        public MySqlCommand GetUpdateQuery(GuildMap database)
        {
            var command = new MySqlCommand($"UPDATE guilds SET Name = '{database.Name}', Lang = '{(int)database.Lang}', IconUrl = '{database.IconUrl}', PermittedRolesCSV = '{database.PermittedRolesCSV}', TicketsBin = @tbin, SetupMessagesBin = @smbin where Id = {database.Id};", Connection);
            command.Parameters.Add("@tbin", MySqlDbType.LongBlob).Value = database.TicketsBin;
            command.Parameters.Add("@smbin", MySqlDbType.LongBlob).Value = database.SetupMessagesBin;
            return command;
        }

        public MySqlCommand GetInsertQuery(GuildMap database)
        {
            var command = new MySqlCommand($"INSERT INTO guilds(Id, Name, Lang, IconUrl, PermittedRolesCSV, TicketsBin, SetupMessagesBin) VALUES({database.Id},'{database.Name}',{(int)database.Lang},'{database.IconUrl}','{database.PermittedRolesCSV}',@tbin,@smbin);", Connection);
            command.Parameters.Add("@tbin", MySqlDbType.LongBlob).Value = database.TicketsBin;
            command.Parameters.Add("@smbin", MySqlDbType.LongBlob).Value = database.SetupMessagesBin;
            return command;
        }

    }
}
