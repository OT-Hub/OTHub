using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using OTHub.APIServer.Helpers;
using OTHub.Settings;
using OTHub.Settings.Constants;
using Quartz;

namespace OTHub.APIServer.Notifications
{
    [DisallowConcurrentExecution]
    public class LowAvailableTokensJob : IJob
    {
        private readonly TelegramBot _bot;

        public LowAvailableTokensJob(TelegramBot bot)
        {
            _bot = bot;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (!_bot.IsConnected)
                return;

            decimal minimumStake = TracToken.MinimumStake;

            await using (MySqlConnection connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                LowAvailableTokenUsers[] users = (await connection.QueryAsync<LowAvailableTokenUsers>(
                    @"SELECT ts.UserID, ts.LowAvailableTokensAmount, u.TelegramUserID
FROM telegramsettings ts
JOIN Users u on u.ID = ts.UserID
WHERE ts.LowAvailableTokensEnabled = 1 AND ts.NotificationsEnabled = 1 AND ts.HasReceivedMessageFromUser = 1 AND u.TelegramUserID is not null")).ToArray();

                foreach (LowAvailableTokenUsers user in users)
                {
                    try
                    {
                        LowAvailableTokenNode[] nodes = (await connection.QueryAsync<LowAvailableTokenNode>(
                            @"SELECT i.NodeID, i.Identity, i.Stake, i.StakeReserved, i.BlockchainID, b.DisplayName AS BlockchainName, mn.DisplayName NodeName
FROM otidentity i
JOIN blockchains b ON b.ID = i.BlockchainID
JOIN mynodes mn ON mn.NodeID = i.NodeId
WHERE mn.UserID = @userID AND i.LastActivityTimestamp > DATE_Add(NOW(), INTERVAL -30 DAY)",
                            new
                            {
                                userID = user.UserID
                            })).ToArray();

                        foreach (LowAvailableTokenNode lowAvailableTokenNode in nodes)
                        {
                            decimal available = lowAvailableTokenNode.Stake - minimumStake -
                                                lowAvailableTokenNode.StakeReserved;

                            if (available < 0)
                            {
                                available = 0;
                            }

                            if (available < user.LowAvailableTokensAmount)
                            {
                                try
                                {
                                    await Task.Delay(200);
                                    await _bot.LowAvailableTokensOnNode(user, lowAvailableTokenNode, available);
                                }
                                catch (Exception e)
                                {

                                }
                            }
                        }
                    }
                    catch
                    {

                    }
                }
            }
        }
    }

    public class LowAvailableTokenUsers
    {
        public string UserID { get; set; }
        public int LowAvailableTokensAmount { get; set; }
        public string TelegramUserID { get; set; }
    }

    public class LowAvailableTokenNode
    {
        public string NodeID { get; set; }
        public string Identity { get; set; }
        public decimal Stake { get; set; }
        public decimal StakeReserved { get; set; }
        public int BlockchainID { get; set; }
        public string BlockchainName { get; set; }
        public string NodeName { get; set; }
    }
}