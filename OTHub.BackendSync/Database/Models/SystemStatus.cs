using System;
using Dapper;
using MySql.Data.MySqlClient;

namespace OTHub.BackendSync.Database.Models
{
    public class SystemStatus
    {
        public string Name { get; }

        public SystemStatus(string name)
        {
            Name = name;
        }

        public void InsertOrUpdate(MySqlConnection connection, bool success)
        {
            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM systemstatus WHERE Name = @Name", new
            {
               Name = Name
            });

            if (count == 0)
            {
                Insert(connection, success);
            }
            else
            {
                Update(connection, success);
            }
        }

        private void Insert(MySqlConnection connection, bool success)
        {
            var now = DateTime.Now;

            connection.Execute(@"INSERT INTO systemstatus(Name, LastSuccessDateTime, LastTriedDateTime, Success) 
VALUES(@Name, @LastSuccessDateTime, @LastTriedDateTime, @Success)",
                new
                {
                    Name = Name,
                    Success = success,
                    LastTriedDateTime = now,
                    LastSuccessDateTime = success ? (DateTime?)now : null
                });
        }

        private void Update(MySqlConnection connection, bool success)
        {
            var now = DateTime.Now;

            connection.Execute(@"UPDATE systemstatus SET Success = @Success, LastTriedDateTime = @LastTriedDateTime,
LastSuccessDateTime = COALESCE(@LastSuccessDateTime, LastSuccessDateTime) WHERE Name = @Name", new
            {
                Name = Name,
                Success = success,
                LastTriedDateTime = now,
                LastSuccessDateTime = success ? (DateTime?)now : null
            });
        }
    }
}