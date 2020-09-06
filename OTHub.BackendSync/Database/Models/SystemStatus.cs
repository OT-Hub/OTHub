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

        public void InsertOrUpdate(MySqlConnection connection, bool? success, DateTime? nextRunDateTime, bool isRunning)
        {
            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM systemstatus WHERE Name = @Name", new
            {
               Name = Name
            });

            if (count == 0)
            {
                Insert(connection, success, isRunning, nextRunDateTime);
            }
            else
            {
                Update(connection, success, isRunning, nextRunDateTime);
            }
        }

        private void Insert(MySqlConnection connection, bool? success, bool isRunning, DateTime? nextRunDateTime)
        {
            var now = DateTime.Now;

            connection.Execute(@"INSERT INTO systemstatus(Name, LastSuccessDateTime, LastTriedDateTime, Success, IsRunning, NextRunDateTime) 
VALUES(@Name, @LastSuccessDateTime, @LastTriedDateTime, @Success, @IsRunning, @NextRunDateTime)",
                new
                {
                    Name = Name,
                    Success = success ?? true,
                    LastTriedDateTime = now,
                    LastSuccessDateTime = success == true ? (DateTime?)now : null,
                    NextRunDateTime = nextRunDateTime,
                    IsRunning = isRunning
                });
        }

        private void Update(MySqlConnection connection, bool? success, bool isRunning, DateTime? nextRunDateTime)
        {
            var now = DateTime.Now;

            connection.Execute(@"UPDATE systemstatus SET Success = COALESCE(@Success, Success), LastTriedDateTime = @LastTriedDateTime, IsRunning = @IsRunning, NextRunDateTime = @NextRunDateTime,
LastSuccessDateTime = COALESCE(@LastSuccessDateTime, LastSuccessDateTime) WHERE Name = @Name", new
            {
                Name = Name,
                Success = success,
                LastTriedDateTime = now,
                LastSuccessDateTime = success == true ? (DateTime?)now : null,
                NextRunDateTime = nextRunDateTime,
                IsRunning = isRunning
            });
        }
    }
}