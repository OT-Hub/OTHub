import { Observable } from 'rxjs';
import { OrdersChart } from './orders-chart';
import { NodesChart  } from './nodes-chart';

export interface OrderNodesChartSummary {
  title: string;
  value: number;
}

export abstract class OrdersNodesChartData {
  abstract getOrderProfitChartSummary(): Observable<OrderNodesChartSummary[]>;
  abstract getOrdersChartData(period: string): Observable<OrdersChart>;
  abstract getNodesChartData(period: string): Observable<NodesChart>;
}
