import {Component, OnDestroy, OnInit, ViewChild} from '@angular/core';
import { Inject, NgZone, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import {NbThemeService, NbMediaBreakpoint, NbMediaBreakpointsService, NbPopoverDirective} from '@nebular/theme';
import {SystemStatusModel} from "../system/status/system-models";
import {HttpClient, HttpHeaders} from "@angular/common/http";
import {MyNodeService} from "../nodes/mynodeservice";
import {HubHttpService} from "../hub-http-service";
import * as moment from 'moment';
import * as am4core from '@amcharts/amcharts4/core';
import * as am4charts from '@amcharts/amcharts4/charts';
import am4themes_animated from '@amcharts/amcharts4/themes/animated';

@Component({
  selector: 'ngx-ecommerce',
  templateUrl: './e-commerce.component.html',
  styleUrls: ['./e-commerce.component.scss'],
})
export class ECommerceComponent implements OnDestroy, OnInit {
  breakpoint: NbMediaBreakpoint;
  breakpoints: any;
  themeSubscription: any;
  getDataObservable: any;
  Data: HomeV3Model;
  failedLoading: boolean;
  isLoading: boolean;


  private chart: am4charts.XYChart;
  JobsChartData: HomeJobsChartDataModel[];

  constructor(private themeService: NbThemeService,
              private breakpointService: NbMediaBreakpointsService,
              private httpService: HubHttpService,
              private http: HttpClient,
              @Inject(PLATFORM_ID) private platformId, private zone: NgZone) {

    this.breakpoints = this.breakpointService.getBreakpointsMap();
    this.themeSubscription = this.themeService.onMediaQueryChange()
      .subscribe(([oldValue, newValue]) => {
        this.breakpoint = newValue;
      });
  }

  browserOnly(f: () => void) {
    if (isPlatformBrowser(this.platformId)) {
      this.zone.runOutsideAngular(() => {
        f();
      });
    }
  }

  ngAfterViewInit() {

    var that = this;
    // Chart code goes in here
    this.browserOnly(() => {
      am4core.useTheme(am4themes_animated);

      this.getJobsChartData().subscribe(chartData => {
        const endTime = new Date();
        this.JobsChartData = chartData;
        this.failedLoading = false;
        this.isLoading = false;

        let chart = am4core.create("chartdiv", am4charts.XYChart);

        chart.paddingRight = 20;

        let data = [];

        chartData.forEach((v) => {
          data.push({ date: v.Date, name: "name", newJobs: v.NewJobs, completedJobs: v.CompletedJobs });
        });

        // let visits = 10;
        // for (let i = 1; i < 366; i++) {
        //   visits += Math.round((Math.random() < 0.5 ? 1 : -1) * Math.random() * 10);
        //   data.push({ date: new Date(2018, 0, i), name: "name" + i, value: visits });
        // }

        chart.data = data;
        chart.legend = new am4charts.Legend();
        chart.legend.maxHeight = 150;
        chart.legend.scrollable = true;
        chart.legend.useDefaultMarker = true;

        let dateAxis = chart.xAxes.push(new am4charts.DateAxis());
        dateAxis.renderer.grid.template.location = 0;


        let valueAxis = chart.yAxes.push(new am4charts.ValueAxis());
        valueAxis.tooltip.disabled = true;
        valueAxis.renderer.minWidth = 35;
        valueAxis.title.text = 'Jobs';

        let series = chart.series.push(new am4charts.LineSeries());
        series.dataFields.dateX = "date";
        series.dataFields.valueY = "newJobs";
        series.tooltipText = "{valueY.value}";
        series.name = 'New Jobs';
        series.stroke = am4core.color('#00d68f');
        series.fill = am4core.color('#00d68f');

        let series2 = chart.series.push(new am4charts.LineSeries());
        series2.dataFields.dateX = "date";
        series2.dataFields.valueY = "completedJobs";
        series2.tooltipText = "{valueY.value}";
        series2.name = 'Completed Jobs';


        chart.cursor = new am4charts.XYCursor();

        // let scrollbarX = new am4charts.XYChartScrollbar();
        // scrollbarX.series.push(series);
        // scrollbarX.series.push(series2);
        // chart.scrollbarX = scrollbarX;

        this.chart = chart;
      }, err => {
        this.failedLoading = true;
        this.isLoading = false;
      });
    });
  }

  ngOnDestroy() {
    this.browserOnly(() => {
      if (this.chart) {
        this.chart.dispose();
      }
    });

    this.themeSubscription.unsubscribe();
    this.getDataObservable.unsubscribe();
  }


  getHomeData() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    const url = this.httpService.ApiUrl + '/api/home/HomeV3';
    return this.http.get<HomeV3Model>(url, { headers });
  }

  getJobsChartData() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    const url = this.httpService.ApiUrl + '/api/home/JobsChartDataV3';
    return this.http.get<HomeJobsChartDataModel[]>(url, { headers });
  }


  formatTime(time: number) {
    if (time == null) {
      return '?';
    }
    return moment.duration(time, 'minutes').humanize();
  }

  ngOnInit() {
    this.getDataObservable = this.getHomeData().subscribe(data => {
      const endTime = new Date();
      this.Data = data;
      this.failedLoading = false;
      this.isLoading = false;
    }, err => {
      this.failedLoading = true;
      this.isLoading = false;
    });
  }

  formatAmount(amount) {
    if (amount === null) {
      return null;
    }
    const split = amount.toString().split('.');
    let lastSplit = '';
    if (split.length === 2) {
      lastSplit = split[1];
      if (lastSplit.length > 3) {
        lastSplit = lastSplit.substring(0, 3);
      }
      return split[0] + '.' + lastSplit;
    }
    return split[0];
  }
}

export class HomeV3Model {
TotalJobs: number;
ActiveNodes: number;
ActiveJobs: number;
Jobs24H: number;
JobsReward24H: number;
JobsDuration24H: number;
JobsSize24H: number;
StakedTokens: string;
  PercentChange24H: number;
  PriceUsd: number;
  CirculatingSupply: number;
FeesByBlockchain: HomeFeesByBlockchainModel[];
StakedByBlockchain: HomeStakedTokensByBlockchainModel[];
}
export class HomeFeesByBlockchainModel {
  BlockchainName: string;
  NetworkName: string;
  ShowCostInUSD: boolean;
  JobCreationCost: number;
  JobFinalisedCost: number;
  PayoutCost: number;
}

export class HomeStakedTokensByBlockchainModel {
  BlockchainName: string;
  NetworkName: string;
  StakedTokens: string;
}

export class HomeJobsChartDataModel {
  Label: string;
  Date: Date;
  NewJobs: number;
  CompletedJobs: number;
}
