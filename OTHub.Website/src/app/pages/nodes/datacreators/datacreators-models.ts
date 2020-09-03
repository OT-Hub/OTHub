
export class DataCreatorSummaryModel   {
    Identity: string;
    NodeId: string;
    Version: number;
    StakeTokens: number;
    StakeReservedTokens: number;
    Approved: boolean;
    OffersTotal: number;
    OffersLast7Days: number;
    AvgDataSetSizeKB: number;
    AvgHoldingTimeInMinutes: number;
    AvgTokenAmountPerHolder: number;
    CreatedTimestamp: Date;
}
