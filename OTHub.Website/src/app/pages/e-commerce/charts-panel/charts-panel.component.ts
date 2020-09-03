import { Component, OnDestroy, ViewChild } from '@angular/core';
import { takeWhile } from 'rxjs/operators';

import { OrdersChartComponent } from './charts/orders-chart.component';
import { ProfitChartComponent } from './charts/profit-chart.component';
import { OrdersChart } from '../../../@core/data/orders-chart';
import { ProfitChart } from '../../../@core/data/profit-chart';
import { OrderProfitChartSummary, OrdersProfitChartData } from '../../../@core/data/orders-profit-chart';
import { HubHttpService } from '../../hub-http-service';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { JobChartDataV2SummaryModel } from './charts/OTHomeJobsChartData';

@Component({
  selector: 'ngx-ecommerce-charts',
  styleUrls: ['./charts-panel.component.scss'],
  templateUrl: './charts-panel.component.html',
})
export class ECommerceChartsPanelComponent implements OnDestroy {

  private alive = true;

  chartPanelSummary: OrderProfitChartSummary[];
  period: string = '7 Days';
  ordersChartData: OrdersChart;
  profitChartData: ProfitChart;

  @ViewChild('ordersChart', { static: true }) ordersChart: OrdersChartComponent;
  @ViewChild('profitChart', { static: true }) profitChart: ProfitChartComponent;

  getJobsChartData() {
		const headers = new HttpHeaders()
			.set('Content-Type', 'application/json')
			.set('Accept', 'application/json');
		const url = this.httpService.ApiUrl + '/api/home/JobsChartDataSummaryV2?' + (new Date()).getTime();
		return this.http.get<JobChartDataV2SummaryModel>(url, { headers });
	}


  constructor(private ordersProfitChartService: OrdersProfitChartData, private httpService: HubHttpService,
    private http: HttpClient) {


      this.getJobsChartData().pipe(takeWhile(() => this.alive)).subscribe(data => {


        this.chartPanelSummary = [
          {
            title: 'Active',
            value: data.OffersActive,
          },
          {
            title: 'Started Last Month',
            value: data.OffersLastMonth,
          },
          {
            title: 'Started Last Week',
            value: data.OffersLast7Days,
          },
          {
            title: 'Started Today',
            value: data.OffersLast24Hours,
          },
        ];

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
    this.getProfitChartData(this.period);
  }

  setPeriodAndGetChartData(value: string): void {
    if (this.period !== value) {
      this.period = value;
    }

    this.getOrdersChartData(value);
    this.getProfitChartData(value);
  }

  changeTab(selectedTab) {
    if (selectedTab.tabTitle === 'Profit') {
      this.profitChart.resizeChart();
    } else {
      this.ordersChart.resizeChart();
    }
  }

  getOrdersChartData(period: string) {
    this.ordersProfitChartService.getOrdersChartData(period)
      .pipe(takeWhile(() => this.alive))
      .subscribe(ordersChartData => {
        ordersChartData.period = period;
        this.ordersChartData = ordersChartData;
      });
  }

  getProfitChartData(period: string) {
    this.ordersProfitChartService.getProfitChartData(period)
      .pipe(takeWhile(() => this.alive))
      .subscribe(profitChartData => {
        this.profitChartData = profitChartData;
      });
  }

  ngOnDestroy() {
    this.alive = false;
  }
}
