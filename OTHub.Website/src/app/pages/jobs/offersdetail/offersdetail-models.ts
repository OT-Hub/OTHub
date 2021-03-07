export class OTOfferDetailModel {
    OfferId: string;
    DataSetId: string;
    CreatedTimestamp: Date;
    DataSetSizeInBytes: number;
    HoldingTimeInMinutes: number;
    TokenAmountPerHolder: number;
    FinalizedTimestamp; Date;
    IsFinalized: boolean;
    Status: string;
    EndTimestamp: Date;
    LitigationIntervalInMinutes: number;
    EstimatedLambda: number;

    CreatedBlockNumber: number;
    CreatedTransactionHash: string;
    FinalizedBlockNumber: number;
    FinalizedTransactionHash: string;
    DCNodeId: string;
    // DCIdentity: string;
    Holders: OTOfferDetailIdentityModel[];
    Timeline: OTOfferDetailTimelineModel[];

    OffersTotal: number;
    OffersLast7Days: number;
    PaidoutTokensTotal: number;

    CreatedGasUsed: number;
    FinalizedGasUsed: number;
    CreatedGasPrice: number;
    FinalizedGasPrice: number;

  BlockchainDisplayName: string;
}

export class OTOfferDetailIdentityModel {
    NodeId: string;
    LitigationStatus: number;
    LitigationStatusText: string;
}

export class OTOfferDetailTimelineModel {
    Name: string;
    Timestamp: Date;
    RelatedTo: string;
    TransactionHash: string;

    Test: string;

    constructor() {
        this.Test = 'hello<br>123';
    }
}
