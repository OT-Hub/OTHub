export class SystemStatusItemModel   {
    LastSuccessDateTime: Date;
    LastTriedDateTime: Date;
    Success: boolean;
    Name: string;
  BlockchainDisplayName: string;

    Children: SystemStatusItemModel[];
}

export class SystemStatusGroupModel   {
    Items: SystemStatusItemModel[];
}

export class SystemStatusModel   {
    Groups: SystemStatusGroupModel[];
}

