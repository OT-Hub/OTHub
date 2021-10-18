import { Component, HostListener, Inject, NgZone, OnInit, PLATFORM_ID } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { HubHttpService } from 'app/pages/hub-http-service';
import { isPlatformBrowser } from '@angular/common';
import * as am4core from '@amcharts/amcharts4/core';
import * as am4charts from '@amcharts/amcharts4/charts';
import am4themes_animated from '@amcharts/amcharts4/themes/animated';
import { Blockchain } from 'app/pages/tools/find-nodes-by-wallet/find-nodes-by-wallet.component';
import { NbThemeService } from '@nebular/theme';

@Component({
  selector: 'ngx-holding-time-per-month',
  templateUrl: './holding-time-per-month.component.html',
  styleUrls: ['./holding-time-per-month.component.scss']
})
export class HoldingTimePerMonthComponent implements OnInit {
  blockchains: Blockchain[];

  constructor(private zone: NgZone,
    @Inject(PLATFORM_ID) private platformId, private http: HttpClient, private httpService: HubHttpService,
    private themeService: NbThemeService) { 
      this.selectedBlockchain = 'All Blockchains';
      this.getScreenSize();
    }

  ngOnInit(): void {
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
      that.loadChart();
    });
  }

  getBlockchains() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    let url = this.httpService.ApiUrl + '/api/blockchain/GetBlockchains';

    return this.http.get<Blockchain[]>(url, { headers });
  }

  getData() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    const url = this.httpService.ApiUrl + '/api/reports/HoldingTimePerMonth';
    return this.http.get<HoldingTimePerMonthReportModel>(url, { headers });
  }

  createTopChart(chartData: HoldingTimePerMonthReportModel) {
    let chart = am4core.create("Chart", am4charts.XYChart);

    let data = [];

    let yearsFound = [];

    chartData.Data.forEach((v) => {

      let row = data.find(d => d.year == v.Year && d.month == v.Month);
      if (row == null) {
        row = {
          year: v.Year, month: v.Month, date: new Date(parseInt(v.Year), v.Month - 1, 1)
        };
        data.push(row);
      }

      let count = v.Count;

      if (this.selectedBlockchain != 'All Blockchains') {
        let item = v.BlockchainCounts.find(c => c.BlockchainName == this.selectedBlockchain);
        if (item != null) {
          count = item.Count;
        } else {
          count = 0;
        }
      }

      row[v.HoldingTimeInMonths + 'monthscount'] = count;

      if (!yearsFound.includes(v.Year)) {
        yearsFound.push(v.Year);
      }
    });

    chart.data = data;
    chart.legend = new am4charts.Legend();
    chart.legend.scrollable = true;
    chart.legend.useDefaultMarker = true;

dateAxis: am4charts.DateAxis;
    
    var dateAxis = chart.xAxes.push(new am4charts.DateAxis());
    dateAxis.renderer.grid.template.location = 0;
    dateAxis.renderer.minGridDistance = 20;
    dateAxis.dateFormats.setKey("month", "M");
    dateAxis.periodChangeDateFormats.setKey("month", "M");
    dateAxis.dataFields.date = 'date';


    yearsFound.forEach(y => {
      let range = dateAxis.axisRanges.create();
      range.date = new Date(y, 0, 1);
      range.endDate = new Date(y, 11, 31);
      range.label.text = y;
      range.label.paddingTop = 40;
      range.label.location = 0.46;
      range.label.horizontalCenter = "middle";
      range.label.fontWeight = "bolder";
      range.grid.disabled = true;

      range = dateAxis.axisRanges.create();
      range.date = new Date(y, 0, 1);
      range.grid.strokeOpacity = 0.2;
      range.tick.disabled = false;
      range.tick.strokeOpacity = 0.3;
      range.tick.length = 60;
    });

    // let categoryAxis = chart.xAxes.push(new am4charts.CategoryAxis());
    // categoryAxis.dataFields.category = "year";
    // categoryAxis.renderer.grid.template.location = 0;
    // categoryAxis.renderer.minGridDistance = 65;


    let valueAxis = chart.yAxes.push(new am4charts.ValueAxis());
    valueAxis.tooltip.disabled = true;
    valueAxis.renderer.minWidth = 35;
    valueAxis.title.text = 'Jobs';

    chartData.HoldingTimesAvailable.forEach((v) => {

    let series = chart.series.push(new am4charts.ColumnSeries());
    //series.dataFields.valueY = 'count';
   series.dataFields.dateX = "date";
   series.dataFields.valueY = v.toString() + 'monthscount';
   //series.dataFields.categoryX = "date";
   //series.sequencedInterpolation = true;
   series.name = v.toString() + (v == 1 ? ' Month' : ' Months');
   series.stacked = true;
   series.columns.template.tooltipText = "[b]{valueY}[/] jobs at " + v.toString() + ' months';
   series.strokeWidth = 2;
   series.columns.template.strokeWidth = 0;

  });


    chart.cursor = new am4charts.XYCursor();
    chart.cursor.behavior = "panXY";
    chart.cursor.xAxis = dateAxis;

    chart.scrollbarX = new am4core.Scrollbar();

    if (this.themeService.currentTheme != 'light' && this.themeService.currentTheme != 'corporate' && this.themeService.currentTheme != 'default') {
      //title.fill = am4core.color('white');
      dateAxis.renderer.labels.template.fill = am4core.color('white');
      valueAxis.renderer.labels.template.fill = am4core.color('white');
      chart.legend.labels.template.fill = am4core.color('white');
    }

    chart.events.on('ready', () => {
      if (!this.hasSetZoomFirstTime) {
        this.hasSetZoomFirstTime = true;
        var start = new Date(new Date().getFullYear(), 0, 1);
        let end = new Date(start.getFullYear(), new Date().getMonth() + 1, 0);
        dateAxis.zoomToDates(start, end);
      }
    });
    //chart.scrollbarY = new am4core.Scrollbar();
  }

  getHeight() {
    let height = (this.screenHeight * 0.7);

    if (height < 600) {
      height = 600;
    }

    return height;
  }

  screenHeight: number;
  screenWidth: number;
  @HostListener('window:resize', ['$event'])
  getScreenSize(event?) {
        this.screenHeight = window.innerHeight;
        this.screenWidth = window.innerWidth;
  }

  hasSetZoomFirstTime = false;
  loadChart() {
    this.getBlockchains().subscribe(blockchains => {
      this.blockchains = blockchains;
      this.getData().subscribe(chartData => {
        this.createTopChart(chartData);
      });
    });
  }

  selectedBlockchain: string;
  changeBlockchainFilter(nodeName: string) {
    this.selectedBlockchain = nodeName;
    this.loadChart();
  }
}

interface HoldingTimePerMonthReportModel {
  Data: HoldingTimePerMonthModel[];
  HoldingTimesAvailable: Number[];
}

interface HoldingTimePerMonthModel {
  Year: string;
  Month: number;
  HoldingTimeInMonths: number;
  Count: number;
  BlockchainCounts: HoldingTimePerMonthBlockchainModel[];
}

interface HoldingTimePerMonthBlockchainModel {
  BlockchainName: string;
  Count: number;
}