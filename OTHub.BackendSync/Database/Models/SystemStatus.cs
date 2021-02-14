using System;
using Dapper;
using MySqlConnector;

namespace OTHub.BackendSync.Database.Models
{
    public class SystemStatus
    {
        public string Name { get; }
        public int? BlockchainID { get; }

        public SystemStatus(string name)
        {
            Name = name;
        }

        public SystemStatus(string name, int blockchainID)
        {
            Name = name;
            BlockchainID = blockchainID;
        }

        public void InsertOrUpdate(MySqlConnection connection, bool? success, DateTime? nextRunDateTime, bool isRunning, string parentName)
        {
            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM systemstatus WHERE Name = @Name AND ((@blockchainID is null AND BlockchainID is null) or (@blockchainID is not null and BlockchainID = @blockchainID))", new
            {
               Name = Name,
               blockchainID = BlockchainID
            });

            if (count == 0)
            {
                Insert(connection, success, isRunning, nextRunDateTime, parentName);
            }
            else
            {
                Update(connection, success, isRunning, nextRunDateTime, parentName);
            }
        }

        private void Insert(MySqlConnection connection, bool? success, bool isRunning, DateTime? nextRunDateTime,
            string parentName)
        {
            var now = DateTime.Now;

            connection.Execute(@"INSERT INTO systemstatus(Name, LastSuccessDateTime, LastTriedDateTime, Success, IsRunning, NextRunDateTime, BlockchainID, ParentName) 
VALUES(@Name, @LastSuccessDateTime, @LastTriedDateTime, @Success, @IsRunning, @NextRunDateTime, @BlockchainID, @ParentName)",
                new
                {
                    Name = Name,
                    Success = success ?? true,
                    LastTriedDateTime = now,
                    LastSuccessDateTime = success == true ? (DateTime?)now : null,
                    NextRunDateTime = nextRunDateTime,
                    IsRunning = isRunning,
                    BlockchainID = BlockchainID,
                    ParentName = parentName
                });
        }

        private void Update(MySqlConnection connection, bool? success, bool isRunning, DateTime? nextRunDateTime,
            string parentName)
        {
            var now = DateTime.Now;

            connection.Execute(@"UPDATE systemstatus SET ParentName = @ParentName, Success = COALESCE(@Success, Success), LastTriedDateTime = @LastTriedDateTime, IsRunning = @IsRunning, NextRunDateTime = @NextRunDateTime,
LastSuccessDateTime = COALESCE(@LastSuccessDateTime, LastSuccessDateTime) WHERE Name = @Name AND ((@BlockchainID is null AND BlockchainID is null) or (@BlockchainID is not null and BlockchainID = @BlockchainID))", new
            {
                Name = Name,
                Success = success,
                LastTriedDateTime = now,
                LastSuccessDateTime = success == true ? (DateTime?)now : null,
                NextRunDateTime = nextRunDateTime,
                IsRunning = isRunning,
                BlockchainID = BlockchainID,
                ParentName = parentName
            });
        }
    }
}