export class OTHomeJobsChartData {
    Week: number[][];
    Month: number[][];
    Year: number[][];

    WeekLabels: string[];
    MonthLabels: string[];
    YearLabels: string[];
}

export class OTHomeNodesChartData {
    Week: number[][];
    Month: number[][];
    Year: number[][];

    WeekLabels: string[];
    MonthLabels: string[];
    YearLabels: string[];
}

export class JobChartDataV2SummaryModel {
    OffersActive: number;
    OffersLast24Hours: number;
    OffersLast7Days: number;
    OffersLastMonth: number;
}


export class HomeNodesInfoV2SummaryModel {
    OnlineNodesCount: number;
    NodesWithActiveJobs: number;
    NodesWithJobsThisWeek: number;
    NodesWithJobsThisMonth: number;
}