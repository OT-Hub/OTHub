export class SmartContractItemModel   {
    BlockchainName: string;
    NetworkName: string;
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
}