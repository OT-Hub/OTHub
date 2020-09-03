export class GlobalActivityModel   {
    Timestamp: Date;
    EventName: string;
    RelatedEntity: string;
    RelatedEntity2: string;
    TransactionHash: string;
    Message: string;
}

export class GlobalActivityModelWithPaging   {
    data: GlobalActivityModel[];
    draw: number;
    recordsTotal: number;
    recordsFiltered: number;
}