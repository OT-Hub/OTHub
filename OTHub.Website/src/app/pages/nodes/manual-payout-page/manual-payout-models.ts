export class BeforePayoutResult {
    CanTryPayout: boolean;
    Header: string;
    Message: string;
    BlockchainExplorerUrlFormat: string;
    EstimatedPayout: number;
}

export class ContractAddress {
    Address: string;
    IsLatest: boolean;
}

export class RecentPayoutGasPrice {
    GasPrice: number;
    GasUsed: number;
    TotalCount: number;
}
