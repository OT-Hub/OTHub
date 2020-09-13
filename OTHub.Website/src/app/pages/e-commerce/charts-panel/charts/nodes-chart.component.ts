import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AfterViewInit, Component, Input, OnChanges, OnDestroy } from '@angular/core';
import { NbThemeService } from '@nebular/theme';
import { delay, takeWhile } from 'rxjs/operators';

import { NodesChart } from '../../../../@core/data/nodes-chart';
import { LayoutService } from '../../../../@core/utils/layout.service';
import { HubHttpService } from '../../../hub-http-service';
import { OTHomeJobsChartData, OTHomeNodesChartData } from './OTHomeJobsChartData';

@Component({
  selector: 'ngx-nodes-chart',
  styleUrls: ['./charts-common.component.scss'],
  template: `
    <div echarts [options]="options" class="echart" (chartInit)="onChartInit($event)"></div>
  `,
})
export class NodesChartComponent implements AfterViewInit, OnDestroy, OnChanges {

  @Input()
  nodesChartData: NodesChart;

  private alive = true;

  echartsIntance: any;
  options: any = {};
  nodesData: OTHomeNodesChartData;

  constructor(private theme: NbThemeService,
              private layoutService: LayoutService, private httpService: HubHttpService,
              private http: HttpClient) {
    this.layoutService.onSafeChangeLayoutSize()
      .pipe(
        takeWhile(() => this.alive),
      )
      .subscribe(() => this.resizeChart());
  }

  getNodesChartData() {
		const headers = new HttpHeaders()
			.set('Content-Type', 'application/json')
			.set('Accept', 'application/json');
		const url = this.httpService.ApiUrl + '/api/home/nodeschartdatav2?' + (new Date()).getTime();
		return this.http.get<OTHomeNodesChartData>(url, { headers });
	}

  ngOnChanges(): void {
    if (this.echartsIntance) {
      this.updateNodesChartOptions(this.nodesChartData);
    }
  }

  ngAfterViewInit() {

    this.theme.getJsTheme()
    .pipe(
      takeWhile(() => this.alive),
      delay(1),
    )
    .subscribe(config => {

      this.getNodesChartData().subscribe(data => {

      this.nodesData = data;

      const eTheme: any = config.variables.orders;

   

      this.setOptions(eTheme);
      this.updateNodesChartOptions(this.nodesChartData);

      });
    });

    // this.theme.getJsTheme()
    //   .pipe(takeWhile(() => this.alive))
    //   .subscribe(config => {
    //     const eTheme: any = config.variables.profit;

    //     this.setOptions(eTheme);
    //   });
  }

  setOptions(eTheme) {
    this.options = {
      backgroundColor: eTheme.bg,
      tooltip: {
        trigger: 'axis',
        axisPointer: {
          type: 'shadow',
          shadowStyle: {
            color: 'rgba(0, 0, 0, 0.3)',
          },
        },
      },
      grid: {
        left: '3%',
        right: '4%',
        bottom: '3%',
        containLabel: true,
      },
      xAxis: {
        type: 'category',
        boundaryGap: false,
        offset: 5,
        data: [],
        axisTick: {
          show: true,
          alignWithLabel: true,
        },
        // axisLabel: {
        //   //color: eTheme.axisTextColor,
        //   lineStyle: {
        //     color: eTheme.axisLineColor,
        //   },
        // },
        // axisLine: {
        //   lineStyle: {
        //     color: eTheme.axisTextColor,
        //     fontSize: eTheme.axisFontSize,
        //   },
        // },
      },
      yAxis: [
        {
          type: 'value',
          axisLine: {
            lineStyle: {
              color: eTheme.axisLineColor,
            },
          },
          splitLine: {
            lineStyle: {
              color: eTheme.splitLineColor,
            },
          },
          axisLabel: {
            color: eTheme.axisTextColor,
            fontSize: eTheme.axisFontSize,
          },
        },
      ],
      series: [
        this.getSecondLine(eTheme),
        this.getThirdLine(eTheme),
        // {
        //   name: 'Canceled',
        //   type: 'bar',
        //   barGap: 0,
        //   barWidth: '20%',
        //   itemStyle: {
        //     normal: {
        //       color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [{
        //         offset: 0,
        //         color: eTheme.firstLineGradFrom,
        //       }, {
        //         offset: 1,
        //         color: eTheme.firstLineGradTo,
        //       }]),
        //     },
        //   },
        //   data: [],
        // },
        // {
        //   name: 'Payment',
        //   type: 'bar',
        //   barWidth: '20%',
        //   itemStyle: {
        //     normal: {
        //       color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [{
        //         offset: 0,
        //         color: eTheme.secondLineGradFrom,
        //       }, {
        //         offset: 1,
        //         color: eTheme.secondLineGradTo,
        //       }]),
        //     },
        //   },
        //   data: [],
        // },
        // {
        //   name: 'All orders',
        //   type: 'bar',
        //   barWidth: '20%',
        //   itemStyle: {
        //     normal: {
        //       color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [{
        //         offset: 0,
        //         color: eTheme.thirdLineGradFrom,
        //       }, {
        //         offset: 1,
        //         color: eTheme.thirdLineGradTo,
        //       }]),
        //     },
        //   },
        //   data: [],
        // },
      ],
    };
  }

  getSecondLine(eTheme) {
    return         {
      type: 'line',
      smooth: true,
      symbolSize: 20,
      itemStyle: {
        normal: {
          opacity: 0,
        },
        emphasis: {
          color: '#ffffff',
          borderColor: eTheme.itemBorderColor,
          borderWidth: 2,
          opacity: 1,
        },
      },
      lineStyle: {
        normal: {
          width: eTheme.lineWidth,
          type: eTheme.lineStyle,
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [{
            offset: 0,
            color: eTheme.secondLineGradFrom,
          }, {
            offset: 1,
            color: eTheme.secondLineGradTo,
          }]),
        },
      },
      areaStyle: {
        normal: {
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [{
            offset: 0,
            color: eTheme.secondAreaGradFrom,
          }, {
            offset: 1,
            color: eTheme.secondAreaGradTo,
          }]),
        },
      },
      data: [],
    };
  }

  getThirdLine(eTheme) {
    return {
      type: 'line',
      smooth: true,
      symbolSize: 20,
      itemStyle: {
        normal: {
          opacity: 0,
        },
        emphasis: {
          color: '#ffffff',
          borderColor: eTheme.itemBorderColor,
          borderWidth: 2,
          opacity: 1,
        },
      },
      lineStyle: {
        normal: {
          width: eTheme.lineWidth,
          type: eTheme.lineStyle,
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [{
            offset: 0,
            color: eTheme.thirdLineGradFrom,
          }, {
            offset: 1,
            color: eTheme.thirdLineGradTo,
          }]),
        },
      },
      areaStyle: {
        normal: {
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [{
            offset: 0,
            color: eTheme.thirdAreaGradFrom,
          }, {
            offset: 1,
            color: eTheme.thirdAreaGradTo,
          }]),
        },
      },
      data: [],
    };
  }

  updateNodesChartOptions(nodesChartData: NodesChart) {

    const options = this.options;

    let data;
    let labels;

    if (nodesChartData.period == '12 Months') {
      data = this.nodesData.Month;
      labels = this.nodesData.MonthLabels;
    }
    else if (nodesChartData.period == 'All Years') {
      data = this.nodesData.Year;
      labels = this.nodesData.YearLabels;
    } else {
      data = this.nodesData.Week;
      labels = this.nodesData.WeekLabels;
    }
    
    const series = this.getNewSeries(options.series, data);
    const xAxis = this.getNewXAxis(options.xAxis, labels);
    //const series = this.getNewSeries(options.series, ordersChartData.linesData);
    //const xAxis = this.getNewXAxis(options.xAxis, ordersChartData.chartLabel);

    this.options = {
      ...options,
      xAxis,
      series,
    };

    // const options = this.options;
    // const series = this.getNewSeries(options.series, nodesChartData.data);

    // this.echartsIntance.setOption({
    //   series: series,
    //   xAxis: {
    //     data: this.nodesChartData.chartLabel,
    //   },
    // });
  }

  getNewSeries(series, data: number[][]) {
    return series.map((line, index) => {
      return {
        ...line,
        data: data[index],
      };
    });
  }

  getNewXAxis(xAxis, chartLabel: string[]) {
    return {
      ...xAxis,
      data: chartLabel,
    };
  }

  onChartInit(echarts) {
    this.echartsIntance = echarts;
  }

  resizeChart() {
    if (this.echartsIntance) {
      // Fix recalculation chart size
      // TODO: investigate more deeply
      setTimeout(() => {
        this.echartsIntance.resize();
      }, 0);
    }
  }

  ngOnDestroy(): void {
    this.alive = false;
  }
}
