import { Component, OnDestroy, ViewChild } from '@angular/core';
import { takeWhile } from 'rxjs/operators';

import { OrdersChartComponent } from './charts/orders-chart.component';
import { NodesChartComponent } from './charts/nodes-chart.component';
import { OrdersChart } from '../../../@core/data/orders-chart';
import { NodesChart } from '../../../@core/data/nodes-chart';
import { OrderNodesChartSummary, OrdersNodesChartData } from '../../../@core/data/orders-nodes-chart';
import { HubHttpService } from '../../hub-http-service';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { HomeNodesInfoV2SummaryModel, JobChartDataV2SummaryModel } from './charts/OTHomeJobsChartData';

@Component({
  selector: 'ngx-ecommerce-charts',
  styleUrls: ['./charts-panel.component.scss'],
  templateUrl: './charts-panel.component.html',
})
export class ECommerceChartsPanelComponent implements OnDestroy {

  private alive = true;

  chartPanelSummary: OrderNodesChartSummary[];
  period: string = '7 Days';
  ordersChartData: OrdersChart;
  nodesChartData: NodesChart;

  jobsSummary: JobChartDataV2SummaryModel;
  nodesSummary: HomeNodesInfoV2SummaryModel;

  @ViewChild('ordersChart', { static: true }) ordersChart: OrdersChartComponent;
  @ViewChild('nodesChart', { static: true }) nodesChart: NodesChartComponent;

  getJobsSummary() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    const url = this.httpService.ApiUrl + '/api/home/JobsChartDataSummaryV2?' + (new Date()).getTime();
    return this.http.get<JobChartDataV2SummaryModel>(url, { headers });
  }

  getNodesSummary() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    const url = this.httpService.ApiUrl + '/api/home/HomeNodesInfoV2?' + (new Date()).getTime();
    return this.http.get<HomeNodesInfoV2SummaryModel>(url, { headers });
  }

  setJobsSummary() {
    if (this.jobsSummary)
    this.chartPanelSummary = [
      {
        title: 'Active',
        value: this.jobsSummary.OffersActive,
      },
      {
        title: 'Started Last Month',
        value: this.jobsSummary.OffersLastMonth,
      },
      {
        title: 'Started Last Week',
        value: this.jobsSummary.OffersLast7Days,
      },
      {
        title: 'Started Today',
        value: this.jobsSummary.OffersLast24Hours,
      },
    ];
  }

  setNodesSummary() {
    if (this.nodesSummary)
    this.chartPanelSummary = [
      {
        title: 'Online Nodes',
        value: this.nodesSummary.OnlineNodesCount,
      },
      {
        title: 'Nodes with Active Jobs',
        value: this.nodesSummary.NodesWithActiveJobs,
      },
      {
        title: 'Nodes Chosen for Jobs this Week',
        value: this.nodesSummary.NodesWithJobsThisWeek,
      },
      {
        title: 'Nodes Chosen for Jobs this Month',
        value: this.nodesSummary.NodesWithJobsThisMonth,
      },
    ];
  }

  constructor(private ordersNodesChartService: OrdersNodesChartData, private httpService: HubHttpService,
    private http: HttpClient) {


    this.getNodesSummary().pipe(takeWhile(() => this.alive)).subscribe(data => {

      this.nodesSummary = data;

    });

    this.getJobsSummary().pipe(takeWhile(() => this.alive)).subscribe(data => {

      this.jobsSummary = data;

      this.setJobsSummary();

    });

    // this.ordersProfitChartService.getOrderProfitChartSummary()
    //   .pipe(takeWhile(() => this.alive))
    //   .subscribe((summary) => {
    //     debugger;
    //     this.chartPanelSummary = [
    //       {
    //         title: 'Total1',
    //         value: 3654,
    //       },
    //       {
    //         title: 'Started Last Month',
    //         value: 946,
    //       },
    //       {
    //         title: 'Started Last Week',
    //         value: 654,
    //       },
    //       {
    //         title: 'Started Today',
    //         value: 230,
    //       },
    //     ];
    //   });

    this.getOrdersChartData(this.period);
    this.getNodesChartData(this.period);
  }

  setPeriodAndGetChartData(value: string): void {
    if (this.period !== value) {
      this.period = value;
    }

    this.getOrdersChartData(value);
    this.getNodesChartData(value);
  }

  changeTab(selectedTab) {
    if (selectedTab.tabTitle === 'Nodes') {
      this.setNodesSummary();
      this.nodesChart.resizeChart();
    } else {
      this.setJobsSummary();
      this.ordersChart.resizeChart();
    }
  }

  getOrdersChartData(period: string) {
    this.ordersNodesChartService.getOrdersChartData(period)
      .pipe(takeWhile(() => this.alive))
      .subscribe(ordersChartData => {
        ordersChartData.period = period;
        this.ordersChartData = ordersChartData;
      });
  }

  getNodesChartData(period: string) {
    this.ordersNodesChartService.getNodesChartData(period)
      .pipe(takeWhile(() => this.alive))
      .subscribe(nodesChartData => {
        nodesChartData.period = period;
        this.nodesChartData = nodesChartData;
      });
  }

  ngOnDestroy() {
    this.alive = false;
  }
}
