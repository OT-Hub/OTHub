import { Component, EventEmitter, Input, OnDestroy, OnInit, Output } from '@angular/core';
import { NbMediaBreakpoint, NbMediaBreakpointsService, NbThemeService } from '@nebular/theme';
import { takeWhile } from 'rxjs/operators';


@Component({
  selector: 'ngx-chart-panel-header',
  styleUrls: ['./chart-panel-header.component.scss'],
  templateUrl: './chart-panel-header.component.html',
})
export class ChartPanelHeaderComponent implements OnDestroy, OnInit {

  private alive = true;

  @Output() periodChange = new EventEmitter<string>();

  @Input() type: string = '7 Days';
  @Input() isjobschart: string;

  types: string[] = ['7 Days', '12 Months', 'All Years'];
  chartLegend: {iconColor: string; title: string}[];
  breakpoint: NbMediaBreakpoint = { name: '', width: 0 };
  breakpoints: any;
  currentTheme: string;

  constructor(private themeService: NbThemeService,
              private breakpointService: NbMediaBreakpointsService) {
    this.themeService.getJsTheme()
      .pipe(takeWhile(() => this.alive))
      .subscribe(theme => {
        const orderProfitLegend = theme.variables.orderProfitLegend;

        this.currentTheme = theme.name;
        this.setLegendItems(orderProfitLegend);
      });

      this.breakpoints = this.breakpointService.getBreakpointsMap();
      this.themeService.onMediaQueryChange()
        .pipe(takeWhile(() => this.alive))
        .subscribe(([oldValue, newValue]) => {
          this.breakpoint = newValue;
        });
  }

  setLegendItems(orderProfitLegend) {

    this.chartLegend = [
      {
        iconColor: orderProfitLegend.firstItem,
        title: this.isjobschart == 'true' ? 'Jobs Started' : 'Nodes with Active Jobs',
      },
      {
        iconColor: orderProfitLegend.secondItem,
        title: 'Jobs Completed',
      }
    ];
  }

  changePeriod(period: string): void {
    this.type = period;
    this.periodChange.emit(period);
  }

  ngOnInit() {
    this.themeService.getJsTheme().subscribe(j => {
      this.setLegendItems(j.variables.orderProfitLegend);
    });
  }

  ngOnDestroy() {
    this.alive = false;
  }
}
