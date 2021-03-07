export class SmartContractItemModel   {
  BlockchainDisplayName: string;
    Address: string;
    Type: number;
    IsLatest: boolean;
    FromBlockNumber: number;
    IsArchived: boolean;
    SyncBlockNumber: number;
    LastSyncedTimestamp: Date;
}

export class SmartContractGroupModel   {
    Items: SmartContractItemModel[];
    Name: string;
    HubAddress: string;
}
