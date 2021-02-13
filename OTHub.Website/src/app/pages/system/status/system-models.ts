export class SystemStatusItemModel   {
    LastSuccessDateTime: Date;
    LastTriedDateTime: Date;
    Success: boolean;
    Name: string;
    BlockchainName: string;
    NetworkName: string;
}

export class SystemStatusGroupModel   {
    Items: SystemStatusItemModel[];
}

export class SystemStatusModel   {
    Groups: SystemStatusGroupModel[];
}

