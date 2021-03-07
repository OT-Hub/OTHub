
using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using OTHub.BackendSync.Logging;
using OTHub.Settings;

namespace OTHub.BackendSync.System.Tasks
{
    public static class DatabaseUpgradeTask
    {
        public static void Execute()
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

                        connection.Execute("UPDATE OTOffer SET DCNodeID = @nodeId WHERE OfferID = @offerId",
                            new {nodeId, offerId});
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
                    var each = connection.Query(
                        @"SELECT * FROM otcontract WHERE address = @address and type = @type order by id", new
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

                connection.Execute(
                    @"CREATE INDEX IF NOT EXISTS `otnode_history_NodeID` ON otnode_history  (`NodeID`, `Timestamp`, `Success`) USING BTREE;");

                connection.Execute(@"ALTER TABLE systemstatus
ADD COLUMN IF NOT EXISTS `IsRunning` bit NOT NULL DEFAULT 0");

                connection.Execute(@"ALTER TABLE systemstatus
ADD COLUMN IF NOT EXISTS `NextRunDateTime` datetime NULL DEFAULT NULL");

                connection.Execute(@"ALTER TABLE systemstatus
MODIFY COLUMN `LastTriedDateTime` datetime NULL DEFAULT NULL");

                connection.Execute(@"ALTER TABLE otnode_ipinfo
ADD COLUMN IF NOT EXISTS `NetworkLastCheckedTimestamp` DATETIME NULL DEFAULT NULL");

                connection.Execute(@"ALTER TABLE otnode_ipinfo
ADD COLUMN IF NOT EXISTS `UnknownNodeResponse` BIT NOT NULL DEFAULT 0");

                connection.Execute(@"CREATE TABLE if NOT exists `otnode_ipinfov2` (
	`NodeId` VARCHAR(100) NOT NULL COLLATE 'latin1_swedish_ci',
	`Wallet` VARCHAR(100) NULL DEFAULT NULL COLLATE 'latin1_swedish_ci',
	`Port` INT(11) NOT NULL,
	`Timestamp` DATETIME NOT NULL,
	`Hostname` VARCHAR(1000) NOT NULL COLLATE 'latin1_swedish_ci',
	`NetworkId` TEXT(65535) NULL DEFAULT NULL COLLATE 'latin1_swedish_ci',
	`LastCheckedOnlineTimestamp` DATETIME NULL DEFAULT NULL,
	`LastCheckedGetContactTimestamp` DATETIME NULL DEFAULT NULL,
	`UnknownNodeResponseCount` int NOT NULL DEFAULT 0,
	PRIMARY KEY (`NodeId`) USING BTREE
)
COLLATE='latin1_swedish_ci'
ENGINE=InnoDB
;
");

                //connection.Execute(@"delete from otnode_ipinfov2");

                connection.Execute(
                    @"CREATE INDEX IF NOT EXISTS `otidentity_NodeID` ON otidentity  (`NodeID`) USING BTREE;");

                connection.Execute(@"CREATE TABLE IF NOT EXISTS `blockchains` (
  `ID` int(11) NOT NULL AUTO_INCREMENT,
  `BlockchainName` varchar(100) NOT NULL,
  `NetworkName` varchar(100) NOT NULL,
  `DisplayName` varchar(100) NOT NULL,
  `Color` char(6) NOT NULL,
  `HubAddress` varchar(100) NOT NULL,
  `FromBlockNumber` BIGINT(20) UNSIGNED ZEROFILL NOT NULL DEFAULT '00000000000000000000',
  `BlockchainNodeUrl` varchar(500) NOT NULL,
  `TokenTicker` varchar(10) NOT NULL,
  `GasTicker` varchar(10) NOT NULL,
  `ShowCostInUSD` bit NOT NULL,
  PRIMARY KEY (`ID`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=latin1;");


                if (connection.ExecuteScalar<int>(@"SELECT COUNT(*) FROM blockchains") <= 0)
                {
                    Thread.Sleep(2000);
                    throw new Exception("Blockchains table needs to be populated. Make sure blockchain ID 1 is used for the original blockchain to make historical data correct.");
                }


                bool isUpgradedForMultiChain = connection.ExecuteScalar<int>(@$"SELECT 
  COUNT(*)
FROM
  INFORMATION_SCHEMA.KEY_COLUMN_USAGE
WHERE
REFERENCED_TABLE_SCHEMA = '{OTHubSettings.Instance.MariaDB.Database}' AND
  REFERENCED_TABLE_NAME = 'ethblock' AND
  REFERENCED_COLUMN_NAME = 'BlockchainID'") > 0;

                if (!isUpgradedForMultiChain)
                {
                    connection.Execute(@"ALTER TABLE ethblock ADD COLUMN IF NOT EXISTS `BlockchainID` INT NOT NULL DEFAULT 1");
                    //connection.Execute(@"UPDATE ethblock SET blockchainid = 1 WHERE blockchainid IS null"); //TODO long term needs something better

                    //connection.Execute(@"ALTER TABLE ethblock MODIFY COLUMN `BlockchainID` INT NOT NULL");
                    connection.Execute(@"ALTER TABLE `ethblock`
ADD CONSTRAINT `FK_ethblock_blockchains` FOREIGN KEY IF NOT EXISTS
(`blockchainid`) REFERENCES `blockchains` (`id`);");
            

                    connection.Execute(@"ALTER TABLE marketvaluebyday
ADD COLUMN IF NOT EXISTS `BlockchainID` INT NOT NULL DEFAULT 1");
                    connection.Execute(@"ALTER TABLE `marketvaluebyday`
ADD CONSTRAINT `FK_marketvaluebyday_blockchains` FOREIGN KEY IF NOT EXISTS
(`blockchainid`) REFERENCES `blockchains` (`id`);");
                    //connection.Execute(
                    //    @"UPDATE marketvaluebyday SET blockchainid = 1 WHERE blockchainid IS null"); //TODO long term needs something better
                    //connection.Execute(@"ALTER TABLE marketvaluebyday MODIFY COLUMN `BlockchainID` INT NOT NULL");

                    connection.Execute(@"ALTER TABLE otcontract
ADD COLUMN IF NOT EXISTS `BlockchainID` INT NOT NULL DEFAULT 1");
                    connection.Execute(@"ALTER TABLE `otcontract`
ADD CONSTRAINT `FK_otcontract_blockchains` FOREIGN KEY IF NOT EXISTS
(`blockchainid`) REFERENCES `blockchains` (`id`);");
                    //connection.Execute(
                    //    @"UPDATE otcontract SET blockchainid = 1 WHERE blockchainid IS null"); //TODO long term needs something better

                    //connection.Execute(@"ALTER TABLE otcontract MODIFY COLUMN `BlockchainID` INT NOT NULL");

                    connection.Execute(@"ALTER TABLE otcontract_approval_nodeapproved
ADD COLUMN IF NOT EXISTS `BlockchainID` INT NOT NULL DEFAULT 1");
//                connection.Execute(@"ALTER TABLE `otcontract_approval_nodeapproved`
//ADD CONSTRAINT `FK_otcontract_approval_nodeapproved_blockchains` FOREIGN KEY IF NOT EXISTS
//(`blockchainid`) REFERENCES `blockchains` (`id`);");
                    //connection.Execute(
                    //    @"UPDATE otcontract_approval_nodeapproved SET blockchainid = 1 WHERE blockchainid IS null"); //TODO long term needs something better
                    //connection.Execute(
                    //    @"ALTER TABLE otcontract_approval_nodeapproved MODIFY COLUMN `BlockchainID` INT NOT NULL");

                    connection.Execute(@"ALTER TABLE otcontract_approval_noderemoved
ADD COLUMN IF NOT EXISTS `BlockchainID` INT NOT NULL DEFAULT 1");
//                connection.Execute(@"ALTER TABLE `otcontract_approval_noderemoved`
//ADD CONSTRAINT `FK_otcontract_approval_noderemoved_blockchains` FOREIGN KEY IF NOT EXISTS
//(`blockchainid`) REFERENCES `blockchains` (`id`);");
                    //connection.Execute(
                    //    @"UPDATE otcontract_approval_noderemoved SET blockchainid = 1 WHERE blockchainid IS null"); //TODO long term needs something better
                    //connection.Execute(
                    //    @"ALTER TABLE otcontract_approval_noderemoved MODIFY COLUMN `BlockchainID` INT NOT NULL");

                    connection.Execute(@"ALTER TABLE otcontract_holding_offercreated
ADD COLUMN IF NOT EXISTS `BlockchainID` INT NOT NULL DEFAULT 1");
//                connection.Execute(@"ALTER TABLE `otcontract_holding_offercreated`
//ADD CONSTRAINT `FK_otcontract_holding_offercreated_blockchains` FOREIGN KEY IF NOT EXISTS
//(`blockchainid`) REFERENCES `blockchains` (`id`);");
                    //connection.Execute(
                    //    @"UPDATE otcontract_holding_offercreated SET blockchainid = 1 WHERE blockchainid IS null"); //TODO long term needs something better
                    //connection.Execute(
                    //    @"ALTER TABLE otcontract_holding_offercreated MODIFY COLUMN `BlockchainID` INT NOT NULL");

                    connection.Execute(@"ALTER TABLE otcontract_holding_offerfinalized
ADD COLUMN IF NOT EXISTS `BlockchainID` INT NOT NULL DEFAULT 1");
//                connection.Execute(@"ALTER TABLE `otcontract_holding_offerfinalized`
//ADD CONSTRAINT `FK_otcontract_holding_offerfinalized_blockchains` FOREIGN KEY IF NOT EXISTS
//(`blockchainid`) REFERENCES `blockchains` (`id`);");
                    //connection.Execute(
                    //    @"UPDATE otcontract_holding_offerfinalized SET blockchainid = 1 WHERE blockchainid IS null"); //TODO long term needs something better
                    //connection.Execute(
                    //    @"ALTER TABLE otcontract_holding_offerfinalized MODIFY COLUMN `BlockchainID` INT NOT NULL");

                    connection.Execute(@"ALTER TABLE otcontract_holding_offertask
ADD COLUMN IF NOT EXISTS `BlockchainID` INT NOT NULL DEFAULT 1");
//                connection.Execute(@"ALTER TABLE `otcontract_holding_offertask`
//ADD CONSTRAINT `FK_otcontract_holding_offertask_blockchains` FOREIGN KEY IF NOT EXISTS
//(`blockchainid`) REFERENCES `blockchains` (`id`);");
                    //connection.Execute(
                    //    @"UPDATE otcontract_holding_offertask SET blockchainid = 1 WHERE blockchainid IS null"); //TODO long term needs something better
                    //connection.Execute(
                    //    @"ALTER TABLE otcontract_holding_offertask MODIFY COLUMN `BlockchainID` INT NOT NULL");

                    connection.Execute(@"ALTER TABLE otcontract_holding_ownershiptransferred
ADD COLUMN IF NOT EXISTS `BlockchainID` INT NOT NULL DEFAULT 1");
//                connection.Execute(@"ALTER TABLE `otcontract_holding_ownershiptransferred`
//ADD CONSTRAINT `FK_otcontract_holding_ownershiptransferred_blockchains` FOREIGN KEY IF NOT EXISTS
//(`blockchainid`) REFERENCES `blockchains` (`id`);");
                    //connection.Execute(
                    //    @"UPDATE otcontract_holding_ownershiptransferred SET blockchainid = 1 WHERE blockchainid IS null"); //TODO long term needs something better
                    //connection.Execute(
                    //    @"ALTER TABLE otcontract_holding_ownershiptransferred MODIFY COLUMN `BlockchainID` INT NOT NULL");

                    connection.Execute(@"ALTER TABLE otcontract_holding_paidout
ADD COLUMN IF NOT EXISTS `BlockchainID` INT NOT NULL DEFAULT 1");
//                connection.Execute(@"ALTER TABLE `otcontract_holding_paidout`
//ADD CONSTRAINT `FK_otcontract_holding_paidout_blockchains` FOREIGN KEY IF NOT EXISTS
//(`blockchainid`) REFERENCES `blockchains` (`id`);");
                    //connection.Execute(
                    //    @"UPDATE otcontract_holding_paidout SET blockchainid = 1 WHERE blockchainid IS null"); //TODO long term needs something better
                    //connection.Execute(
                    //    @"ALTER TABLE otcontract_holding_paidout MODIFY COLUMN `BlockchainID` INT NOT NULL");

                    connection.Execute(@"ALTER TABLE otcontract_litigation_litigationanswered
ADD COLUMN IF NOT EXISTS `BlockchainID` INT NOT NULL DEFAULT 1");
//                connection.Execute(@"ALTER TABLE `otcontract_litigation_litigationanswered`
//ADD CONSTRAINT `FK_otcontract_litigation_litigationanswered_blockchains` FOREIGN KEY IF NOT EXISTS
//(`blockchainid`) REFERENCES `blockchains` (`id`);");
                    //connection.Execute(
                    //    @"UPDATE otcontract_litigation_litigationanswered SET blockchainid = 1 WHERE blockchainid IS null"); //TODO long term needs something better
                    //connection.Execute(
                    //    @"ALTER TABLE otcontract_litigation_litigationanswered MODIFY COLUMN `BlockchainID` INT NOT NULL");

                    connection.Execute(@"ALTER TABLE otcontract_litigation_litigationcompleted
ADD COLUMN IF NOT EXISTS `BlockchainID` INT NOT NULL DEFAULT 1");
//                connection.Execute(@"ALTER TABLE `otcontract_litigation_litigationcompleted`
//ADD CONSTRAINT `FK_otcontract_litigation_litigationcompleted_blockchains` FOREIGN KEY IF NOT EXISTS
//(`blockchainid`) REFERENCES `blockchains` (`id`);");
                    //connection.Execute(
                    //    @"UPDATE otcontract_litigation_litigationcompleted SET blockchainid = 1 WHERE blockchainid IS null"); //TODO long term needs something better
                    //connection.Execute(
                    //    @"ALTER TABLE otcontract_litigation_litigationcompleted MODIFY COLUMN `BlockchainID` INT NOT NULL");

                    connection.Execute(@"ALTER TABLE otcontract_litigation_litigationinitiated
ADD COLUMN IF NOT EXISTS `BlockchainID` INT NOT NULL DEFAULT 1");
//                connection.Execute(@"ALTER TABLE `otcontract_litigation_litigationinitiated`
//ADD CONSTRAINT `FK_otcontract_litigation_litigationinitiated_blockchains` FOREIGN KEY IF NOT EXISTS
//(`blockchainid`) REFERENCES `blockchains` (`id`);");
                    //connection.Execute(
                    //    @"UPDATE otcontract_litigation_litigationinitiated SET blockchainid = 1 WHERE blockchainid IS null"); //TODO long term needs something better
                    //connection.Execute(
                    //    @"ALTER TABLE otcontract_litigation_litigationinitiated MODIFY COLUMN `BlockchainID` INT NOT NULL");

                    connection.Execute(@"ALTER TABLE otcontract_litigation_litigationtimedout
ADD COLUMN IF NOT EXISTS `BlockchainID` INT NOT NULL DEFAULT 1");
//                connection.Execute(@"ALTER TABLE `otcontract_litigation_litigationtimedout`
//ADD CONSTRAINT `FK_otcontract_litigation_litigationtimedout_blockchains` FOREIGN KEY IF NOT EXISTS
//(`blockchainid`) REFERENCES `blockchains` (`id`);");
                    //connection.Execute(
                    //    @"UPDATE otcontract_litigation_litigationtimedout SET blockchainid = 1 WHERE blockchainid IS null"); //TODO long term needs something better
                    //connection.Execute(
                    //    @"ALTER TABLE otcontract_litigation_litigationtimedout MODIFY COLUMN `BlockchainID` INT NOT NULL");

                    connection.Execute(@"ALTER TABLE otcontract_litigation_replacementstarted
ADD COLUMN IF NOT EXISTS `BlockchainID` INT NOT NULL DEFAULT 1");
//                connection.Execute(@"ALTER TABLE `otcontract_litigation_replacementstarted`
//ADD CONSTRAINT `FK_otcontract_litigation_replacementstarted_blockchains` FOREIGN KEY IF NOT EXISTS
//(`blockchainid`) REFERENCES `blockchains` (`id`);");
                    //connection.Execute(
                    //    @"UPDATE otcontract_litigation_replacementstarted SET blockchainid = 1 WHERE blockchainid IS null"); //TODO long term needs something better
                    //connection.Execute(
                    //    @"ALTER TABLE otcontract_litigation_replacementstarted MODIFY COLUMN `BlockchainID` INT NOT NULL");

                    connection.Execute(@"ALTER TABLE otcontract_profile_identitycreated
ADD COLUMN IF NOT EXISTS `BlockchainID` INT NOT NULL DEFAULT 1");
//                connection.Execute(@"ALTER TABLE `otcontract_profile_identitycreated`
//ADD CONSTRAINT `FK_otcontract_profile_identitycreated_blockchains` FOREIGN KEY IF NOT EXISTS
//(`blockchainid`) REFERENCES `blockchains` (`id`);");
                    //connection.Execute(
                    //    @"UPDATE otcontract_profile_identitycreated SET blockchainid = 1 WHERE blockchainid IS null"); //TODO long term needs something better
                    //connection.Execute(
                    //    @"ALTER TABLE otcontract_profile_identitycreated MODIFY COLUMN `BlockchainID` INT NOT NULL");

                    connection.Execute(@"ALTER TABLE otcontract_profile_identitytransferred
ADD COLUMN IF NOT EXISTS `BlockchainID` INT NOT NULL DEFAULT 1");
//                connection.Execute(@"ALTER TABLE `otcontract_profile_identitytransferred`
//ADD CONSTRAINT `FK_otcontract_profile_identitytransferred_blockchains` FOREIGN KEY IF NOT EXISTS
//(`blockchainid`) REFERENCES `blockchains` (`id`);");
                    //connection.Execute(
                    //    @"UPDATE otcontract_profile_identitytransferred SET blockchainid = 1 WHERE blockchainid IS null"); //TODO long term needs something better
                    //connection.Execute(
                    //    @"ALTER TABLE otcontract_profile_identitytransferred MODIFY COLUMN `BlockchainID` INT NOT NULL");

                    connection.Execute(@"ALTER TABLE otcontract_profile_profilecreated
ADD COLUMN IF NOT EXISTS `BlockchainID` INT NOT NULL DEFAULT 1");
//                connection.Execute(@"ALTER TABLE `otcontract_profile_profilecreated`
//ADD CONSTRAINT `FK_otcontract_profile_profilecreated_blockchains` FOREIGN KEY IF NOT EXISTS
//(`blockchainid`) REFERENCES `blockchains` (`id`);");
                    //connection.Execute(
                    //    @"UPDATE otcontract_profile_profilecreated SET blockchainid = 1 WHERE blockchainid IS null"); //TODO long term needs something better
                    //connection.Execute(
                    //    @"ALTER TABLE otcontract_profile_profilecreated MODIFY COLUMN `BlockchainID` INT NOT NULL");

                    connection.Execute(@"ALTER TABLE otcontract_profile_tokensdeposited
ADD COLUMN IF NOT EXISTS `BlockchainID` INT NOT NULL DEFAULT 1");
//                connection.Execute(@"ALTER TABLE `otcontract_profile_tokensdeposited`
//ADD CONSTRAINT `FK_otcontract_profile_tokensdeposited_blockchains` FOREIGN KEY IF NOT EXISTS
//(`blockchainid`) REFERENCES `blockchains` (`id`);");
                    //connection.Execute(
                    //    @"UPDATE otcontract_profile_tokensdeposited SET blockchainid = 1 WHERE blockchainid IS null"); //TODO long term needs something better
                    //connection.Execute(
                    //    @"ALTER TABLE otcontract_profile_tokensdeposited MODIFY COLUMN `BlockchainID` INT NOT NULL");

                    connection.Execute(@"ALTER TABLE otcontract_profile_tokensreleased
ADD COLUMN IF NOT EXISTS `BlockchainID` INT NOT NULL DEFAULT 1");
//                connection.Execute(@"ALTER TABLE `otcontract_profile_tokensreleased`
//ADD CONSTRAINT `FK_otcontract_profile_tokensreleased_blockchains` FOREIGN KEY IF NOT EXISTS
//(`blockchainid`) REFERENCES `blockchains` (`id`);");
                    //connection.Execute(
                    //    @"UPDATE otcontract_profile_tokensreleased SET blockchainid = 1 WHERE blockchainid IS null"); //TODO long term needs something better
                    //connection.Execute(
                    //    @"ALTER TABLE otcontract_profile_tokensreleased MODIFY COLUMN `BlockchainID` INT NOT NULL");

                    connection.Execute(@"ALTER TABLE otcontract_profile_tokensreserved
ADD COLUMN IF NOT EXISTS `BlockchainID` INT NOT NULL DEFAULT 1");
//                connection.Execute(@"ALTER TABLE `otcontract_profile_tokensreserved`
//ADD CONSTRAINT `FK_otcontract_profile_tokensreserved_blockchains` FOREIGN KEY IF NOT EXISTS
//(`blockchainid`) REFERENCES `blockchains` (`id`);");
                    //connection.Execute(
                    //    @"UPDATE otcontract_profile_tokensreserved SET blockchainid = 1 WHERE blockchainid IS null"); //TODO long term needs something better
                    //connection.Execute(
                    //    @"ALTER TABLE otcontract_profile_tokensreserved MODIFY COLUMN `BlockchainID` INT NOT NULL");

                    connection.Execute(@"ALTER TABLE otcontract_profile_tokenstransferred
ADD COLUMN IF NOT EXISTS `BlockchainID` INT NOT NULL DEFAULT 1");
//                connection.Execute(@"ALTER TABLE `otcontract_profile_tokenstransferred`
//ADD CONSTRAINT `FK_otcontract_profile_tokenstransferred_blockchains` FOREIGN KEY IF NOT EXISTS
//(`blockchainid`) REFERENCES `blockchains` (`id`);");
                    //connection.Execute(
                    //    @"UPDATE otcontract_profile_tokenstransferred SET blockchainid = 1 WHERE blockchainid IS null"); //TODO long term needs something better
                    //connection.Execute(
                    //    @"ALTER TABLE otcontract_profile_tokenstransferred MODIFY COLUMN `BlockchainID` INT NOT NULL");

                    connection.Execute(@"ALTER TABLE otcontract_profile_tokenswithdrawn
ADD COLUMN IF NOT EXISTS `BlockchainID` INT NOT NULL DEFAULT 1");
//                connection.Execute(@"ALTER TABLE `otcontract_profile_tokenswithdrawn`
//ADD CONSTRAINT `FK_otcontract_profile_tokenswithdrawn_blockchains` FOREIGN KEY IF NOT EXISTS
//(`blockchainid`) REFERENCES `blockchains` (`id`);");
                    //connection.Execute(
                    //    @"UPDATE otcontract_profile_tokenswithdrawn SET blockchainid = 1 WHERE blockchainid IS null"); //TODO long term needs something better
                    //connection.Execute(
                    //    @"ALTER TABLE otcontract_profile_tokenswithdrawn MODIFY COLUMN `BlockchainID` INT NOT NULL");

                    connection.Execute(@"ALTER TABLE otcontract_replacement_replacementcompleted
ADD COLUMN IF NOT EXISTS `BlockchainID` INT NOT NULL DEFAULT 1");
//                connection.Execute(@"ALTER TABLE `otcontract_replacement_replacementcompleted`
//ADD CONSTRAINT `FK_otcontract_replacement_replacementcompleted_blockchains` FOREIGN KEY IF NOT EXISTS
//(`blockchainid`) REFERENCES `blockchains` (`id`);");
                    //connection.Execute(
                    //    @"UPDATE otcontract_replacement_replacementcompleted SET blockchainid = 1 WHERE blockchainid IS null"); //TODO long term needs something better
                    //connection.Execute(
                    //    @"ALTER TABLE otcontract_replacement_replacementcompleted MODIFY COLUMN `BlockchainID` INT NOT NULL");

                    connection.Execute(@"ALTER TABLE otidentity
ADD COLUMN IF NOT EXISTS `BlockchainID` INT NOT NULL DEFAULT 1");
                    connection.Execute(@"ALTER TABLE `otidentity`
ADD CONSTRAINT `FK_otidentity_blockchains` FOREIGN KEY IF NOT EXISTS
(`blockchainid`) REFERENCES `blockchains` (`id`);");
                    //connection.Execute(@"UPDATE otidentity SET blockchainid = 1 WHERE blockchainid IS null"); //TODO long term needs something better

                    connection.Execute(@"ALTER TABLE otoffer
ADD COLUMN IF NOT EXISTS `BlockchainID` INT NOT NULL DEFAULT 1");
                    connection.Execute(@"ALTER TABLE `otoffer`
ADD CONSTRAINT `FK_otoffer_blockchains` FOREIGN KEY IF NOT EXISTS
(`blockchainid`) REFERENCES `blockchains` (`id`);");
                    //connection.Execute(@"UPDATE otoffer SET blockchainid = 1 WHERE blockchainid IS null"); //TODO long term needs something better

                    connection.Execute(@"ALTER TABLE otoffer_holders
ADD COLUMN IF NOT EXISTS `BlockchainID` INT NOT NULL DEFAULT 1");
                    connection.Execute(@"ALTER TABLE `otoffer_holders`
ADD CONSTRAINT `FK_otoffer_holders_blockchains` FOREIGN KEY IF NOT EXISTS
(`blockchainid`) REFERENCES `blockchains` (`id`);");
                    //connection.Execute(@"UPDATE otoffer_holders SET blockchainid = 1 WHERE blockchainid IS null"); //TODO long term needs something better

                    //After adding all these new columns we have some issues where a few tables use blockchain specific information as their PK
                    //an example of this is ethblock which uses BlockNumber for it's PK. We need to change these Composite keys (BlockchainID, BlockNumber)
                    //this means we have to delete all FKs first and then readd them after we are done... fun!

                    var fkRows = connection.Query(@$"SELECT 
  TABLE_NAME,COLUMN_NAME,CONSTRAINT_NAME, REFERENCED_TABLE_NAME,REFERENCED_COLUMN_NAME
FROM
  INFORMATION_SCHEMA.KEY_COLUMN_USAGE
WHERE
REFERENCED_TABLE_SCHEMA = '{OTHubSettings.Instance.MariaDB.Database}' AND
  REFERENCED_TABLE_NAME = 'ethblock' AND
  REFERENCED_COLUMN_NAME = 'BlockNumber';").ToArray();

                    connection.Open();

                    string sql = null;

                    try
                    {
                        //If we hit an error we want to rollback as it won't be fun to try recover this if we are halfway through deleting or adding FKs
                        using (var tran = connection.BeginTransaction(IsolationLevel.Serializable))
                        {
                            foreach (var row in fkRows)
                            {
                                string tableName = row.TABLE_NAME;
                                string columnName = row.COLUMN_NAME;
                                string fkName = row.CONSTRAINT_NAME;

                                connection.Execute(sql = $"ALTER TABLE {tableName} DROP FOREIGN KEY {fkName}",
                                    transaction: tran);
                                connection.Execute(sql = $"ALTER TABLE {tableName} DROP INDEX IF EXISTS {fkName}",
                                    transaction: tran);
                            }

                            connection.Execute(sql = @"ALTER TABLE ethblock
  DROP PRIMARY KEY,
  ADD PRIMARY KEY (BlockchainID, BlockNumber);", transaction: tran);

                            foreach (var row in fkRows)
                            {
                                string tableName = row.TABLE_NAME;
                                string columnName = row.COLUMN_NAME;
                                string fkName = row.CONSTRAINT_NAME;

                                connection.Execute(sql = @$"ALTER TABLE `{tableName}`
ADD CONSTRAINT `{fkName}` FOREIGN KEY
(`blockchainid`, `{columnName}`) REFERENCES `ethblock` (`BlockchainID`, `BlockNumber`);", transaction: tran);
                            }

                            tran.Commit();
                        }
                    }
                    catch
                    {
                        Console.WriteLine("SQL: " + (sql ?? ""));
                        throw;
                    }

                    connection.Execute("DELETE FROM systemstatus");

                    connection.Execute(@"ALTER TABLE systemstatus
ADD COLUMN IF NOT EXISTS `BlockchainID` INT NULL");

                    connection.Execute(@"ALTER TABLE `systemstatus`
ADD CONSTRAINT `FK_systemstatus_blockchains` FOREIGN KEY IF NOT EXISTS
(`blockchainid`) REFERENCES `blockchains` (`id`);");

                    connection.Execute(@"ALTER TABLE systemstatus
ADD COLUMN IF NOT EXISTS `ParentName` VARCHAR(100) NULL");

                }
            }
        }
    }
}