import {Component, OnDestroy, OnInit, ViewChild} from '@angular/core';
import { Inject, NgZone, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import {NbThemeService, NbMediaBreakpoint, NbMediaBreakpointsService, NbPopoverDirective} from '@nebular/theme';
import {HttpClient, HttpHeaders} from "@angular/common/http";
import {HubHttpService} from "../hub-http-service";
import * as moment from 'moment';
import * as am4core from '@amcharts/amcharts4/core';
import * as am4charts from '@amcharts/amcharts4/charts';
import am4themes_animated from '@amcharts/amcharts4/themes/animated';

@Component({
  selector: 'ngx-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss'],
})
export class HomeComponent implements OnDestroy, OnInit {
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


  loadJobOMeterChart() {

  }


  loadJobBlockchainDistributionChart() {

    this.get24HJobBlockchainDistribution().subscribe(chartData => {

      this.failedLoading = false;
      this.isLoading = false;

      //let chart = am4core.create("JobBlockchainDistributionChart", am4charts.XYChart);
      //chart.paddingRight = 20;

      let data = [];

      let percent = 0;
      let jobs = 0;
      chartData.Blockchains.forEach((v) => {
        data.push({
          "category": "",
          "from": percent,
          "to": percent + v.Percentage,
          "name": v.DisplayName,
          "fill": am4core.color("#" + v.Color),
          "jobsFrom": jobs,
          "jobsTo": jobs + v.Jobs
        });
        percent += v.Percentage;
        jobs += v.Jobs;
      });
      
      let chart = am4core.create("JobOMeterChart", am4charts.GaugeChart);
      chart.hiddenState.properties.opacity = 0; // this makes initial fade in effect
      
      chart.innerRadius = -25;

      let max = chartData.MaxDailyJobs;

      if (max < chartData.TotalJobs) {
        max = chartData.TotalJobs;
      }

      let axis: am4charts.ValueAxis;
      axis = chart.xAxes.push(new am4charts.ValueAxis());
      axis.min = 0;
      axis.max = max;
      axis.strictMinMax = true;
      axis.renderer.grid.template.stroke = new am4core.InterfaceColorSet().getFor("background");
      axis.renderer.grid.template.strokeOpacity = 0.3;
      
      for (let index = 0; index < data.length; index++) {
        const element = data[index];
        
        let range0 = axis.axisRanges.create();
        range0.value = element["jobsFrom"];
        range0.endValue = element["jobsTo"];
        range0.axisFill.fillOpacity = 1;
        range0.axisFill.fill = element["fill"];
        range0.axisFill.zIndex = - 1;
      }
    
      
      let hand = chart.hands.push(new am4charts.ClockHand());
      hand.value = chartData.TotalJobs;
  
      let title = chart.titles.create();
      title.text = "24h Job-O-Meter";
      title.fontSize = 18;
      
      title.marginBottom = 35;
  
      let legend = new am4charts.Legend();
      legend.isMeasured = true;
      legend.y = am4core.percent(100);
      legend.parent = chart.chartContainer;
      legend.data = data;
  
      if (this.themeService.currentTheme != 'light' && this.themeService.currentTheme != 'corporate' && this.themeService.currentTheme != 'default') {
        title.fill = am4core.color('white');
        axis.renderer.labels.template.fill = am4core.color('white');
        hand.fill = am4core.color('white');
        legend.labels.template.fill = am4core.color('white');
      }
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
      that.loadJobOMeterChart();
      that.loadJobsChart();
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
    const url = this.httpService.ApiUrl + '/api/home/HomeV3?excludeBreakdown=true';
    return this.http.get<HomeV3Model>(url, { headers });
  }

  get24HJobBlockchainDistribution() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    const url = this.httpService.ApiUrl + '/api/home/24HJobBlockchainDistribution';
    return this.http.get<HomeJobBlockchainDistributionSummaryModel>(url, { headers });
  }

  formatTime(time: number) {
    if (time == null) {
      return '?';
    }
    return moment.duration(time, 'minutes').humanize();
  }



  getJobsChartData() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    const url = this.httpService.ApiUrl + '/api/home/JobsChartDataV3';
    return this.http.get<HomeJobsChartDataModel[]>(url, { headers });
  }

  loadJobsChart() {
    this.getJobsChartData().subscribe(chartData => {
      const endTime = new Date();
      this.failedLoading = false;
      this.isLoading = false;

      let chart = am4core.create("JobsHistoryChart", am4charts.XYChart);

      chart.paddingRight = 20;

      let data = [];

      chartData.forEach((v) => {
        data.push({ date: v.Date, name: "name", newJobs: v.NewJobs, completedJobs: v.CompletedJobs });
      });


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
      series.name = 'Started';
      series.stroke = am4core.color('#00d68f');
      series.fill = am4core.color('#00d68f');
      series.strokeWidth = 3;
      // series.stroke = am4core.color('#5a03fc');
      // series.fill = am4core.color('#5a03fc');
      // series.strokeWidth = 1;
      series.fillOpacity = 0.2;

      let series2 = chart.series.push(new am4charts.LineSeries());
      series2.dataFields.dateX = "date";
      series2.dataFields.valueY = "completedJobs";
      series2.tooltipText = "{valueY.value}";
      series2.name = 'Completed';
      series2.strokeWidth = 3;
      series2.fillOpacity = 0.2;

   

      chart.cursor = new am4charts.XYCursor();
      chart.cursor.behavior = "panXY";
      chart.cursor.xAxis = dateAxis;
    
      //chart.cursor.snapToSeries = series;

      let scrollbarX = new am4charts.XYChartScrollbar();
      scrollbarX.series.push(series);
      scrollbarX.series.push(series2);
      chart.scrollbarX = scrollbarX;
      scrollbarX.parent = chart.chartAndLegendContainer;



      chart.events.on('ready', () => {
        series2.hide();
        let start = new Date();
        start.setDate(start.getDate() - 150);
        let end = new Date();
        dateAxis.zoomToDates(start, end);
      });

      let title = chart.titles.create();


      if (this.themeService.currentTheme != 'light' && this.themeService.currentTheme != 'corporate' && this.themeService.currentTheme != 'default') {
        title.fill = am4core.color('white');
        dateAxis.renderer.labels.template.fill = am4core.color('white');
        valueAxis.renderer.labels.template.fill = am4core.color('white');
        chart.legend.labels.template.fill = am4core.color('white');
      }


      title.text = "Jobs";
      title.fontSize = 18;
      title.marginBottom = 15;
    }, err => {
      this.failedLoading = true;
      this.isLoading = false;
    });
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
      while(lastSplit[lastSplit.length - 1] == '0') {
        lastSplit = lastSplit.substr(0, lastSplit.length - 1);
      }
      if (lastSplit == '') {
        return split[0];
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
  PriceFactorLow24H: number;
  PriceFactorHigh24H: number;
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

export class HomeJobBlockchainDistributionSummaryModel {

  Blockchains: HomeJobBlockchainDistributionModel[];
  TotalJobs: number;
  MaxDailyJobs: number;
}

// export class HomeJobsModel {
//   BlockchainName: string;
//   Jobs: number;
// }
