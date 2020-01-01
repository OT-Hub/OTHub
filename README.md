# OT Hub

OT Hub is a community-made project for live insights into the OriginTrail Decentralized Network.

https://othub.origin-trail.network/

This repository contains the web server API that powers the OT Hub website and the backend synchronisation process that connects to the Ethereum Blockchain.

Due to licencing issues the website built with Angular is not currently included in this repository.

## Code Projects
- OTHub.ApiServer - This project is the web server API
- OTHub.BackendSync - This project is the backend synchronisation process 
- OTHub.Settings - This project contains the code for the settings described below

## API Documentation

https://othub-api.origin-trail.network/docs

## Configuration

The configuration uses User Secrets which are stored in the authenticated users profile for security.

#### Required
- Infura:Url - This is the full https URL from Infura used to connect to the Ethereum Blockchain
- Blockchain:Network - Either Mainnet or Testnet
- Blockchain:HubAddress - Leave this as 0xa287d7134fb40bef071c932286bd2cd01efccf30 which is the current Mainnet hub address (correct as of November 2019).
- Blockchain:StartSyncFromBlockNumber - The Ethereum block number to start the sync from if syncing from scratch. 6655078 is the recommended block number for mainnet to capture all data.
- MariaDB:Server - The server IP or hostname of the MariaDB.
- MariaDB:Database - The database name for the MariaDB.
- MariaDB:UserID - The user ID for the MariaDB.
- MariaDB:Password - The password for the MariaDB.
- WebServer:AccessControlAllowOrigin - The URL to allow browser requests from (CORS). This can be left as https://localhost:4200 unless you plan on hosting your own website using the API.
#### Optional
- OriginTrailNode:Url - This can be used to perform uptime/online checks

#### Example

```
{
  "Infura:Url": "https://mainnet.infura.io/v3/xxxx",
  "Blockchain:Network": "Mainnet",
  "Blockchain:HubAddress": "0xa287d7134fb40bef071c932286bd2cd01efccf30",
  "Blockchain:StartSyncFromBlockNumber": 6655078,
  "OriginTrailNode:Url": "http://xx.xx.xx.xx:8900",
  "MariaDB:Server": "127.0.0.1",
  "MariaDB:Database": "xx",
  "MariaDB:UserID": "xx",
  "MariaDB:Password": "xx",
  "WebServer:AccessControlAllowOrigin": "https://localhost:4200"
}
```

## Installation
Coming soon
