using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Humanizer;
using Humanizer.Localisation;
using MySqlConnector;
using OTHub.Messaging;

namespace OTHub.APIServer.Notifications
{
    public static class NotificationsReaderWriter
    {
        public static async Task<(string title, string url)> InsertJobWonNotification(MySqlConnection connection, OfferFinalizedMessage message, string userID,
            string nodeName, decimal tokenAmount, long holdingTimeInMinutes)
        {
            string title = $"Job awarded for {nodeName}";

            var exitsingCount = await connection.ExecuteScalarAsync<int>(@"SELECT COUNT(*) FROM notifications where UserID = @userID AND CreatedAt = @date AND Title = @title", 
                new
                {
                    userID = userID,
                    date = message.Timestamp,
                    title = title,
                });

            if (exitsingCount != 0)
                return (null, null);

            var timeInText = TimeSpan.FromMinutes(holdingTimeInMinutes)
                .Humanize(5, maxUnit: TimeUnit.Year, minUnit: TimeUnit.Minute);

            tokenAmount = Math.Truncate(100 * tokenAmount) / 100;

            string url = $"offers/{message.OfferID}";

            await connection.ExecuteAsync("INSERT INTO notifications(`UserID`, `Read`, `Dismissed`, `CreatedAt`, `Title`, `Description`, `RelativeUrl`) VALUES(@userID, 0, 0, @date, @title, @description, @url)", new
            {
                userID = userID,
                date = message.Timestamp,
                title = title,
                description = $"{timeInText} for {tokenAmount:N} TRAC",
                url = url
            });

            return (title, url);
        }
    }
}