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
  selector: 'ngx-job-heatmap',
  templateUrl: './job-heatmap.component.html',
  styleUrls: ['./job-heatmap.component.scss']
})
export class JobHeatmapComponent implements OnInit {
  heatLegend: am4charts.HeatLegend;
  blockchains: Blockchain[];
  nothingToShow: boolean;

  constructor(private zone: NgZone,
    @Inject(PLATFORM_ID) private platformId, private http: HttpClient, private httpService: HubHttpService,
    private themeService: NbThemeService) {
    this.selectedBlockchain = 'All Blockchains';
    this.nothingToShow = false;
    this.getScreenSize();
  }

  selectedBlockchain: string;

  ngOnInit(): void {
    this.getBlockchains().subscribe(blockchains => {
      this.blockchains = blockchains;
      this.load();
    });
  }

  load() {
    this.getData().subscribe(data => {
      this.loadChart(data);
    });
  }

  getBlockchains() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    let url = this.httpService.ApiUrl + '/api/blockchain/GetBlockchains';

    return this.http.get<Blockchain[]>(url, { headers });
  }

  changeBlockchainFilter(nodeName: string) {
    this.selectedBlockchain = nodeName;
    this.load();
  }

  getHeight() {
    let height = (this.screenHeight * 0.7);

    if (height < 600) {
      height = 600;
    }

    return height;
  }

  handleHover(column) {
    if (!isNaN(column.dataItem.value)) {
      this.heatLegend.valueAxis.showTooltipAt(column.dataItem.value)
    }
    else {
      this.heatLegend.valueAxis.hideTooltip();
    }
  }

  getData() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    const url = this.httpService.ApiUrl + '/api/reports/JobHeatmap?blockchain=' + (this.selectedBlockchain == 'All Blockchains' ? '' : this.selectedBlockchain);
    return this.http.get<JobHeatmapModel[]>(url, { headers });
  }
  


  loadChart(data: JobHeatmapModel[]) {

    let hasData = false;

    data.forEach(element => {
      if (element.Count > 0) {
        hasData = true;
        return;
      }
    });

    this.nothingToShow = !hasData;

    let chart = am4core.create("Chart", am4charts.XYChart);
    chart.maskBullets = false;

    let xAxis = chart.xAxes.push(new am4charts.CategoryAxis());
    let yAxis = chart.yAxes.push(new am4charts.CategoryAxis());

    xAxis.dataFields.category = "Day";
    yAxis.dataFields.category = "HourText";

    xAxis.renderer.grid.template.disabled = true;
    xAxis.renderer.minGridDistance = 40;

    yAxis.renderer.grid.template.disabled = true;
    yAxis.renderer.inversed = true;
    yAxis.renderer.minGridDistance = 30;

    let series = chart.series.push(new am4charts.ColumnSeries());
    series.dataFields.categoryX = "Day";
    series.dataFields.categoryY = "HourText";
    series.dataFields.value = "Count";
    series.sequencedInterpolation = true;
    series.defaultState.transitionDuration = 3000;

    let bgColor = new am4core.InterfaceColorSet().getFor("background");

    let columnTemplate = series.columns.template;
    columnTemplate.strokeWidth = 1;
    columnTemplate.strokeOpacity = 0.2;
    columnTemplate.stroke = bgColor;
    columnTemplate.tooltipText = "{Day}, {HourText}: {value.workingValue.formatNumber('#.')}";
    columnTemplate.width = am4core.percent(100);
    columnTemplate.height = am4core.percent(100);

    series.heatRules.push({
      target: columnTemplate,
      property: "fill",
      min: am4core.color("#ffffff"),
      max: am4core.color("#0b58bd")
    });


    // heat legend
    this.heatLegend = chart.bottomAxesContainer.createChild(am4charts.HeatLegend);
    this.heatLegend.width = am4core.percent(100);
    this.heatLegend.series = series;
    this.heatLegend.valueAxis.renderer.labels.template.fontSize = 9;
    this.heatLegend.valueAxis.renderer.minGridDistance = 30;

    let that = this;

    // heat legend behavior
    series.columns.template.events.on("over", function (event) {
      that.handleHover(event.target);
    })

    series.columns.template.events.on("hit", function (event) {
      that.handleHover(event.target);
    })

    series.columns.template.events.on("out", function(event) {
      that.heatLegend.valueAxis.hideTooltip();
    })

    chart.data = data;
  }

  screenHeight: number;
  screenWidth: number;
  @HostListener('window:resize', ['$event'])
  getScreenSize(event?) {
    this.screenHeight = window.innerHeight;
    this.screenWidth = window.innerWidth;
  }

  

}

export interface JobHeatmapModel {
  Day: string;
  HourText: string;
  Count: number;
}
