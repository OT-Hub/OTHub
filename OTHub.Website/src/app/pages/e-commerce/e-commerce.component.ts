import {Component, OnDestroy, OnInit, ViewChild} from '@angular/core';
import { Inject, NgZone, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import {NbThemeService, NbMediaBreakpoint, NbMediaBreakpointsService, NbPopoverDirective} from '@nebular/theme';
import {SystemStatusModel} from "../system/status/system-models";
import {HttpClient, HttpHeaders} from "@angular/common/http";
import {HubHttpService} from "../hub-http-service";
import * as moment from 'moment';
import * as am4core from '@amcharts/amcharts4/core';
import * as am4charts from '@amcharts/amcharts4/charts';
import am4themes_animated from '@amcharts/amcharts4/themes/animated';
import {AxisDataItem, DateAxisDataItem} from "@amcharts/amcharts4/charts";

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
  noJobsIn24H: boolean;


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



  loadJobBlockchainDistributionChart() {
    this.get24HJobBlockchainDistribution().subscribe(chartData => {
      const endTime = new Date();
      this.failedLoading = false;
      this.isLoading = false;

      let chart = am4core.create("JobBlockchainDistributionChart", am4charts.XYChart);
      //chart.paddingRight = 20;

      let data = [];

      let percent = 0;
      chartData.forEach((v) => {
        data.push({
          "category": "",
          "from": percent,
          "to": percent + v.Percentage,
          "name": v.DisplayName,
          "fill": am4core.color("#" + v.Color)
        });
        percent += v.Percentage;
      });

      if (percent === 0) {
        this.noJobsIn24H = true;
      }

      // let visits = 10;
      // for (let i = 1; i < 366; i++) {
      //   visits += Math.round((Math.random() < 0.5 ? 1 : -1) * Math.random() * 10);
      //   data.push({ date: new Date(2018, 0, i), name: "name" + i, value: visits });
      // }

      //chart.data = data;


      chart.data = data;


      // Create axes
      var yAxis = chart.yAxes.push(new am4charts.CategoryAxis());
      yAxis.dataFields.category = "category";
      yAxis.renderer.grid.template.disabled = true;
      yAxis.renderer.labels.template.disabled = true;

      var xAxis = chart.xAxes.push(new am4charts.ValueAxis());
      xAxis.renderer.grid.template.disabled = true;
      xAxis.renderer.grid.template.disabled = true;
      xAxis.renderer.labels.template.disabled = true;
      xAxis.min = 0;
      xAxis.max = 100;

// Create series
      var series = chart.series.push(new am4charts.ColumnSeries());
      series.dataFields.valueX = "to";
      series.dataFields.openValueX = "from";
      series.dataFields.categoryY = "category";
      series.columns.template.propertyFields.fill = "fill";
      series.columns.template.strokeOpacity = 0;
      series.columns.template.height = am4core.percent(100);

// Ranges/labels
      chart.events.on("beforedatavalidated", function(ev) {
        var data = chart.data;
        for(var i = 0; i < data.length; i++) {
          var range = xAxis.axisRanges.create();
          range.value = data[i].to;
          range.label.text = data[i].to - data[i].from + "%";
          range.label.horizontalCenter = "right";
          range.label.paddingLeft = 5;
          range.label.paddingTop = 5;
          range.label.fontSize = 10;
          range.grid.strokeOpacity = 0.2;
          range.tick.length = 18;
          range.tick.strokeOpacity = 0.2;
        }
      });

// Legend
      var legend = new am4charts.Legend();
      legend.parent = chart.chartContainer;
      legend.itemContainers.template.clickable = false;
      legend.itemContainers.template.focusable = false;
      legend.itemContainers.template.cursorOverStyle = am4core.MouseCursorStyle.default;
      legend.align = "right";
      legend.data = chart.data;

      let title = chart.titles.create();
      title.text = "24h Job Distribution";
      title.fontSize = 18;
      title.marginBottom = 15;

      this.chart = chart;
    }, err => {
      this.failedLoading = true;
      this.isLoading = false;
    });
  }

  ngAfterViewInit() {
    var that = this;
    // Chart code goes in here
    this.browserOnly(() => {
      am4core.useTheme(am4themes_animated);
      that.loadJobBlockchainDistributionChart();
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

  get24HJobBlockchainDistribution() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    const url = this.httpService.ApiUrl + '/api/home/24HJobBlockchainDistribution';
    return this.http.get<HomeJobBlockchainDistributionModel[]>(url, { headers });
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

  PercentChange24H: number;
  PriceUsd: number;
  CirculatingSupply: number;
  MarketCapUsd: number;
  Volume24HUsd: number;
  PriceBtc: number;
// FeesByBlockchain: HomeFeesByBlockchainModel[];
// StakedByBlockchain: HomeStakedTokensByBlockchainModel[];
// JobBlockchainDistribution: HomeJobBlockchainDistributionModel[];
// TotalJobsByBlockchain: HomeJobsModel[];
// Jobs24HByBlockchain: HomeJobsModel[];
All: HomeV3BlockchainModel;
Blockchains: HomeV3BlockchainModel[];
}

export class HomeV3BlockchainModel {
  LogoLocation: string;
  TotalJobs: number;
  ActiveNodes: number;
  ActiveJobs: number;
  Jobs24H: number;
  JobsReward24H: number;
  JobsDuration24H: number;
  JobsSize24H: number;
  TokensLocked24H: number;
  TokensPaidout24H: number;
  StakedTokens: string;
  GasTicker: string;
  TokenTicker: string;
  Fees: HomeFeesByBlockchainModel;
  HoursTillFirstJob: number;
}

export class HomeFeesByBlockchainModel {
  BlockchainName: string;
  ShowCostInUSD: boolean;
  JobCreationCost: number;
  JobFinalisedCost: number;
  PayoutCost: number;
}

// export class HomeStakedTokensByBlockchainModel {
//   BlockchainName: string;
//   StakedTokens: string;
// }

export class HomeJobsChartDataModel {
  Label: string;
  Date: Date;
  NewJobs: number;
  CompletedJobs: number;
}

export class HomeJobBlockchainDistributionModel {
  DisplayName: string;
  Color: string;
  Jobs: number;
  Percentage: number;
}

// export class HomeJobsModel {
//   BlockchainName: string;
//   Jobs: number;
// }
