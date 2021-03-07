export class OTOfferSummaryModel   {
    OfferId: string;
    Timestamp: Date;
    DataSetSizeInBytes: number;
    HoldingTimeInMinutes: number;
    TokenAmountPerHolder: number;
    IsFinalized: boolean;
    Status: string;
    EndTimestamp: Date;
    DCIdentity: string;
    BlockchainDisplayName: string;
}

export class OTOfferSummaryWithPaging   {
    data: OTOfferSummaryModel[];
    draw: number;
    recordsTotal: number;
    recordsFiltered: number;
}
