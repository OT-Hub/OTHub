export class SystemStatusItemModel   {
    LastSuccessDateTime: Date;
    LastTriedDateTime: Date;
    Success: boolean;
    Name: string;
}

export class SystemStatusModel   {
    Items: SystemStatusItemModel[];
}
