using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MySql.Data.MySqlClient;
using OTHub.BackendSync.Logging;
using OTHub.Settings;

namespace OTHub.BackendSync.System.Tasks
{
    public class DatabaseUpgradeTask : TaskRun
    {
        public override async Task Execute(Source source)
        {
            using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var update = @"ALTER TABLE OTIdentity
ADD COLUMN IF NOT EXISTS	`Approved` BIT(1) NULL DEFAULT NULL";

                connection.Execute(update);

                update = @"ALTER TABLE otcontract_holding_paidout
ADD COLUMN IF NOT EXISTS	`AmountInUSD` DECIMAL(10, 2) NULL DEFAULT NULL";

                connection.Execute(update);

                connection.Execute(@"CREATE TABLE IF NOT EXISTS `marketvaluebyday` (
	`Date` DATE NOT NULL,
	`Open` DECIMAL(10,8) NOT NULL,
	`High` DECIMAL(10,8) NOT NULL,
	`Low` DECIMAL(10,8) NOT NULL,
	`Close` DECIMAL(10,8) NOT NULL,
	`Volume` BIGINT(20) NOT NULL,
	`MarketCap` BIGINT(20) NOT NULL,
	PRIMARY KEY (`Date`)
)");

                connection.Execute(@"CREATE TABLE IF NOT EXISTS `otcontract_profile_tokensdeposited` (
	`ID` BIGINT(20) NOT NULL AUTO_INCREMENT,
	`TransactionHash` VARCHAR(100) NOT NULL,
	`Profile` VARCHAR(100) NOT NULL,
	`AmountDeposited` BIGINT(20) NOT NULL,
	`NewBalance` BIGINT(20) NOT NULL,
	`ContractAddress` VARCHAR(100) NOT NULL,
	`BlockNumber` BIGINT(20) NOT NULL,
	PRIMARY KEY (`ID`),
	INDEX `TransactionHash` (`TransactionHash`)
)");

                connection.Execute(@"CREATE TABLE IF NOT EXISTS `otcontract_profile_tokensreleased` (
	`ID` BIGINT(20) NOT NULL AUTO_INCREMENT,
	`TransactionHash` VARCHAR(100) NOT NULL,
	`Profile` VARCHAR(100) NOT NULL,
	`Amount` BIGINT(20) NOT NULL,
	`ContractAddress` VARCHAR(100) NOT NULL,
	`BlockNumber` BIGINT(20) NOT NULL,
	PRIMARY KEY (`ID`),
	INDEX `TransactionHash` (`TransactionHash`)
)");

                connection.Execute(@"CREATE TABLE IF NOT EXISTS `otcontract_profile_tokensreserved` (
	`ID` BIGINT(20) NOT NULL AUTO_INCREMENT,
	`TransactionHash` VARCHAR(100) NOT NULL,
	`Profile` VARCHAR(100) NOT NULL,
	`AmountReserved` BIGINT(20) NOT NULL,
	`ContractAddress` VARCHAR(100) NOT NULL,
	`BlockNumber` BIGINT(20) NOT NULL,
	PRIMARY KEY (`ID`),
	INDEX `TransactionHash` (`TransactionHash`)
)");

                connection.Execute(@"CREATE TABLE IF NOT EXISTS `otcontract_profile_tokenstransferred` (
	`ID` BIGINT(20) NOT NULL AUTO_INCREMENT,
	`TransactionHash` VARCHAR(100) NOT NULL,
	`Sender` VARCHAR(100) NOT NULL,
	`Receiver` VARCHAR(100) NOT NULL,
	`Amount` BIGINT(20) NOT NULL,
	`ContractAddress` VARCHAR(100) NOT NULL,
	`BlockNumber` BIGINT(20) NOT NULL,
	PRIMARY KEY (`ID`),
	INDEX `TransactionHash` (`TransactionHash`)
)");

                connection.Execute(@"CREATE TABLE IF NOT EXISTS `otcontract_profile_tokenswithdrawn` (
	`ID` BIGINT(20) NOT NULL AUTO_INCREMENT,
	`TransactionHash` VARCHAR(100) NOT NULL,
	`Profile` VARCHAR(100) NOT NULL,
	`AmountWithdrawn` BIGINT(20) NOT NULL,
	`NewBalance` BIGINT(20) NOT NULL,
	`ContractAddress` VARCHAR(100) NOT NULL,
	`BlockNumber` BIGINT(20) NOT NULL,
	PRIMARY KEY (`ID`),
	INDEX `TransactionHash` (`TransactionHash`)
)");

                connection.Execute(@"ALTER TABLE otidentity
ADD COLUMN IF NOT EXISTS	`TotalOffers` INT NULL DEFAULT NULL");

                connection.Execute(@"ALTER TABLE otidentity
ADD COLUMN IF NOT EXISTS	`OffersLast7Days` INT NULL DEFAULT NULL");

                connection.Execute(@"ALTER TABLE otidentity
ADD COLUMN IF NOT EXISTS	`ActiveOffers` INT NULL DEFAULT NULL");

                connection.Execute(@"ALTER TABLE otnode_ipinfo
ADD COLUMN IF NOT EXISTS	`LastCheckedTimestamp` datetime NULL DEFAULT NULL");

                connection.Execute(@"ALTER TABLE otnode_ipinfo
DROP COLUMN IF EXISTS	`ToBlockNumber`");

                connection.Execute(@"ALTER TABLE otcontract
ADD COLUMN IF NOT EXISTS	`IsArchived` bit NOT NULL DEFAULT false");

                connection.Execute(@"ALTER TABLE otcontract
ADD COLUMN IF NOT EXISTS	`LastSyncedTimestamp` datetime NULL DEFAULT NULL");

                connection.Execute(@"ALTER TABLE otidentity
ADD COLUMN IF NOT EXISTS	`LastSyncedTimestamp` datetime NULL DEFAULT NULL");

                var updateRows = connection
                    .Query("SELECT * FROM OTOffer WHERE DCNodeId like '0x0000000000000000000000%'").ToArray();

                if (updateRows.Any())
                {
                    Console.WriteLine("Updating bad DCNodeIds for " + updateRows.Length + " offers.");
                }
                foreach (var row in updateRows)
                {
                    string offerId = row.OfferID;
                    string nodeId = row.DCNodeId;

                    if (nodeId.Length > 40)
                    {
                        nodeId = nodeId.Substring(nodeId.Length - 40);

                        connection.Execute("UPDATE OTOffer SET DCNodeID = @nodeId WHERE OfferID = @offerId", new {nodeId, offerId });
                    }
                }

                connection.Execute(
                    "DELETE from otcontract_holding_offertask where offerid = '0xfc9b3be5d10e49b3a378ab4a9af79fd70f4256182aa089900d78420f7db29d2b'"); //uncle block

                connection.Execute(
                    "DELETE from otoffer where offerid = '0xfc9b3be5d10e49b3a378ab4a9af79fd70f4256182aa089900d78420f7db29d2b'"); //uncle block

                try
                {
                    connection.Query("SELECT ID FROM otcontract");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Dropping otcontract able");
                    try
                    {
                        connection.Execute("DROP TABLE otcontract");
                    }
                    catch (Exception exx)
                    {
                        Console.WriteLine("Failed to drop otcontract table: " + exx.Message);
                    }

                    connection.Execute(@"CREATE TABLE `otcontract` (
	`ID` INT NOT NULL AUTO_INCREMENT,
	`Address` VARCHAR(100) NOT NULL,
	`Type` INT(11) NOT NULL,
	`IsLatest` BIT(1) NOT NULL,
	`FromBlockNumber` BIGINT(20) UNSIGNED ZEROFILL NOT NULL DEFAULT '00000000000006744371',
	`SyncBlockNumber` BIGINT(20) UNSIGNED ZEROFILL NOT NULL DEFAULT '00000000000006744371',
	`ToBlockNumber` BIGINT(20) UNSIGNED NULL DEFAULT NULL,
	`IsArchived` BIT(1) NOT NULL DEFAULT b'0',
	`LastSyncedTimestamp` DATETIME NULL DEFAULT NULL,
	PRIMARY KEY (`ID`)
)
COLLATE='latin1_swedish_ci'
ENGINE=InnoDB
;
");
                }

                connection.Execute(
                    "ALTER TABLE `otidentity` DROP FOREIGN KEY IF EXISTS `FK_otidentity_otcontract_profile_identitycreated`;");

                //foreach (var row in connection.Query(@"select * from otcontract_holding_paidout where AmountInUSD is null"))
                //{
                //    DateTime timestamp = row.Timestamp;

                //    if (timestamp.Date != DateTime.Now.Date)
                //    {
                //        var amount = connection.ExecuteScalar<decimal?>(@"select Close from MarketValueByDay WHERE Date = @date", new {date = timestamp.Date});

                //        connection.Execute("UPDATE otcontract_holding_paidout");
                //    }
                //}

                connection.Execute(@"CREATE TABLE IF NOT EXISTS `otnode_history` (
	`Id` BIGINT(20) NOT NULL AUTO_INCREMENT,
	`NodeId` VARCHAR(200) NOT NULL,
	`Timestamp` DATETIME NOT NULL,
	`Success` BIT(1) NOT NULL,
	`Duration` SMALLINT(6) NOT NULL,
	PRIMARY KEY (`Id`),
	INDEX `NodeId` (`NodeId`)
)");

                connection.Execute(@"CREATE TABLE IF NOT EXISTS `discord_nodesubscription` (
	`ID` INT(11) NOT NULL AUTO_INCREMENT,
	`UserID` BIGINT(20) UNSIGNED NOT NULL,
	`Timestamp` DATETIME NOT NULL,
	`NodeID` VARCHAR(200) NOT NULL,
	PRIMARY KEY (`ID`),
	UNIQUE INDEX `UserID_NodeID` (`UserID`, `NodeID`)
)
");

//                connection.Execute(@"ALTER TABLE otoffer
//ADD COLUMN IF NOT EXISTS	`ContractAddress` varchar(100) NULL DEFAULT NULL");

                var duplicates = connection.Query(@"select address, type from otcontract
group by address, type
having count(*) > 1");

                foreach (var duplicate in duplicates)
                {
                    var each = connection.Query(@"SELECT * FROM otcontract WHERE address = @address and type = @type order by id", new
                    {
                        address = duplicate.address, type = duplicate.type
                    }).ToArray();

                    if (each.Length > 1)
                    {
                        for (int i = 2; i <= each.Length; i++)
                        {
                            var id = each[i - 1].ID;

                            Console.WriteLine("Deleting address " + duplicate.address + " type " + duplicate.type);

                            connection.Execute(@"DELETE FROM otcontract where id = @id", new {id});
                        }
                    }
                }

                //                Console.WriteLine("NEED TO REMOVE THIS LINE");
                //                connection.Execute("truncate otnode_history");

                //                connection.Execute(@"update otnode_ipinfo
                //set LastCheckedTimestamp = null, TimeStamp = '1970/01/01'");


                connection.Execute(@"ALTER TABLE otnode_ipinfo
ADD COLUMN IF NOT EXISTS	`UseIPForChecking` bit NOT NULL DEFAULT false");

                connection.Execute(@"CREATE TABLE IF NOT EXISTS `otnode_onlinecheck` (
	`ID` BIGINT(20) UNSIGNED NOT NULL AUTO_INCREMENT,
	`IPAddress` VARCHAR(1000) NOT NULL,
	`Identity` VARCHAR(100) NOT NULL,
	`Timestamp` DATETIME NOT NULL,
	PRIMARY KEY (`ID`),
	INDEX `IPAddress_Timestamp` (`IPAddress`, `Timestamp`),
	INDEX `Identity_Timestamp` (`Identity`, `Timestamp`)
)
");

                connection.Execute(
                    @"ALTER TABLE otoffer_holders DROP FOREIGN KEY IF EXISTS FK_otoffer_holders_otcontract_holding_offerfinalized");


                connection.Execute(@"ALTER TABLE OTNode_IPInfo
MODIFY Wallet varchar(100) NULL");

                connection.Execute(
                    @"CREATE INDEX IF NOT EXISTS `OfferId_HolderIdentity` ON otcontract_litigation_litigationanswered  (`OfferId`, `HolderIdentity`) ;
CREATE INDEX IF NOT EXISTS `OfferId_HolderIdentity` ON otcontract_litigation_litigationcompleted  (`OfferId`, `HolderIdentity`) ;
CREATE INDEX IF NOT EXISTS `OfferId_HolderIdentity` ON otcontract_litigation_litigationinitiated  (`OfferId`, `HolderIdentity`) ;
CREATE INDEX IF NOT EXISTS `OfferId_HolderIdentity` ON otcontract_litigation_replacementstarted  (`OfferId`, `HolderIdentity`) ;");

                connection.Execute(@"ALTER TABLE otcontract_litigation_litigationinitiated
DROP COLUMN IF EXISTS	`RequestedDataIndex`");

                connection.Execute(@"ALTER TABLE otcontract_litigation_litigationinitiated
 ADD COLUMN IF NOT EXISTS	`requestedObjectIndex` BIGINT(20) NOT NULL");

                connection.Execute(@"ALTER TABLE otcontract_litigation_litigationinitiated
 ADD COLUMN IF NOT EXISTS	`requestedBlockIndex` BIGINT(20) NOT NULL");

                connection.Execute(@"ALTER TABLE otnode_ipinfo
ADD COLUMN IF NOT EXISTS `NetworkId` TEXT NULL DEFAULT NULL");

                connection.Execute(@"CREATE TABLE IF NOT EXISTS `systemstatus` (
	`ID` INT(11) NOT NULL AUTO_INCREMENT,
	`Name` VARCHAR(200) NOT NULL,
	`LastSuccessDateTime` DATETIME NULL DEFAULT NULL,
	`LastTriedDateTime` DATETIME NOT NULL,
	`Success` BIT(1) NOT NULL,
	PRIMARY KEY (`ID`)
)");

                connection.Execute(@"ALTER TABLE otoffer
ADD COLUMN IF NOT EXISTS `EstimatedLambda` DECIMAL(10,2) NULL DEFAULT NULL");



                connection.Execute(
    @"CREATE INDEX IF NOT EXISTS `Otoffer_DCNodeID` ON otoffer  (`DCNodeId`) USING BTREE;
CREATE INDEX IF NOT EXISTS `OTContract_Profile_ProfileCreated_Profile` ON OTContract_Profile_ProfileCreated  (`Profile`) USING BTREE;
CREATE INDEX IF NOT EXISTS `OTContract_Profile_IdentityCreated_NewIdentity` ON OTContract_Profile_IdentityCreated  (`NewIdentity`) USING BTREE;");



                connection.Execute(@"DELETE FROM SystemStatus");

                connection.Execute(@"DROP INDEX IF EXISTS `NodeId` on otnode_history");

                connection.Execute(@"CREATE INDEX IF NOT EXISTS `otnode_history_NodeID` ON otnode_history  (`NodeID`, `Timestamp`, `Success`) USING BTREE;");

                connection.Execute(@"ALTER TABLE systemstatus
ADD COLUMN IF NOT EXISTS `IsRunning` bit NOT NULL DEFAULT 0");

                connection.Execute(@"ALTER TABLE systemstatus
ADD COLUMN IF NOT EXISTS `NextRunDateTime` datetime NULL DEFAULT NULL");

                connection.Execute(@"ALTER TABLE systemstatus
MODIFY COLUMN `LastTriedDateTime` datetime NULL DEFAULT NULL");
            }


        }

        public DatabaseUpgradeTask() : base("Database Upgrade")
        {
        }
    }
}