
// import Big from 'big.js';

export class DataHolderDetailedModel   {
    Identity: string;
    NodeId: string;
    Version: number;
    StakeTokens: string;
    StakeReservedTokens: number;
    PaidTokens: number;
    TotalWonOffers: number;
    WonOffersLast7Days: number;
    Approved: boolean;
    OldIdentity: string;
    NewIdentity: string;
    ManagementWallet: string;
    CreateTransactionHash: string;
    CreateGasPrice: number;
    CreateGasUsed: number;

    Offers: DataHolderDetailedOfferModel[];
    Payouts: DataHolderDetailedPayoutModel[];
    ProfileTransfers: DataHolderDetailedProfileTransfer[];
    NodeUptime: DataHolderDetailedNodeUptime;
    Litigations: DataHolderLitigation[];
}

export class DataHolderDetailedOfferModel {
    OfferId: string;
    FinalizedTimestamp: Date;
    HoldingTimeInMinutes: number;
    Paidout: boolean;
    CanPayout: boolean;
    TokenAmountPerHolder: string;
    EndTimestamp: Date;
    Status: string;
}

export class DataHolderDetailedPayoutModel {
    OfferId: string;
    Amount: string;
    Timestamp: Date;
    TransactionHash: string;
    GasUsed: number;
    GasPrice: number;
}

export class DataHolderDetailedProfileTransfer {
    TransactionHash: string;
    Amount: string;
    Timestamp: Date;
    GasPrice: number;
    GasUsed: number;
}

export class DataHolderDetailedNodeUptime {
    LastSuccess: Date;
    LastCheck: Date;
    TotalSuccess24Hours: number;
    TotalFailed24Hours: number;
    TotalSuccess7Days: number;
    TotalFailed7Days: number;
    ChartData: string;
}

export class DataHolderTestOnlineResult {
    Message: string;
    Success: boolean;
    Error: boolean;
    Warning: boolean;
    Header: string;
}

export class DataHolderLitigation {
    TransactionHash: string;
    Timestamp: Date;
    RequestedObjectIndex: number;
    RequestedBlockIndex: number;
    OfferId: string;
    LitigationStatus: number;
}
