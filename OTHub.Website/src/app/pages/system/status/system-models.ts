export class SystemStatusItemModel   {
    LastSuccessDateTime: Date;
    LastTriedDateTime: Date;
    Success: boolean;
    Name: string;
    BlockchainName: string;
    NetworkName: string;
}

export class SystemStatusModel   {
    Items: SystemStatusItemModel[];
}
