export interface NodesChart {
  period: string;
  chartLabel: string[];
  data: number[][];
}

export abstract class NodesChartData {
  abstract getNodesChartData(period: string): NodesChart;
}
