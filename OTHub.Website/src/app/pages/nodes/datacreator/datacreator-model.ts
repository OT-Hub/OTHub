import { OTOfferSummaryModel } from '../../jobs/offers/offers-models';
import {DataholderIdentityModel} from "../dataholder/dataholder-models";

export class DataCreatedDetailedModel   {
    // Identity: string;
    NodeId: string;
    Version: number;
    StakeTokens: number;
    StakeReservedTokens: number;
    Approved: boolean;
    OldIdentity: string;
    NewIdentity: string;
    ManagementWallet: string;
    CreateTransactionHash: string;
    CreateGasPrice: number;
    CreateGasUsed: number;

    Offers: OTOfferSummaryModel[];
    ProfileTransfers: DataHolderDetailedProfileTransfer[];
    Litigations: DataCreatorLitigation[];

    BlockchainName: string;
    NetworkName: string;
  Identities: DataholderIdentityModel[];

}



export class DataHolderDetailedProfileTransfer {
    TransactionHash: string;
    Amount: number;
    Timestamp: Date;
    GasPrice: number;
    GasUsed: number;
}

export class DataCreatorLitigation {
    TransactionHash: string;
    Timestamp: Date;
    RequestedObjectIndex: number;
    RequestedBlockIndex: number;
    OfferId: string;
    LitigationStatus: number;
    // HolderIdentity: string;
  NodeId: string;
}
