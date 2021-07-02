import { isPlatformBrowser } from '@angular/common';
import { Component, Inject, NgZone, OnDestroy, OnInit, PLATFORM_ID } from '@angular/core';
import * as am4core from '@amcharts/amcharts4/core';
import * as am4charts from '@amcharts/amcharts4/charts';
import am4themes_animated from '@amcharts/amcharts4/themes/animated';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { HomeJobsChartDataModel } from 'app/pages/e-commerce/e-commerce.component';
import { HubHttpService } from 'app/pages/hub-http-service';

@Component({
  selector: 'ngx-staked-tokens-by-day',
  templateUrl: './staked-tokens-by-day.component.html',
  styleUrls: ['./staked-tokens-by-day.component.scss']
})
export class StakedTokensByDayComponent implements OnInit, OnDestroy {

  constructor(private zone: NgZone, 
    @Inject(PLATFORM_ID) private platformId, private http: HttpClient,  private httpService: HubHttpService) { }

  ngOnDestroy(): void {
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

  getData() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    const url = this.httpService.ApiUrl + '/api/reports/StakedTokensByDay';
    return this.http.get<TokensModel[]>(url, { headers });
  }

  loadChart() {
    this.getData().subscribe(chartData => {
      this.createTopChart(chartData);
      this.createBottomChart(chartData);
    });
  }
  createTopChart(chartData) {
    let chart = am4core.create("Chart", am4charts.XYChart);

    let data = [];

    chartData.forEach((v) => {
      data.push({ date: v.Date, name: "name", staked: v.Staked});
    });

    chart.data = data;
    chart.legend = new am4charts.Legend();
    chart.legend.scrollable = true;
    chart.legend.useDefaultMarker = true;

    let dateAxis = chart.xAxes.push(new am4charts.DateAxis());
    dateAxis.renderer.grid.template.location = 0;
    dateAxis.renderer.minGridDistance = 65;


    let valueAxis = chart.yAxes.push(new am4charts.ValueAxis());
    valueAxis.tooltip.disabled = true;
    valueAxis.renderer.minWidth = 35;
    valueAxis.title.text = 'Tokens';

    let series = chart.series.push(new am4charts.LineSeries());
    series.dataFields.dateX = "date";
    series.dataFields.valueY = "staked";
    series.tooltipText = "{valueY.value}";
    series.name = 'Staked';
    series.stroke = am4core.color('#5a03fc');
    series.fill = am4core.color('#5a03fc');
    series.strokeWidth = 1;
    series.fillOpacity = 0.2;

    chart.cursor = new am4charts.XYCursor();
    chart.cursor.behavior = "panXY";
    chart.cursor.xAxis = dateAxis;
  }
  createBottomChart(chartData) {
    let chart = am4core.create("ChartDW", am4charts.XYChart);

    let data = [];

    chartData.forEach((v) => {
      data.push({ date: v.Date, name: "name", deposit: v.Deposited, withdrawn: v.Withdrawn});
    });

    chart.data = data;
    chart.legend = new am4charts.Legend();
    chart.legend.scrollable = true;
    chart.legend.useDefaultMarker = true;

    let dateAxis = chart.xAxes.push(new am4charts.DateAxis());
    dateAxis.renderer.grid.template.location = 0;
    dateAxis.renderer.minGridDistance = 65;



    let valueAxis = chart.yAxes.push(new am4charts.ValueAxis());
    valueAxis.tooltip.disabled = true;
    valueAxis.renderer.minWidth = 35;
    valueAxis.title.text = 'Tokens';

    let series = chart.series.push(new am4charts.LineSeries());
    series.dataFields.dateX = "date";
    series.dataFields.valueY = "deposit";
    series.tooltipText = "{valueY.value}";
    series.name = 'Deposited';
    series.stroke = am4core.color('#03fcbe');
    series.fill = am4core.color('#03fcbe');
    series.fillOpacity = 0.2;
    series.strokeWidth = 1;

    series = chart.series.push(new am4charts.LineSeries());
    series.dataFields.dateX = "date";
    series.dataFields.valueY = "withdrawn";
    series.tooltipText = "{valueY.value}";
    series.name = 'Withdrawn';
    series.stroke = am4core.color('#fc9403');
    series.fill = am4core.color('#fc9403');
    series.fillOpacity = 0.2;
    series.strokeWidth = 1;

    chart.cursor = new am4charts.XYCursor();
    chart.cursor.behavior = "panXY";
    chart.cursor.xAxis = dateAxis;
  }
}

interface TokensModel {
Date: Date;
Deposited: string;
Withdrawn: string;
Staked: string;
}