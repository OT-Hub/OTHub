import { identity } from 'rxjs';

export class OTNodeSummaryModel   {
    // Identity: string;
    NodeId: string;
    Version: number;
    StakeTokens: number;
    StakeReservedTokens: number;
    PaidTokens: number;
    TotalWonOffers: number;
    WonOffersLast7Days: number;
    ActiveOffers: number;
}
