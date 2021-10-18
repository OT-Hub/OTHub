export class RecentActivityJobModel {
    Identity: string;
    OfferId: string;
    Timestamp: Date;
    TokenAmountPerHolder: number;
    EndTimestamp: Date;
}

export interface TelegramSettings {
    TelegramID: Number;
    NotificationsEnabled: Boolean;
    JobWonEnabled: Boolean;
    LowAvailableTokensEnabled: Boolean;
    HasReceivedMessageFromUser: Boolean;
    LowAvailableTokensAmount: Number;
}