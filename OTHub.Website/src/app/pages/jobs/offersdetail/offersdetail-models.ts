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
    DcDisplayName: string;
    // DCIdentity: string;
    Holders: OTOfferDetailIdentityModel[];
    TimelineEvents: OTOfferDetailTimelineEventModel[];

    OffersTotal: number;
    OffersLast7Days: number;
    PaidoutTokensTotal: number;

    CreatedGasUsed: number;
    FinalizedGasUsed: number;
    CreatedGasPrice: number;
    FinalizedGasPrice: number;

  BlockchainDisplayName: string;
  GasTicker: string;
}

export class OTOfferDetailIdentityModel {
    NodeId: string;
    LitigationStatus: number;
    LitigationStatusText: string;
  JobStarted: Date;
  JobCompleted: Date;
  DisplayName: string;
}

export class OTOfferDetailTimelineEventModel {
    Name: string;
    Timestamp: Date;
    RelatedTo: string;
    TransactionHash: string;

    Test: string;

    constructor() {
        this.Test = 'hello<br>123';
    }
}
