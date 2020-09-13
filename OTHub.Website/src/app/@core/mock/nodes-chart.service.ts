import { Injectable } from '@angular/core';
import { PeriodsService } from './periods.service';
import { NodesChart, NodesChartData } from '../data/nodes-chart';

@Injectable()
export class NodesChartService extends NodesChartData {

  private year = [
    '2012',
    '2013',
    '2014',
    '2015',
    '2016',
    '2017',
    '2018',
  ];

  private data = {};

  constructor(private period: PeriodsService) {
    super();
    this.data = {
      '7 Days': this.getDataForWeekPeriod(),
      '12 Months': this.getDataForMonthPeriod(),
      'All Years': this.getDataForYearPeriod(),
    };
  }

  private getDataForWeekPeriod(): NodesChart {
    const nPoint = this.period.getWeeks().length;

    return {
      period: '7 Days',
      chartLabel: this.period.getWeeks(),
      data: [
        this.getRandomData(nPoint),
        this.getRandomData(nPoint),
        this.getRandomData(nPoint),
      ],
    };
  }

  private getDataForMonthPeriod(): NodesChart {
    const nPoint = this.period.getMonths().length;

    return {
      period: 'month',
      chartLabel: this.period.getMonths(),
      data: [
        this.getRandomData(nPoint),
        this.getRandomData(nPoint),
        this.getRandomData(nPoint),
      ],
    };
  }

  private getDataForYearPeriod(): NodesChart {
    const nPoint = this.year.length;

    return {
      period: 'year',
      chartLabel: this.year,
      data: [
        this.getRandomData(nPoint),
        this.getRandomData(nPoint),
        this.getRandomData(nPoint),
      ],
    };
  }

  private getRandomData(nPoints: number): number[] {
    return Array.from(Array(nPoints)).map(() => {
      return Math.round(Math.random() * 500);
    });
  }

  getNodesChartData(period: string): NodesChart {
    return this.data[period];
  }
}
