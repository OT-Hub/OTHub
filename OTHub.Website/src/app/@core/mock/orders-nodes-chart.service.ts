import { of as observableOf,  Observable } from 'rxjs';
import { Injectable } from '@angular/core';
import { OrdersChart, OrdersChartData } from '../data/orders-chart';
import { OrderNodesChartSummary, OrdersNodesChartData } from '../data/orders-nodes-chart';
import { NodesChart, NodesChartData } from '../data/nodes-chart';

@Injectable()
export class OrdersNodesChartService extends OrdersNodesChartData {

  private summary = [
    {
      title: 'Total',
      value: 3654,
    },
    {
      title: 'Started Last Month',
      value: 946,
    },
    {
      title: 'Started Last Week',
      value: 654,
    },
    {
      title: 'Started Today',
      value: 230,
    },
  ];

  constructor(private ordersChartService: OrdersChartData,
              private nodesChartService: NodesChartData) {
    super();
  }

  getOrderProfitChartSummary(): Observable<OrderNodesChartSummary[]> {
    return observableOf(this.summary);
  }

  getOrdersChartData(period: string): Observable<OrdersChart> {
    return observableOf(this.ordersChartService.getOrdersChartData(period));
  }

  getNodesChartData(period: string): Observable<NodesChart> {
    return observableOf(this.nodesChartService.getNodesChartData(period));
  }
}
