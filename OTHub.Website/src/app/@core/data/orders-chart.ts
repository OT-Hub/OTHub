export interface OrdersChart {
  period: string;
  chartLabel: string[];
  linesData: number[][];
}

export abstract class OrdersChartData {
  abstract getOrdersChartData(period: string): OrdersChart;
}
